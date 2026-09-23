using System.Net;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>Production locale : le front compilé est servi par l'API (TS §1.2).</summary>
public sealed class SpaHostingTests : IDisposable
{
    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), "financereport-it", Guid.NewGuid().ToString("N"), "wwwroot");
    private readonly ApiFactory _factory;

    public SpaHostingTests()
    {
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), "<html><body><app-root></app-root></body></html>");
        File.WriteAllText(Path.Combine(_webRoot, "main.js"), "console.log('front');");
        _factory = new ApiFactory { WebRootPath = _webRoot };
    }

    public void Dispose()
    {
        _factory.Dispose();
        TestData.Delete(Path.GetDirectoryName(_webRoot)!);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/accounts")]
    [InlineData("/referentials/zones")]
    public async Task Front_routes_serve_index_html_without_authentication(string path)
    {
        var response = await _factory.CreateClient().GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("<app-root>");
    }

    [Fact]
    public async Task Static_files_are_served()
    {
        var response = await _factory.CreateClient().GetAsync("/main.js");

        (await response.Content.ReadAsStringAsync()).Should().Contain("front");
    }

    [Fact]
    public async Task Unknown_api_route_stays_a_json_404()
    {
        await (await _factory.CreateClient().GetAsync("/api/inconnu")).ShouldBeErrorAsync(HttpStatusCode.NotFound, "NOT_FOUND");
    }

    [Fact]
    public async Task Api_still_requires_a_token()
    {
        await (await _factory.CreateClient().GetAsync("/api/accounts")).ShouldBeErrorAsync(HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }
}
