using System.Text.Json;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>Lecture de <c>snapshots.json</c> : patrimoine total et valeur de chaque compte par date.</summary>
internal static class SnapshotFile
{
    public static async Task<Dictionary<string, JsonElement>> ReadAsync(ApiFactory factory)
    {
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(factory.FilePath("snapshots")));
        return document.RootElement.GetProperty("items").EnumerateArray()
            .ToDictionary(s => s.GetProperty("date").GetString()!, s => s.Clone());
    }

    public static decimal AccountValue(this JsonElement snapshot, Guid accountId) =>
        snapshot.GetProperty("accounts").EnumerateArray()
            .Single(a => a.GetProperty("accountId").GetGuid() == accountId)
            .GetProperty("value").GetDecimal();

    public static decimal Total(this JsonElement snapshot) => snapshot.GetProperty("totalNetWorth").GetDecimal();
}
