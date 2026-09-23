using FinanceReport.Api.Errors;
using FinanceReport.Application.Exceptions;
using FinanceReport.Domain.Exceptions;

namespace FinanceReport.Api.Middleware;

/// <summary>Convertit les exceptions en réponses au format unique ; aucune stack trace n'est exposée (FS §4.1).</summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (status, error, message, details) = Map(exception);
            if (status >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Erreur non gérée sur {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            context.Response.Clear();
            await ErrorResponses.WriteAsync(context, status, error, message, details);
        }
    }

    private static (int Status, string Error, string Message, object? Details) Map(Exception exception) => exception switch
    {
        ValidationException e => (StatusCodes.Status400BadRequest, ErrorCodes.Validation, e.Message, e.Details),
        UnauthorizedException e => (StatusCodes.Status401Unauthorized, ErrorCodes.Unauthorized, e.Message, e.Details),
        NotFoundException e => (StatusCodes.Status404NotFound, ErrorCodes.NotFound, e.Message, e.Details),
        ConflictException e => (StatusCodes.Status409Conflict, ErrorCodes.Conflict, e.Message, e.Details),
        InsufficientQuantityException e => (
            StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.InsufficientQuantity,
            e.Message,
            new Dictionary<string, object?> { ["availableQuantity"] = e.AvailableQuantity, ["conflictingMovementId"] = e.MovementId }),
        AccountLockedException e => (StatusCodes.Status423Locked, ErrorCodes.AccountLocked, e.Message, e.Details),
        BadHttpRequestException => (
            StatusCodes.Status400BadRequest,
            ErrorCodes.Validation,
            "Requête invalide ou trop volumineuse (1 Mo maximum).",
            null),
        _ => (StatusCodes.Status500InternalServerError, ErrorCodes.Internal, "Erreur interne. Consultez les journaux de l'application.", null),
    };
}
