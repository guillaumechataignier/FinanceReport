using System.Text.Json;
using FinanceReport.Infrastructure.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FinanceReport.Api.Errors;

public static class ErrorResponses
{
    private static readonly JsonSerializerOptions SerializerOptions =
        JsonDefaults.Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static Task WriteAsync(HttpContext context, int statusCode, string error, string message, object? details = null)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new ErrorResponse(error, message, details), SerializerOptions);
    }

    /// <summary>Réponse aux statuts sans corps (route inconnue, méthode non autorisée…).</summary>
    public static Task WriteForStatusCodeAsync(HttpContext context)
    {
        var status = context.Response.StatusCode;
        var (error, message) = status switch
        {
            StatusCodes.Status401Unauthorized => (ErrorCodes.Unauthorized, "Authentification requise."),
            StatusCodes.Status404NotFound => (ErrorCodes.NotFound, "Ressource inexistante."),
            StatusCodes.Status405MethodNotAllowed => (ErrorCodes.NotFound, "Méthode non disponible sur cette ressource."),
            StatusCodes.Status415UnsupportedMediaType => (ErrorCodes.Validation, "Le corps de la requête doit être au format JSON."),
            >= 500 => (ErrorCodes.Internal, "Erreur interne."),
            _ => (ErrorCodes.Validation, "Requête invalide."),
        };
        return WriteAsync(context, status, error, message);
    }

    /// <summary>Erreurs de liaison de modèle (JSON mal formé, valeur d'enum inconnue…) au format unique.</summary>
    public static IActionResult FromModelState(ActionContext context)
    {
        var details = context.ModelState
            .Where(e => e.Value is { ValidationState: ModelValidationState.Invalid })
            .Select(e => FieldName(e.Key))
            .Distinct()
            .ToDictionary(field => field, _ => new[] { "Valeur invalide." });

        return new ObjectResult(new ErrorResponse(ErrorCodes.Validation, "Requête invalide : vérifiez le format des champs.", details))
        {
            StatusCode = StatusCodes.Status400BadRequest,
        };
    }

    // "$.amount" → "amount" ; "$" ou "request" (corps absent ou illisible) → "body".
    private static string FieldName(string key)
    {
        var name = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
        return name is "" or "$" or "request" ? "body" : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
