namespace FinanceReport.Domain.Entities;

/// <summary>Valeur d'un référentiel administrable (zone, secteur, établissement).</summary>
public sealed record ReferentialItem
{
    public required Guid Id { get; init; }

    /// <summary>Code stable, figé dès que la valeur est utilisée (RG-30). Toujours null pour un établissement.</summary>
    public string? Code { get; init; }

    public required string Label { get; init; }
    public bool Archived { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
