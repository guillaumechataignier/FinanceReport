namespace FinanceReport.Domain.Enums;

public static class AccountTypeExtensions
{
    /// <summary>TITRES pour CTO, PEA et CRYPTO ; ESPECES pour les autres (FS §3.2).</summary>
    public static AccountCategory GetCategory(this AccountType type) => type switch
    {
        AccountType.Cto or AccountType.Pea or AccountType.Crypto => AccountCategory.Titres,
        _ => AccountCategory.Especes,
    };

    public static bool IsSecuritiesAccount(this AccountType type) => type.GetCategory() == AccountCategory.Titres;
}
