namespace FinanceReport.Application.Exceptions;

/// <summary>Doublon, application déjà initialisée, valeur utilisée… (409 CONFLICT).</summary>
public sealed class ConflictException(string message, IReadOnlyDictionary<string, object?>? details = null)
    : AppException(message, details);
