using FinanceReport.Domain.Enums;

namespace FinanceReport.Application.Dtos;

/// <summary>Indicateurs de la page d'accueil (FS §3.8). <c>MonthVariation</c> est null si non calculable (RG-15).</summary>
public sealed record SummaryDto(
    DateOnly Date,
    decimal TotalNetWorth,
    MonthVariationDto? MonthVariation,
    GainDto UnrealizedGain,
    IReadOnlyList<AccountTypeShareDto> ByAccountType,
    int MissingPriceCount);

/// <param name="Percent">Null si le patrimoine de référence vaut 0.</param>
public sealed record MonthVariationDto(decimal Amount, decimal? Percent, DateOnly ReferenceDate);

/// <param name="Percent">En % du coût d'acquisition (Σ quantité × PRU) ; null sans position.</param>
public sealed record GainDto(decimal Amount, decimal? Percent);

public sealed record AccountTypeShareDto(AccountType Type, decimal Amount, decimal Percent);

public sealed record HistoryDto(string Period, IReadOnlyList<HistoryPointDto> Points);

public sealed record HistoryPointDto(DateOnly Date, decimal TotalNetWorth);

public sealed record DashboardFilter(
    IReadOnlyCollection<Guid> AccountIds,
    IReadOnlyCollection<SecurityType> SecurityTypes,
    IReadOnlyCollection<string> Zones,
    IReadOnlyCollection<string> Sectors)
{
    public static DashboardFilter None { get; } = new([], [], [], []);

    /// <summary>RG-28 : les comptes espèces et les liquidités sont inclus tant qu'aucun filtre de support n'est actif.</summary>
    public bool IncludesCash => SecurityTypes.Count == 0 && Zones.Count == 0 && Sectors.Count == 0;
}

public sealed record DashboardPositionsDto(
    IReadOnlyList<DashboardPositionDto> Positions,
    AllocationDto Allocation,
    bool IncludesCash);

public sealed record DashboardPositionDto(
    Guid AccountId,
    string AccountName,
    Guid SecurityId,
    string SecurityName,
    string SecurityCode,
    SecurityType SecurityType,
    string Zone,
    string ZoneLabel,
    string Sector,
    string SectorLabel,
    decimal Quantity,
    decimal AverageCost,
    decimal Price,
    DateOnly? PriceDate,
    bool MissingPrice,
    decimal MarketValue,
    decimal UnrealizedGain,
    decimal? UnrealizedGainPercent);

public sealed record AllocationDto(
    IReadOnlyList<AllocationShareDto> ByAccount,
    IReadOnlyList<AllocationShareDto> BySecurityType,
    IReadOnlyList<AllocationShareDto> ByZone,
    IReadOnlyList<AllocationShareDto> BySector);

/// <param name="Key">Id du compte, type de support, code de zone ou de secteur, ou <c>CASH</c> pour les liquidités.</param>
public sealed record AllocationShareDto(string Key, string Label, decimal Amount, decimal Percent);

public sealed record DashboardAccountsDto(IReadOnlyList<DashboardAccountDto> Accounts, DashboardAccountsTotalDto Total);

/// <param name="UnrealizedGain">Null pour un compte espèces.</param>
/// <param name="RealizedGain">Cumul, positions soldées comprises ; null pour un compte espèces.</param>
public sealed record DashboardAccountDto(
    Guid AccountId,
    string Name,
    AccountType Type,
    decimal Value,
    decimal? UnrealizedGain,
    decimal? RealizedGain);

public sealed record DashboardAccountsTotalDto(decimal Value, decimal UnrealizedGain, decimal RealizedGain);
