using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;

namespace FinanceReport.Domain.Calculators;

public static class ValuationCalculator
{
    /// <summary>Valorise le patrimoine à <paramref name="asOf"/> (TS §3.3, RG-06 à RG-09, RG-11, RG-23, RG-27).</summary>
    public static Valuation Valuate(
        DateOnly asOf,
        IEnumerable<Account> accounts,
        IEnumerable<Movement> movements,
        IEnumerable<Balance> balances,
        IEnumerable<SecurityPrice> prices) =>
        Valuate(
            asOf,
            accounts,
            PositionCalculator.Compute(movements, asOf),
            DatedSeries<Guid>.Create(balances, b => b.AccountId, b => b.Date, b => b.Amount),
            DatedSeries<Guid>.Create(prices, p => p.SecurityId, p => p.Date, p => p.Price));

    /// <summary>
    /// Variante qui reçoit des positions déjà rejouées jusqu'à <paramref name="asOf"/> et des séries indexées,
    /// pour les reconstructions de snapshots en un seul passage chronologique.
    /// </summary>
    public static Valuation Valuate(
        DateOnly asOf,
        IEnumerable<Account> accounts,
        IReadOnlyDictionary<PositionKey, PositionState> positions,
        DatedSeries<Guid> balances,
        DatedSeries<Guid> prices)
    {
        // RG-27 : les comptes archivés sont exclus ; seules les positions des comptes titres sont valorisées (RG-21).
        var activeAccounts = accounts.Where(a => !a.Archived).ToList();
        var securitiesAccountIds = activeAccounts
            .Where(a => a.Type.IsSecuritiesAccount())
            .Select(a => a.Id)
            .ToHashSet();

        var positionsByAccount = positions
            .Where(p => securitiesAccountIds.Contains(p.Key.AccountId))
            .GroupBy(p => p.Key.AccountId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var positionValuations = new List<PositionValuation>();
        var accountValuations = new List<AccountValuation>();

        foreach (var account in activeAccounts)
        {
            var cashEntry = balances.Latest(account.Id, asOf);
            var cash = cashEntry?.Value ?? 0m;

            if (!account.Type.IsSecuritiesAccount())
            {
                // RG-08 : compte espèces = dernier solde.
                accountValuations.Add(new AccountValuation(account.Id, account.Type, cash, cash, cashEntry?.Date, 0m, 0m, 0m));
                continue;
            }

            var accountPositions = positionsByAccount.GetValueOrDefault(account.Id) ?? [];
            var open = accountPositions
                .Where(p => p.Value.IsOpen)
                .OrderBy(p => p.Key.SecurityId)
                .Select(p => ValuatePosition(p.Key, p.Value, asOf, prices))
                .ToList();
            positionValuations.AddRange(open);

            // RG-07 : compte titres = Σ valorisations + liquidités.
            accountValuations.Add(new AccountValuation(
                account.Id,
                account.Type,
                open.Sum(p => p.MarketValue) + cash,
                cash,
                cashEntry?.Date,
                open.Sum(p => p.CostBasis),
                open.Sum(p => p.UnrealizedGain),
                accountPositions.Sum(p => p.Value.RealizedGain)));
        }

        // RG-09 : patrimoine total = Σ valeurs des comptes non archivés.
        return new Valuation(asOf, accountValuations.Sum(a => a.Value), accountValuations, positionValuations);
    }

    private static PositionValuation ValuatePosition(PositionKey key, PositionState state, DateOnly asOf, DatedSeries<Guid> prices)
    {
        // RG-23 : dernier cours de date ≤ D ; RG-11 : à défaut, valorisation au PRU.
        var priceEntry = prices.Latest(key.SecurityId, asOf);
        var price = priceEntry?.Value ?? state.AverageCost;

        return new PositionValuation(
            key.AccountId,
            key.SecurityId,
            state.Quantity,
            state.AverageCost,
            price,
            priceEntry?.Date,
            priceEntry is null,
            state.Quantity * price,
            // RG-06 : PV latente = Q × (cours − PRU).
            state.Quantity * (price - state.AverageCost));
    }
}
