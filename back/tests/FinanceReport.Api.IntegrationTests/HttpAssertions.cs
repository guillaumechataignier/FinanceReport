using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

internal static class HttpAssertions
{
    /// <summary>Vérifie le statut et le format d'erreur unique ; renvoie le corps pour d'autres vérifications.</summary>
    public static async Task<JsonElement> ShouldBeErrorAsync(
        this HttpResponseMessage response,
        HttpStatusCode status,
        string error,
        string? detailField = null)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var root = JsonDocument.Parse(body).RootElement.Clone();
        root.GetProperty("error").GetString().Should().Be(error);
        root.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        if (detailField is not null)
        {
            root.GetProperty("details").TryGetProperty(detailField, out _).Should().BeTrue(body);
        }

        return root;
    }
}
