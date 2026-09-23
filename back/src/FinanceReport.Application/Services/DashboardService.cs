using FinanceReport.Application.Dtos;
using FinanceReport.Application.Exceptions;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Calculators;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FinanceReport.Domain.Rules;

namespace FinanceReport.Application.Services;

/// <summary>
/// Restitutions (TS §3.5) : valorisation du jour pour l'accueil et le tableau de bord, snapshots pour la variation
/// du mois et la courbe. Les montants sont arrondis uniquement ici, au passage vers les DTO (RG-29).
/// </summary>
public sealed class DashboardService(
    IRepository<Account> accounts,
    IRepository<Balance> balances,
    IRepository<Movement> movements,
    IRepository<SecurityPrice> prices,
    IRepository<Security> securities,
    IRepository<Snapshot> snapshots,
    IReferentialRepository referentials,
    IClock clock)
{
    public const string CashKey = "CASH";
    public const string CashLabel = "Espèces et liquidités";

    public SummaryDto Summary()
    {
        var today = clock.Today;
        var valuation = ValuateToday(accounts.GetAll());

        // RG-15 : référence = dernier snapshot antérieur au 1er du mois en cours.
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
        var reference = snapshots.GetAll().Where(s => s.Date < firstOfMonth).MaxBy(s => s.Date);
        MonthVariationDto? variation = null;
        if (reference is not null)
        {
            var amount = valuation.TotalNetWorth - reference.TotalNetWorth;
            variation = new MonthVariationDto(
                Amount(amount),
                reference.TotalNetWorth == 0m ? null : Percent(amount / reference.TotalNetWorth * 100m),
                reference.Date);
        }

        var unrealized = valuation.Positions.Sum(p => p.UnrealizedGain);
        var costBasis = valuation.Positions.Sum(p => p.CostBasis);
        var byType = valuation.Accounts
            .GroupBy(a => a.Type)
            .Select(g => (Type: g.Key, Amount: g.Sum(a => a.Value)))
            .OrderByDescending(t => t.Amount)
            .Select(t => new AccountTypeShareDto(t.Type, Amount(t.Amount), Share(t.Amount, valuation.TotalNetWorth)))
            .ToList();

        return new SummaryDto(
            today,
            Amount(valuation.TotalNetWorth),
            variation,
            new GainDto(Amount(unrealized), costBasis == 0m ? null : Percent(unrealized / costBasis * 100m)),
            byType,
            valuation.Positions.Count(p => p.MissingPrice));
    }

    /// <summary>Points de la courbe (UC-12) : 1M = 30 derniers jours, 1A = 365 derniers jours, ALL = tout.</summary>
    public HistoryDto History(string? period)
    {
        var today = clock.Today;
        DateOnly? from = period switch
        {
            "1M" => today.AddDays(-30),
            "1A" => today.AddDays(-365),
            "ALL" => null,
            _ => throw ValidationException.ForField("period", "La période doit valoir 1M, 1A ou ALL."),
        };

        return new HistoryDto(
            period,
            [
                .. snapshots.GetAll()
                    .Where(s => from is null || s.Date >= from)
                    .OrderBy(s => s.Date)
                    .Select(s => new HistoryPointDto(s.Date, Amount(s.TotalNetWorth))),
            ]);
    }

    /// <summary>
    /// Positions et répartitions du tableau de bord (FS §3.10). Filtres : ET entre critères, OU dans un critère.
    /// Dès qu'un filtre de support est actif, les comptes espèces et les liquidités sont exclus (RG-28).
    /// </summary>
    public DashboardPositionsDto Positions(DashboardFilter filter)
    {
        var allAccounts = accounts.GetAll();
        var accountsById = allAccounts.ToDictionary(a => a.Id);
        var valuation = ValuateToday(allAccounts);
        var labels = new Labels(referentials);
        var securitiesById = securities.GetAll().ToDictionary(s => s.Id);

        var scopedAccounts = valuation.Accounts
            .Where(a => filter.AccountIds.Count == 0 || filter.AccountIds.Contains(a.AccountId))
            .ToList();
        var scopedAccountIds = scopedAccounts.Select(a => a.AccountId).ToHashSet();

        var rows = valuation.Positions
            .Where(p => scopedAccountIds.Contains(p.AccountId))
            .Select(p => (Position: p, Security: securitiesById[p.SecurityId]))
            .Where(x => filter.SecurityTypes.Count == 0 || filter.SecurityTypes.Contains(x.Security.Type))
            .Where(x => filter.Zones.Count == 0 || filter.Zones.Contains(x.Security.Zone))
            .Where(x => filter.Sectors.Count == 0 || filter.Sectors.Contains(x.Security.Sector))
            .ToList();

        var includesCash = filter.IncludesCash;
        var cash = includesCash ? scopedAccounts.Sum(a => a.Cash) : 0m;

        var byAccount = includesCash
            ? scopedAccounts.Select(a => (Key: a.AccountId.ToString(), Label: accountsById[a.AccountId].Name, Amount: a.Value))
            : rows.GroupBy(r => r.Position.AccountId)
                .Select(g => (Key: g.Key.ToString(), Label: accountsById[g.Key].Name, Amount: g.Sum(r => r.Position.MarketValue)));

        var allocation = new AllocationDto(
            Shares(byAccount),
            Shares(WithCash(rows.GroupBy(r => r.Security.Type)
                .Select(g => (Key: SecurityTypeCode(g.Key), Label: SecurityTypeCode(g.Key), Amount: g.Sum(r => r.Position.MarketValue))), includesCash, cash)),
            Shares(WithCash(rows.GroupBy(r => r.Security.Zone)
                .Select(g => (Key: g.Key, Label: labels.Zone(g.Key), Amount: g.Sum(r => r.Position.MarketValue))), includesCash, cash)),
            Shares(WithCash(rows.GroupBy(r => r.Security.Sector)
                .Select(g => (Key: g.Key, Label: labels.Sector(g.Key), Amount: g.Sum(r => r.Position.MarketValue))), includesCash, cash)));

        var positions = rows
            .OrderBy(r => accountsById[r.Position.AccountId].Name, TextComparison.French)
            .ThenBy(r => r.Security.Name, TextComparison.French)
            .Select(r => ToDto(r.Position, r.Security, accountsById[r.Position.AccountId], labels))
            .ToList();

        return new DashboardPositionsDto(positions, allocation, includesCash);
    }

    /// <summary>Tableau des comptes non archivés, avec plus-values latentes et réalisées et ligne de total.</summary>
    public DashboardAccountsDto Accounts(IReadOnlyCollection<Guid> accountIds)
    {
        var allAccounts = accounts.GetAll();
        var accountsById = allAccounts.ToDictionary(a => a.Id);
        var rows = ValuateToday(allAccounts).Accounts
            .Where(a => accountIds.Count == 0 || accountIds.Contains(a.AccountId))
            .OrderByDescending(a => a.Value)
            .ToList();

        return new DashboardAccountsDto(
            [
                .. rows.Select(a =>
                {
                    var isSecurities = a.Type.IsSecuritiesAccount();
                    return new DashboardAccountDto(
                        a.AccountId,
                        accountsById[a.AccountId].Name,
                        a.Type,
                        Amount(a.Value),
                        isSecurities ? Amount(a.UnrealizedGain) : null,
                        isSecurities ? Amount(a.RealizedGain) : null);
                }),
            ],
            new DashboardAccountsTotalDto(
                Amount(rows.Sum(a => a.Value)),
                Amount(rows.Sum(a => a.UnrealizedGain)),
                Amount(rows.Sum(a => a.RealizedGain))));
    }

    private Valuation ValuateToday(IReadOnlyList<Account> allAccounts) =>
        ValuationCalculator.Valuate(clock.Today, allAccounts, movements.GetAll(), balances.GetAll(), prices.GetAll());

    private static IEnumerable<(string Key, string Label, decimal Amount)> WithCash(
        IEnumerable<(string Key, string Label, decimal Amount)> shares, bool includesCash, decimal cash) =>
        includesCash ? shares.Append((CashKey, CashLabel, cash)) : shares;

    private static List<AllocationShareDto> Shares(IEnumerable<(string Key, string Label, decimal Amount)> shares)
    {
        var list = shares.ToList();
        var total = list.Sum(s => s.Amount);
        return
        [
            .. list.OrderByDescending(s => s.Amount)
                .Select(s => new AllocationShareDto(s.Key, s.Label, Amount(s.Amount), Share(s.Amount, total))),
        ];
    }

    private static DashboardPositionDto ToDto(PositionValuation p, Security security, Account account, Labels labels)
    {
        var priceDecimals = Precision.Price(security.Type);
        return new DashboardPositionDto(
            p.AccountId,
            account.Name,
            p.SecurityId,
            security.Name,
            security.Code,
            security.Type,
            security.Zone,
            labels.Zone(security.Zone),
            security.Sector,
            labels.Sector(security.Sector),
            DecimalMath.Round(p.Quantity, Precision.Quantity),
            DecimalMath.Round(p.AverageCost, priceDecimals),
            DecimalMath.Round(p.Price, priceDecimals),
            p.PriceDate,
            p.MissingPrice,
            Amount(p.MarketValue),
            Amount(p.UnrealizedGain),
            p.CostBasis == 0m ? null : Percent(p.UnrealizedGain / p.CostBasis * 100m));
    }

    private static string SecurityTypeCode(SecurityType type) => type.ToString().ToUpperInvariant();

    private static decimal Share(decimal amount, decimal total) => total == 0m ? 0m : Percent(amount / total * 100m);

    private static decimal Amount(decimal value) => DecimalMath.Round(value, Precision.Amount);

    private static decimal Percent(decimal value) => DecimalMath.Round(value, Precision.Percent);

    /// <summary>Libellés courants des zones et secteurs, archivés compris (FS §3.10).</summary>
    private sealed class Labels(IReferentialRepository referentials)
    {
        private readonly Dictionary<string, string> _zones = ByCode(referentials.For(ReferentialKind.Zones).GetAll());
        private readonly Dictionary<string, string> _sectors = ByCode(referentials.For(ReferentialKind.Sectors).GetAll());

        public string Zone(string code) => _zones.GetValueOrDefault(code, code);

        public string Sector(string code) => _sectors.GetValueOrDefault(code, code);

        private static Dictionary<string, string> ByCode(IEnumerable<ReferentialItem> items) =>
            items.Where(i => i.Code is not null).ToDictionary(i => i.Code!, i => i.Label);
    }
}
