namespace FinanceReport.Application.Exceptions;

/// <summary>Erreur métier attendue, convertie par l'API au format <c>{ error, message, details }</c> (FS §4.1).</summary>
public abstract class AppException(string message, IReadOnlyDictionary<string, object?>? details = null) : Exception(message)
{
    public IReadOnlyDictionary<string, object?>? Details { get; } = details;
}
