using FinanceReport.Application.Exceptions;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Rules;

namespace FinanceReport.Application.Services;

internal static class PriceRules
{
    /// <summary>RG-02 : 2 décimales pour un prix ou un cours, 8 pour un support CRYPTO.</summary>
    public static void EnsurePrecision(decimal price, Security security, string field, string label)
    {
        var decimals = Precision.Price(security.Type);
        if (DecimalMath.DecimalPlaces(price) > decimals)
        {
            throw ValidationException.ForField(field, $"{label} doit avoir au plus {decimals} décimale(s) pour ce support.");
        }
    }
}
