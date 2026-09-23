using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>TC-TECH-05, étapes 2 et 3 (l'écoute sur localhost se vérifie manuellement).</summary>
public sealed class CorsTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("http://localhost:4200", true)]
    [InlineData("http://evil.local", false)]
    public async Task Preflight_is_allowed_only_for_the_front_origin(string origin, bool allowed)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");

        var response = await _factory.CreateClient().SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().Be(allowed);
    }
}
