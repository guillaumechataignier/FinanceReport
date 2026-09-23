using System.Net;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

public sealed class StartupSmokeTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose()
    {
        _factory.Dispose();
        TestData.Delete(_factory.DataPath);
    }

    [Fact]
    public async Task Api_starts_and_answers_404_on_unknown_route()
    {
        var response = await _factory.CreateClient().GetAsync("/api/unknown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
