using FinanceReport.Domain.Entities;

namespace FinanceReport.Domain.Calculators;

public static class PositionCalculator
{
    /// <summary>
    /// Rejoue les achats et ventes de date ≤ <paramref name="asOf"/> (TS §3.1). Les positions soldées sont incluses
    /// (Quantity = 0) pour conserver leur plus-value réalisée.
    /// </summary>
    /// <exception cref="Exceptions.InsufficientQuantityException">Une vente dépasse la quantité détenue à sa date.</exception>
    public static IReadOnlyDictionary<PositionKey, PositionState> Compute(IEnumerable<Movement> movements, DateOnly asOf)
    {
        var ledger = new PositionLedger();
        foreach (var movement in movements.Where(m => m.Date <= asOf).InReplayOrder())
        {
            ledger.Apply(movement);
        }

        return ledger.Positions;
    }
}
