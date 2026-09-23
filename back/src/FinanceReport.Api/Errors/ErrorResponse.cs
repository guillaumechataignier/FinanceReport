using System.Text.Json.Serialization;

namespace FinanceReport.Api.Errors;

/// <summary>Format d'erreur unique de l'API (FS §4.1).</summary>
public sealed record ErrorResponse(
    string Error,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Details = null);
