namespace FinanceReport.Application.Exceptions;

/// <summary>Ressource inexistante (404 NOT_FOUND).</summary>
public sealed class NotFoundException(string message) : AppException(message);
