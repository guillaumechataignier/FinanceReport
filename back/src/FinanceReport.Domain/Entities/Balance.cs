namespace FinanceReport.Domain.Entities;

/// <summary>Solde d'un compte à une date. Clé unique : (AccountId, Date).</summary>
public sealed record Balance
{
    public required Guid AccountId { get; init; }
    public required DateOnly Date { get; init; }
    public required decimal Amount { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
