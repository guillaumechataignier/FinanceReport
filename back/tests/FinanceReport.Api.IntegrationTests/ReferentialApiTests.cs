using System.Net;
using FinanceReport.Application.Dtos;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

public sealed class ReferentialApiTests : IAsyncLifetime
{
    private readonly ApiFactory _factory = new();
    private HttpClient _client = null!;
    private ReferenceDataSet _data = null!;

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
        _data = await ReferenceDataSet.LoadAsync(_factory, _client);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact] // TC-FUNC-30, étape 2
    public async Task Default_referentials_are_listed_sorted_by_label()
    {
        var zones = await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/zones");
        var sectors = await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/sectors");

        zones.Select(z => z.Label).Should().Equal("Amérique du Nord", "Asie-Pacifique", "Émergents", "Europe", "Monde");
        sectors.Should().HaveCount(13);
        zones.Single(z => z.Code == "MONDE").UsageCount.Should().Be(2);
        sectors.Single(s => s.Code == "DIVERSIFIE").UsageCount.Should().Be(1);
        sectors.Single(s => s.Code == "NON_APPLICABLE").UsageCount.Should().Be(1);
    }

    [Fact] // TC-TECH-06 (404 référentiel inconnu), TC-TECH-01 étape 5
    public async Task Unknown_kind_is_404_and_anonymous_call_is_401()
    {
        await (await _client.GetAsync("/api/referentials/pays")).ShouldBeErrorAsync(HttpStatusCode.NotFound, "NOT_FOUND");
        await (await _factory.CreateClient().GetAsync("/api/referentials/zones")).ShouldBeErrorAsync(HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact] // TC-FUNC-28 (les contrôles du tableau de bord arrivent en phase 6)
    public async Task Zones_and_sectors_lifecycle()
    {
        // 1. Création : une sauvegarde de sectors.json est prise.
        var luxe = await _client.PostJsonAsync<ReferentialItemDto>("/api/referentials/sectors", new { code = "LUXE", label = "Luxe" });
        luxe.UsageCount.Should().Be(0);
        Directory.GetFiles(Path.Combine(_factory.DataPath, "backups", "sectors")).Should().NotBeEmpty();

        // 2. Format du code, libellé en double (casse ignorée), code en double.
        await (await _client.PostAsync("/api/referentials/sectors", new { code = "luxe", label = "Luxe 2" }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "code");
        await (await _client.PostAsync("/api/referentials/sectors", new { code = "LUXE_2", label = "luxe" }))
            .ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT");
        await (await _client.PostAsync("/api/referentials/sectors", new { code = "LUXE", label = "Autre" }))
            .ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT");

        // 3. Code modifiable tant que la valeur n'est pas utilisée.
        var luxury = await _client.PutJsonAsync<ReferentialItemDto>($"/api/referentials/sectors/{luxe.Id}", new { code = "LUXURY", label = "Luxe" });
        luxury.Code.Should().Be("LUXURY");

        // 4. Nouveau libellé repris partout, sans recalcul des snapshots.
        var monde = (await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/zones")).Single(z => z.Code == "MONDE");
        var snapshotsBefore = await File.ReadAllTextAsync(_factory.FilePath("snapshots"));
        await _client.PutJsonAsync<ReferentialItemDto>($"/api/referentials/zones/{monde.Id}", new { code = "MONDE", label = "Monde entier" });
        var s1 = (await _client.GetJsonAsync<List<SecurityDto>>("/api/securities")).Single(s => s.Id == _data.S1);
        s1.ZoneLabel.Should().Be("Monde entier");
        (await File.ReadAllTextAsync(_factory.FilePath("snapshots"))).Should().Be(snapshotsBefore);

        // 5. Code d'une valeur utilisée figé.
        var frozen = await _client.PutAsync($"/api/referentials/zones/{monde.Id}", new { code = "WORLD", label = "Monde entier" });
        var error = await frozen.ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT");
        error.GetProperty("details").GetProperty("usageCount").GetInt32().Should().Be(2);

        // 6. Suppression : refusée si utilisée, sinon 204.
        await (await _client.DeleteAsync($"/api/referentials/zones/{monde.Id}")).ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT");
        (await _client.DeleteAsync($"/api/referentials/sectors/{luxe.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 7. Archivage : absent de la liste par défaut, présent avec includeArchived ; S1 conserve la valeur.
        var diversifie = (await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/sectors")).Single(s => s.Code == "DIVERSIFIE");
        await _client.PostJsonAsync<ReferentialItemDto>($"/api/referentials/sectors/{diversifie.Id}/archive");
        (await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/sectors")).Should().NotContain(s => s.Code == "DIVERSIFIE");
        (await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/sectors?includeArchived=true"))
            .Single(s => s.Code == "DIVERSIFIE").Archived.Should().BeTrue();
        var s1AfterArchive = await _client.GetJsonAsync<SecurityDto>($"/api/securities/{_data.S1}");
        s1AfterArchive.Sector.Should().Be("DIVERSIFIE");
        s1AfterArchive.SectorArchived.Should().BeTrue();

        // 8. Désarchivage.
        var restored = await _client.PostJsonAsync<ReferentialItemDto>($"/api/referentials/sectors/{diversifie.Id}/unarchive");
        restored.Archived.Should().BeFalse();
    }

    [Fact] // TC-FUNC-29 (étapes API)
    public async Task Institution_is_mandatory_on_accounts()
    {
        // 1. Utilisation, comptes archivés compris.
        var institutions = await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/institutions");
        institutions.Single(i => i.Id == _data.E1).UsageCount.Should().Be(2);
        institutions.Single(i => i.Id == _data.E2).UsageCount.Should().Be(1);
        institutions.Should().OnlyContain(i => i.Code == null);

        // 2 et 3. Établissement absent, nul ou inexistant.
        await (await _client.PostAsync("/api/accounts", new { name = "D", type = "CTO" }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "institutionId");
        await (await _client.PostAsync("/api/accounts", new { name = "D", type = "CTO", institutionId = (Guid?)null }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "institutionId");
        await (await _client.PostAsync("/api/accounts", new { name = "D", type = "CTO", institutionId = Guid.NewGuid() }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "institutionId");

        // 4. Établissement valide.
        var d = await _client.PostJsonAsync<AccountDto>("/api/accounts", new { name = "D", type = "CTO", institutionId = _data.E2 });
        d.InstitutionName.Should().Be("Banque Test");
        (await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/institutions"))
            .Single(i => i.Id == _data.E2).UsageCount.Should().Be(2);

        // 5. Suppression d'un établissement utilisé.
        var deletion = await _client.DeleteAsync($"/api/referentials/institutions/{_data.E1}");
        (await deletion.ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT"))
            .GetProperty("details").GetProperty("usageCount").GetInt32().Should().Be(2);

        // 6. Établissement archivé : refusé pour un nouveau compte.
        await _client.PostJsonAsync<ReferentialItemDto>($"/api/referentials/institutions/{_data.E1}/archive");
        await (await _client.PostAsync("/api/accounts", new { name = "F", type = "CTO", institutionId = _data.E1 }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "institutionId");

        // 7. Établissement archivé inchangé : conservé.
        var a = await _client.PutJsonAsync<AccountDto>($"/api/accounts/{_data.A}", new { name = "PEA Test renommé", type = "PEA", institutionId = _data.E1 });
        a.InstitutionId.Should().Be(_data.E1);
        a.InstitutionArchived.Should().BeTrue();

        // 8. Établissement retiré.
        await (await _client.PutAsync($"/api/accounts/{_data.A}", new { name = "PEA Test", type = "PEA", institutionId = (Guid?)null }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "institutionId");

        // 9. Code interdit pour un établissement.
        await (await _client.PostAsync("/api/referentials/institutions", new { code = "BOURSO", label = "Bourse Direct" }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "code");
    }

    [Theory]
    [InlineData("", "label")]
    [InlineData("   ", "label")]
    public async Task Label_is_required(string label, string field)
    {
        await (await _client.PostAsync("/api/referentials/zones", new { code = "AFRIQUE", label }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", field);
    }

    [Fact]
    public async Task Label_is_limited_to_60_characters()
    {
        (await _client.PostAsync("/api/referentials/zones", new { code = "Z60", label = new string('a', 60) }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        await (await _client.PostAsync("/api/referentials/zones", new { code = "Z61", label = new string('b', 61) }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "label");
    }
}
