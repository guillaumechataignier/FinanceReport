using FinanceReport.Domain.Enums;

namespace FinanceReport.Domain.Calculators;

/// <summary>Valorisation du patrimoine à une date, en précision complète (arrondi à la sortie de l'API, RG-29).</summary>
public sealed record Valuation(
    DateOnly Date,
    decimal TotalNetWorth,
    IReadOnlyList<AccountValuation> Accounts,
    IReadOnlyList<PositionValuation> Positions);

/// <param name="Cash">Solde retenu (compte espèces) ou liquidités (compte titres) ; 0 sans solde saisi.</param>
/// <param name="CashDate">Date du solde retenu, null sans solde saisi.</param>
/// <param name="RealizedGain">Plus-value réalisée cumulée, positions soldées comprises (RG-05).</param>
public sealed record AccountValuation(
    Guid AccountId,
    AccountType Type,
    decimal Value,
    decimal Cash,
    DateOnly? CashDate,
    decimal CostBasis,
    decimal UnrealizedGain,
    decimal RealizedGain);

/// <param name="PriceDate">Date du cours retenu, null si le cours manque (valorisation au PRU, RG-11).</param>
public sealed record PositionValuation(
    Guid AccountId,
    Guid SecurityId,
    decimal Quantity,
    decimal AverageCost,
    decimal Price,
    DateOnly? PriceDate,
    bool MissingPrice,
    decimal MarketValue,
    decimal UnrealizedGain)
{
    public decimal CostBasis => Quantity * AverageCost;
}
