using FluentValidation;
using AppValidationException = FinanceReport.Application.Exceptions.ValidationException;

namespace FinanceReport.Application.Validators;

public static class ValidatorExtensions
{
    /// <summary>Valide <paramref name="instance"/> et lève une <see cref="AppValidationException"/> (400) en cas d'erreur.</summary>
    public static void ValidateOrThrow<T>(this IValidator<T> validator, T instance)
    {
        var result = validator.Validate(instance);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        throw new AppValidationException(result.Errors[0].ErrorMessage, errors);
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) || char.IsLower(name[0]) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
