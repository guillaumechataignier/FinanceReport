using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FinanceReport.Application.Dtos;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>
/// TC-TECH-03 : aucun secret dans les journaux, format et niveau des traces. La rotation journalière repose sur
/// l'horloge système de Serilog (RollingInterval.Day) et n'est pas simulée ici.
/// </summary>
public sealed partial class LoggingTests
{
    [Fact]
    public async Task Logs_contain_no_secret_and_follow_the_expected_format()
    {
        var factory = new ApiFactory(Path.Combine(Path.GetTempPath(), "financereport-it", Guid.NewGuid().ToString("N")), keepData: true);
        try
        {
            var logPath = factory.LogPath;
            var client = factory.CreateClient();
            var token = await ApiFactory.SetupAndLoginAsync(client);
            await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.Username, "mauvais-mot-de-passe"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await client.GetAsync("/api/test/protected");

            // Le journal est lu une fois vidé, à l'arrêt de l'hôte.
            await factory.DisposeAsync();
            var logFile = Directory.GetFiles(logPath).Should().ContainSingle().Subject;
            Path.GetFileName(logFile).Should().MatchRegex(@"^financereport-\d{8}\.log$");

            var content = await File.ReadAllTextAsync(logFile);
            content.Should().NotContain(ApiFactory.Password);
            content.Should().NotContain("mauvais-mot-de-passe");
            content.Should().NotContain("$2a$");
            content.Should().NotContain(token);
            content.Should().MatchRegex(LinePattern());
            content.Should().MatchRegex(@"\[WRN\] \[FinanceReport\.Application\.Services\.AuthService\] Échec de connexion");
            content.Should().Contain("HTTP GET /api/test/protected responded 200");
        }
        finally
        {
            await factory.DisposeAsync();
            TestData.Delete(factory.DataPath);
        }
    }

    [GeneratedRegex(@"(?m)^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}\] \[INF\] \[[\w.]+\] ")]
    private static partial Regex LinePattern();
}
