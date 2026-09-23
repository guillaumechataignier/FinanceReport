namespace FinanceReport.Domain.Calculators;

/// <summary>Couple (compte titres, support).</summary>
public readonly record struct PositionKey(Guid AccountId, Guid SecurityId);
