using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinanceReport.Infrastructure.Serialization;

public static class JsonDefaults
{
    /// <summary>
    /// camelCase, enums en majuscules (AccountType.Cto → "CTO", MovementType.Achat → "ACHAT"),
    /// DateOnly au format YYYY-MM-DD, décimaux en nombres JSON (TS §2.3).
    /// </summary>
    public static JsonSerializerOptions Options { get; } = Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    });

    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false));
        return options;
    }
}
