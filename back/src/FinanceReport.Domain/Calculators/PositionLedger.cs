using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FinanceReport.Domain.Exceptions;

namespace FinanceReport.Domain.Calculators;

/// <summary>
/// Rejeu incrémental des achats et ventes. Les mouvements doivent être appliqués dans l'ordre de rejeu
/// (<see cref="MovementOrdering.InReplayOrder"/>) ; versements et retraits sont ignorés (RG-22).
/// </summary>
public sealed class PositionLedger
{
    private readonly Dictionary<PositionKey, PositionState> _positions = [];

    public IReadOnlyDictionary<PositionKey, PositionState> Positions => _positions;

    public void Apply(Movement movement)
    {
        if (!movement.Type.IsTrade())
        {
            return;
        }

        var key = new PositionKey(movement.AccountId, RequireValue(movement.SecurityId, movement, "securityId"));
        var quantity = RequireValue(movement.Quantity, movement, "quantity");
        var unitPrice = RequireValue(movement.UnitPrice, movement, "unitPrice");
        var fees = movement.Fees ?? 0m;
        var current = _positions.GetValueOrDefault(key, PositionState.Empty);

        _positions[key] = movement.Type == MovementType.Achat
            ? Buy(current, quantity, unitPrice, fees)
            : Sell(current, quantity, unitPrice, fees, movement);
    }

    // RG-03 : PRU pondéré, frais d'achat inclus.
    private static PositionState Buy(PositionState p, decimal quantity, decimal unitPrice, decimal fees)
    {
        var newQuantity = p.Quantity + quantity;
        var averageCost = (p.Quantity * p.AverageCost + quantity * unitPrice + fees) / newQuantity;
        return p with { Quantity = newQuantity, AverageCost = averageCost };
    }

    // RG-04, RG-05 : la vente ne modifie pas le PRU ; la position soldée repart de zéro, le réalisé est conservé.
    private static PositionState Sell(PositionState p, decimal quantity, decimal unitPrice, decimal fees, Movement movement)
    {
        if (quantity > p.Quantity)
        {
            throw new InsufficientQuantityException(movement.Id, p.Quantity, movement.Date);
        }

        var realized = p.RealizedGain + quantity * (unitPrice - p.AverageCost) - fees;
        var remaining = p.Quantity - quantity;
        return new PositionState(remaining, remaining == 0m ? 0m : p.AverageCost, realized);
    }

    private static T RequireValue<T>(T? value, Movement movement, string field) where T : struct =>
        value ?? throw new InvalidOperationException($"Le mouvement {movement.Id} ({movement.Type}) n'a pas de {field}.");
}
