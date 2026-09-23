using System.Net;
using FinanceReport.Application.Dtos;
using FinanceReport.Domain.Enums;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

public sealed class SecurityApiTests : IAsyncLifetime
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

    [Fact] // Ajout validé : dernier cours sur la liste des supports
    public async Task List_shows_labels_and_last_price()
    {
        var securities = await _client.GetJsonAsync<List<SecurityDto>>("/api/securities");

        securities.Select(s => s.Name).Should().Equal("Bitcoin", "ETF Monde");
        var s1 = securities.Single(s => s.Id == _data.S1);
        s1.Type.Should().Be(SecurityType.Etf);
        s1.ZoneLabel.Should().Be("Monde");
        s1.SectorLabel.Should().Be("Diversifié");
        s1.LastPrice.Should().Be(115.00m);
        s1.LastPriceDate.Should().Be(new DateOnly(2026, 9, 22));
        securities.Single(s => s.Id == _data.S2).LastPrice.Should().BeNull();
    }

    [Fact] // TC-FUNC-22
    public async Task Security_code_is_unique_ignoring_case()
    {
        await (await _client.PostAsync("/api/securities", new { name = "Doublon", code = "lu1681043599", type = "ETF", zone = "MONDE", sector = "DIVERSIFIE" }))
            .ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT");
    }

    [Fact] // TC-FUNC-20, étapes 1 à 5
    public async Task Zone_and_sector_must_exist_and_be_active_when_chosen()
    {
        await (await Create("AFR", zone: "AFRIQUE")).ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "zone");
        await (await Create("LUX", sector: "luxe")).ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "sector");

        var zones = await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/zones");
        await _client.PostJsonAsync<ReferentialItemDto>($"/api/referentials/zones/{zones.Single(z => z.Code == "EUROPE").Id}/archive");
        await (await Create("EUR", zone: "EUROPE")).ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "zone");

        await _client.PostJsonAsync<ReferentialItemDto>($"/api/referentials/zones/{zones.Single(z => z.Code == "MONDE").Id}/archive");
        var renamed = await _client.PutJsonAsync<SecurityDto>(
            $"/api/securities/{_data.S1}",
            new { name = "ETF Monde renommé", code = "LU1681043599", type = "ETF", zone = "MONDE", sector = "DIVERSIFIE" });
        renamed.Zone.Should().Be("MONDE");
        renamed.ZoneArchived.Should().BeTrue();

        await (await _client.PutAsync(
                $"/api/securities/{_data.S1}",
                new { name = "ETF Monde", code = "LU1681043599", type = "ETF", zone = "EUROPE", sector = "DIVERSIFIE" }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "zone");
    }

    [Fact] // TC-FUNC-10, étapes 1, 2 et 4 (l'achat sur un support archivé est testé en phase 5)
    public async Task Used_security_can_only_be_archived()
    {
        var deletion = await _client.DeleteAsync($"/api/securities/{_data.S1}");
        (await deletion.ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT"))
            .GetProperty("details").GetProperty("movementCount").GetInt32().Should().Be(3);

        var archived = await _client.PostJsonAsync<SecurityDto>($"/api/securities/{_data.S1}/archive");
        archived.Archived.Should().BeTrue();
        (await _client.GetJsonAsync<List<SecurityDto>>("/api/securities")).Should().NotContain(s => s.Id == _data.S1);
        (await _client.GetJsonAsync<List<SecurityDto>>("/api/securities?includeArchived=true")).Should().Contain(s => s.Id == _data.S1);

        var s3 = await _client.PostJsonAsync<SecurityDto>("/api/securities", new { name = "S3", code = "S3", type = "ACTION", zone = "EUROPE", sector = "SANTE" });
        (await _client.DeleteAsync($"/api/securities/{s3.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await (await _client.GetAsync($"/api/securities/{s3.Id}")).ShouldBeErrorAsync(HttpStatusCode.NotFound, "NOT_FOUND");
    }

    [Fact]
    public async Task Required_fields_are_checked()
    {
        await (await _client.PostAsync("/api/securities", new { name = "", code = "X", type = "ETF", zone = "MONDE", sector = "DIVERSIFIE" }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "name");
        await (await _client.PostAsync("/api/securities", new { name = "X", code = "X", zone = "MONDE", sector = "DIVERSIFIE" }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "type");
        await (await _client.PostAsync("/api/securities", new { name = "X", code = "X", type = "ETF", sector = "DIVERSIFIE" }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "zone");
    }

    private Task<HttpResponseMessage> Create(string code, string zone = "MONDE", string sector = "DIVERSIFIE") =>
        _client.PostAsync("/api/securities", new { name = code, code, type = "ACTION", zone, sector });
}
