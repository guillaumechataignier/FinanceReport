namespace FinanceReport.Domain.Entities;

/// <summary>Photographie du patrimoine à une date (au plus une par jour, RG-14).</summary>
public sealed record Snapshot
{
    public required DateOnly Date { get; init; }
    public required decimal TotalNetWorth { get; init; }
    public required DateTimeOffset ComputedAt { get; init; }
    public required IReadOnlyList<SnapshotAccount> Accounts { get; init; }
    public required IReadOnlyList<SnapshotPosition> Positions { get; init; }
}

public sealed record SnapshotAccount
{
    public required Guid AccountId { get; init; }
    public required decimal Value { get; init; }
    public required decimal Cash { get; init; }
}

public sealed record SnapshotPosition
{
    public required Guid AccountId { get; init; }
    public required Guid SecurityId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal AverageCost { get; init; }
    public required decimal Price { get; init; }
    public required bool MissingPrice { get; init; }
    public required decimal MarketValue { get; init; }
    public required decimal UnrealizedGain { get; init; }
}
