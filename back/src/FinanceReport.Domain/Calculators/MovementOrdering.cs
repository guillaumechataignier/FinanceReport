using FinanceReport.Domain.Entities;

namespace FinanceReport.Domain.Calculators;

public static class MovementOrdering
{
    /// <summary>Ordre de rejeu : date croissante, puis ordre de création (RG-10).</summary>
    public static IOrderedEnumerable<Movement> InReplayOrder(this IEnumerable<Movement> movements) =>
        movements.OrderBy(m => m.Date).ThenBy(m => m.Sequence);
}
