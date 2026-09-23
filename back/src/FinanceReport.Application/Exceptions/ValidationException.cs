namespace FinanceReport.Application.Exceptions;

/// <summary>Saisie invalide (400 VALIDATION_ERROR). Les détails associent chaque champ à ses messages.</summary>
public sealed class ValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
    : AppException(message, errors.ToDictionary(e => e.Key, e => (object?)e.Value))
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public static ValidationException ForField(string field, string message) =>
        new(message, new Dictionary<string, string[]> { [field] = [message] });
}
