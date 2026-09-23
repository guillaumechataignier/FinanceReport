namespace FinanceReport.Application.Validators;

internal static class Messages
{
    public static string MaxDecimals(string field, int decimals) =>
        decimals == 0 ? $"{field} doit être un nombre entier." : $"{field} doit avoir au plus {decimals} décimale(s).";
}
