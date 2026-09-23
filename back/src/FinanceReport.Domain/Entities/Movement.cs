using FinanceReport.Domain.Enums;

namespace FinanceReport.Domain.Entities;

/// <summary>
/// Opération datée. ACHAT/VENTE : SecurityId, Quantity, UnitPrice, Fees renseignés, Amount nul.
/// VERSEMENT/RETRAIT : Amount renseigné, les autres champs nuls (FS §3.4).
/// </summary>
public sealed record Movement
{
    public required Guid Id { get; init; }
    public required MovementType Type { get; init; }
    public required DateOnly Date { get; init; }
    public required Guid AccountId { get; init; }
    public Guid? SecurityId { get; init; }
    public decimal? Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? Fees { get; init; }
    public decimal? Amount { get; init; }

    /// <summary>Ordre de création, attribué une fois (max + 1). Départage deux mouvements de même date (RG-10).</summary>
    public required long Sequence { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
