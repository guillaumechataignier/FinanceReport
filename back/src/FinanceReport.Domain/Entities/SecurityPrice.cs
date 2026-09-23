namespace FinanceReport.Domain.Entities;

/// <summary>Cours d'un support à une date. Clé unique : (SecurityId, Date).</summary>
public sealed record SecurityPrice
{
    public required Guid SecurityId { get; init; }
    public required DateOnly Date { get; init; }
    public required decimal Price { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
