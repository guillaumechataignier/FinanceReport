using FinanceReport.Application.Exceptions;

namespace FinanceReport.Application.Services;

internal static class DateRules
{
    /// <summary>RG-25 : aucune date de mouvement, de solde ou de cours postérieure au jour.</summary>
    public static void EnsureNotFuture(DateOnly date, DateOnly today, string field = "date")
    {
        if (date > today)
        {
            throw ValidationException.ForField(field, "La date ne peut pas être postérieure à la date du jour.");
        }
    }
}
