using System.Net;
using FinanceReport.Application.Dtos;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

public sealed class PriceApiTests : IAsyncLifetime
{
    private readonly ApiFactory _factory = new();
    private HttpClient _client = null!;
    private ReferenceDataSet _data = null!;

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
        _data = await ReferenceDataSet.LoadAsync(_client);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact] // UC-10 : upsert sur (support, date), historique décroissant
    public async Task Price_is_upserted_listed_and_deleted()
    {
        await _client.PutJsonAsync<PriceDto>($"/api/securities/{_data.S1}/prices/2026-09-10", new { price = 110m });
        await _client.PutJsonAsync<PriceDto>($"/api/securities/{_data.S1}/prices/2026-09-10", new { price = 111.50m });

        var history = await _client.GetJsonAsync<List<PriceDto>>($"/api/securities/{_data.S1}/prices");
        history.Select(p => (p.Date, p.Price)).Should().Equal(
            (new DateOnly(2026, 9, 22), 115.00m),
            (new DateOnly(2026, 9, 10), 111.50m));

        (await _client.DeleteAsync($"/api/securities/{_data.S1}/prices/2026-09-22")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var s1 = await _client.GetJsonAsync<SecurityDto>($"/api/securities/{_data.S1}");
        s1.LastPrice.Should().Be(111.50m);
        (await _client.GetJsonAsync<AccountDto>($"/api/accounts/{_data.A}")).CurrentValue.Should().Be(9m * 111.50m + 200m);
        await (await _client.DeleteAsync($"/api/securities/{_data.S1}/prices/2026-09-22")).ShouldBeErrorAsync(HttpStatusCode.NotFound, "NOT_FOUND");
    }

    [Theory] // TC-FUNC-02 (e, f), UC-10
    [InlineData("S1", "104.125", HttpStatusCode.BadRequest)]
    [InlineData("S2", "58000.12345678", HttpStatusCode.OK)]
    [InlineData("S1", "0", HttpStatusCode.BadRequest)]
    [InlineData("S1", "-1", HttpStatusCode.BadRequest)]
    public async Task Price_must_be_positive_with_security_precision(string security, string price, HttpStatusCode expected)
    {
        var id = security == "S1" ? _data.S1 : _data.S2;

        var response = await _client.PutAsync(
            $"/api/securities/{id}/prices/2026-09-20",
            new { price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture) });

        response.StatusCode.Should().Be(expected);
        if (expected == HttpStatusCode.BadRequest)
        {
            await response.ShouldBeErrorAsync(expected, "VALIDATION_ERROR", "price");
        }
    }

    [Fact] // TC-FUNC-21 (cours)
    public async Task Future_price_is_rejected()
    {
        await (await _client.PutAsync($"/api/securities/{_data.S1}/prices/2026-09-24", new { price = 100m }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "date");
        (await _client.PutAsync($"/api/securities/{_data.S1}/prices/2026-09-23", new { price = 100m })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unknown_security_is_404()
    {
        await (await _client.GetAsync($"/api/securities/{Guid.NewGuid()}/prices")).ShouldBeErrorAsync(HttpStatusCode.NotFound, "NOT_FOUND");
    }
}
