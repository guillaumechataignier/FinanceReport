using FinanceReport.Domain.Rules;
using FluentValidation;

namespace FinanceReport.Application.Validators;

public static class DecimalRules
{
    /// <summary>RG-02 : rejette une saisie qui dépasse la précision autorisée.</summary>
    public static IRuleBuilderOptions<T, decimal?> MaxDecimals<T>(this IRuleBuilder<T, decimal?> rule, int decimals, string label) =>
        rule.Must(v => v is null || DecimalMath.DecimalPlaces(v.Value) <= decimals)
            .WithMessage(Messages.MaxDecimals(label, decimals));
}
