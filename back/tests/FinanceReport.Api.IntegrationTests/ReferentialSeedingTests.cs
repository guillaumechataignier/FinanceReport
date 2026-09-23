using System.Text.Json;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>TC-FUNC-30, au niveau des fichiers (les appels GET /api/referentials arrivent en phase 4).</summary>
public sealed class ReferentialSeedingTests : IDisposable
{
    private readonly string _dataPath = Path.Combine(Path.GetTempPath(), "financereport-it", Guid.NewGuid().ToString("N"));

    public void Dispose() => TestData.Delete(_dataPath);

    [Fact]
    public async Task Startup_on_empty_folder_creates_default_referentials_without_backup()
    {
        await StartAsync();

        Codes("zones").Should().Equal("EUROPE", "AMERIQUE_NORD", "ASIE_PACIFIQUE", "EMERGENTS", "MONDE");
        Codes("sectors").Should().Equal(
            "ENERGIE", "MATERIAUX", "INDUSTRIE", "CONSO_DISCRETIONNAIRE", "CONSO_BASE", "SANTE", "FINANCE",
            "TECHNOLOGIE", "COMMUNICATION", "SERVICES_PUBLICS", "IMMOBILIER", "DIVERSIFIE", "NON_APPLICABLE");
        Codes("institutions").Should().BeEmpty();
        Directory.Exists(Path.Combine(_dataPath, "backups")).Should().BeFalse();
    }

    [Fact]
    public async Task Existing_file_is_never_modified_and_missing_file_is_recreated()
    {
        await StartAsync();
        const string emptyZones = """{ "schemaVersion": 1, "items": [] }""";
        await File.WriteAllTextAsync(FilePath("zones"), emptyZones);
        File.Delete(FilePath("sectors"));

        await StartAsync();

        (await File.ReadAllTextAsync(FilePath("zones"))).Should().Be(emptyZones);
        Codes("sectors").Should().HaveCount(13);
    }

    private async Task StartAsync()
    {
        await using var factory = new ApiFactory(_dataPath, keepData: true);
        _ = factory.Services; // démarre l'hôte, donc l'initialisation des référentiels
    }

    private string FilePath(string entity) => Path.Combine(_dataPath, $"{entity}.json");

    private List<string?> Codes(string entity)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(FilePath(entity)));
        return document.RootElement.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("code").GetString())
            .ToList();
    }
}
