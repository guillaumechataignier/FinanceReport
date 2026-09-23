using System.Net;
using System.Net.Http.Headers;
using FinanceReport.Domain.Entities;
using FinanceReport.Infrastructure.Authentication;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>TC-TECH-01 sur un endpoint protégé de test (les endpoints métier arrivent en phase 4).</summary>
public sealed class TokenValidationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Request_without_token_is_rejected()
    {
        var response = await _factory.CreateClient().GetAsync("/api/test/protected");

        await response.ShouldBeErrorAsync(HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task Request_with_valid_token_is_accepted_until_expiry()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        (await client.GetAsync("/api/test/protected")).StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.Time.Advance(new TimeSpan(7, 59, 0));
        (await client.GetAsync("/api/test/protected")).StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.Time.Advance(TimeSpan.FromMinutes(2)); // + 8 h 01
        await (await client.GetAsync("/api/test/protected")).ShouldBeErrorAsync(HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task Token_signed_with_another_key_is_rejected()
    {
        var client = _factory.CreateClient();
        await ApiFactory.SetupAndLoginAsync(client);
        var tokens = new JwtTokenService(new Infrastructure.Time.ParisClock(_factory.Time));
        var (forged, _) = tokens.Issue(new Credentials
        {
            Username = ApiFactory.Username,
            PasswordHash = "",
            JwtSigningKey = tokens.GenerateSigningKey(),
            CreatedAt = _factory.Time.GetUtcNow(),
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        await (await client.GetAsync("/api/test/protected")).ShouldBeErrorAsync(HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task Malformed_token_is_rejected()
    {
        var client = _factory.CreateClient();
        await ApiFactory.SetupAndLoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "pas.un.jeton");

        await (await client.GetAsync("/api/test/protected")).ShouldBeErrorAsync(HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }
}
