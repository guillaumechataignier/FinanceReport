using FinanceReport.Domain.Enums;

namespace FinanceReport.Domain.Entities;

public sealed record Security
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required SecurityType Type { get; init; }

    /// <summary>Code d'une valeur du référentiel des zones.</summary>
    public required string Zone { get; init; }

    /// <summary>Code d'une valeur du référentiel des secteurs.</summary>
    public required string Sector { get; init; }

    public bool Archived { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
