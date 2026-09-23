namespace FinanceReport.Domain.Enums;

/// <summary>Type de compte (liste fixe, FS §3.2). Sérialisé en majuscules : COURANT, LIVRET…</summary>
public enum AccountType
{
    Courant,
    Livret,
    Autre,
    Cto,
    Pea,
    Crypto,
}
