namespace FinanceReport.Application.Exceptions;

/// <summary>Connexion bloquée après 5 échecs (423 ACCOUNT_LOCKED, RG-18).</summary>
public sealed class AccountLockedException(int retryAfterSeconds)
    : AppException(
        $"Connexion bloquée après 5 échecs. Réessayez dans {Math.Ceiling(retryAfterSeconds / 60.0)} minute(s).",
        new Dictionary<string, object?> { ["retryAfterSeconds"] = retryAfterSeconds })
{
    public int RetryAfterSeconds { get; } = retryAfterSeconds;
}
