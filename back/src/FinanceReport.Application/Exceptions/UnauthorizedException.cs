namespace FinanceReport.Application.Exceptions;

/// <summary>Identifiants refusés ou jeton invalide (401 UNAUTHORIZED).</summary>
public sealed class UnauthorizedException(string message) : AppException(message);
