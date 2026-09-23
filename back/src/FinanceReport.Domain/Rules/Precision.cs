using FinanceReport.Domain.Enums;

namespace FinanceReport.Domain.Rules;

/// <summary>Précisions de saisie (RG-02) et d'affichage (RG-29).</summary>
public static class Precision
{
    public const int Quantity = 8;
    public const int Amount = 2;
    public const int Percent = 2;

    /// <summary>Prix unitaire, cours et PRU : 8 décimales pour un support CRYPTO, 2 sinon.</summary>
    public static int Price(SecurityType type) => type == SecurityType.Crypto ? 8 : 2;
}
