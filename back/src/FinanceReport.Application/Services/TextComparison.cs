using System.Globalization;

namespace FinanceReport.Application.Services;

internal static class TextComparison
{
    /// <summary>Tri d'affichage en français, casse et accents compris dans l'ordre alphabétique.</summary>
    public static readonly StringComparer French = StringComparer.Create(CultureInfo.GetCultureInfo("fr-FR"), ignoreCase: true);

    /// <summary>Unicité d'un nom ou d'un libellé : insensible à la casse, après suppression des espaces de bord.</summary>
    public static bool SameText(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}
