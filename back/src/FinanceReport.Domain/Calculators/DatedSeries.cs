namespace FinanceReport.Domain.Calculators;

public readonly record struct DatedValue(DateOnly Date, decimal Value);

/// <summary>
/// Valeurs datées groupées par clé et triées par date (cours par support, soldes par compte),
/// pour retrouver la dernière valeur de date ≤ D par recherche dichotomique (RG-23).
/// </summary>
public sealed class DatedSeries<TKey> where TKey : notnull
{
    private readonly Dictionary<TKey, (DateOnly[] Dates, decimal[] Values)> _series;

    private DatedSeries(Dictionary<TKey, (DateOnly[] Dates, decimal[] Values)> series) => _series = series;

    public static DatedSeries<TKey> Empty { get; } = new([]);

    public static DatedSeries<TKey> Create<T>(
        IEnumerable<T> items,
        Func<T, TKey> keySelector,
        Func<T, DateOnly> dateSelector,
        Func<T, decimal> valueSelector)
    {
        var series = items
            .GroupBy(keySelector)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var sorted = g.OrderBy(dateSelector).ToArray();
                    return (sorted.Select(dateSelector).ToArray(), sorted.Select(valueSelector).ToArray());
                });
        return new DatedSeries<TKey>(series);
    }

    public DatedValue? Latest(TKey key, DateOnly asOf)
    {
        if (!_series.TryGetValue(key, out var s))
        {
            return null;
        }

        var index = Array.BinarySearch(s.Dates, asOf);
        if (index < 0)
        {
            // ~index est l'indice du premier élément postérieur à asOf.
            index = ~index - 1;
        }

        return index >= 0 ? new DatedValue(s.Dates[index], s.Values[index]) : null;
    }
}
