using System.Net;
using System.Text;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>TC-TECH-06 : format d'erreur unique et codes HTTP (401 : voir TokenValidationTests).</summary>
public sealed class ErrorFormatTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ErrorFormatTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("validation", 400, "VALIDATION_ERROR", "amount")]
    [InlineData("notfound", 404, "NOT_FOUND", null)]
    [InlineData("conflict", 409, "CONFLICT", "usageCount")]
    [InlineData("quantity", 422, "INSUFFICIENT_QUANTITY", "availableQuantity")]
    [InlineData("locked", 423, "ACCOUNT_LOCKED", "retryAfterSeconds")]
    public async Task Business_exceptions_map_to_status_and_code(string kind, int status, string error, string? detail)
    {
        var response = await _client.GetAsync($"/api/test/throw/{kind}");

        await response.ShouldBeErrorAsync((HttpStatusCode)status, error, detail);
    }

    [Fact]
    public async Task Insufficient_quantity_exposes_available_quantity_and_conflicting_movement()
    {
        var body = await (await _client.GetAsync("/api/test/throw/quantity"))
            .ShouldBeErrorAsync((HttpStatusCode)422, "INSUFFICIENT_QUANTITY");

        body.GetProperty("message").GetString().Should().Be("Quantité insuffisante : 4.00000000 disponibles au 2026-09-15");
        body.GetProperty("details").GetProperty("availableQuantity").GetDecimal().Should().Be(4m);
        body.GetProperty("details").GetProperty("conflictingMovementId").GetGuid().Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Unexpected_exception_returns_500_without_internal_details()
    {
        var response = await _client.GetAsync("/api/test/throw/boom");

        var body = await response.ShouldBeErrorAsync(HttpStatusCode.InternalServerError, "INTERNAL_ERROR");
        var raw = body.GetRawText();
        raw.Should().NotContain("Détail interne").And.NotContain("InvalidOperationException").And.NotContain(" at ");
        body.TryGetProperty("details", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Unknown_route_returns_404_in_error_format()
    {
        await (await _client.GetAsync("/api/inconnu")).ShouldBeErrorAsync(HttpStatusCode.NotFound, "NOT_FOUND");
    }

    [Theory]
    [InlineData("{ \"amount\": \"abc\", \"date\": \"2026-09-15\" }", "amount")]
    [InlineData("{ \"amount\": 1, \"date\": \"15/09/2026\" }", "date")]
    [InlineData("{ pas du json", null)]
    public async Task Unreadable_body_returns_400_in_error_format(string json, string? field)
    {
        var response = await _client.PostAsync("/api/test/echo", new StringContent(json, Encoding.UTF8, "application/json"));

        await response.ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", field);
    }
}
