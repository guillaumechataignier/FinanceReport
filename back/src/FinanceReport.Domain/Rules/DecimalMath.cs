namespace FinanceReport.Domain.Rules;

public static class DecimalMath
{
    /// <summary>Nombre de décimales significatives : 1.50 → 1, 1.123 → 3, 100 → 0.</summary>
    public static int DecimalPlaces(decimal value)
    {
        // La division par 1.000…0 supprime les zéros non significatifs de l'échelle.
        var normalized = value / 1.0000000000000000000000000000m;
        return normalized.Scale;
    }

    /// <summary>Arrondi au demi supérieur (RG-29), réservé à la sortie de l'API.</summary>
    public static decimal Round(decimal value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    public static decimal? Round(decimal? value, int decimals) =>
        value is { } v ? Round(v, decimals) : null;
}
