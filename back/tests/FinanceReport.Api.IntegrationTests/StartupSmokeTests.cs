using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinanceReport.Api.IntegrationTests;

public class StartupSmokeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Api_starts_and_answers_404_on_unknown_route()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/unknown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
