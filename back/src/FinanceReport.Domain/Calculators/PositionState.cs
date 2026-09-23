namespace FinanceReport.Domain.Calculators;

/// <summary>
/// État d'un couple (compte, support) après rejeu de ses mouvements. Valeurs en précision complète (RG-29).
/// Une position soldée garde Quantity = 0, AverageCost = 0 et son cumul réalisé (RG-04).
/// </summary>
public sealed record PositionState(decimal Quantity, decimal AverageCost, decimal RealizedGain)
{
    public static readonly PositionState Empty = new(0m, 0m, 0m);

    public bool IsOpen => Quantity > 0m;

    /// <summary>Coût d'acquisition de la quantité détenue (Q × PRU).</summary>
    public decimal CostBasis => Quantity * AverageCost;
}
