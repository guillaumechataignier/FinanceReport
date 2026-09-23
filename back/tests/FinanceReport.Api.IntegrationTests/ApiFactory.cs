using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceReport.Application.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>
/// API en mémoire sur un dossier temporaire propre à chaque instance, avec une horloge simulée
/// (23/09/2026 09:00 à Paris) et les contrôleurs de test de cet assembly.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Username = "guillaume";
    public const string Password = "motdepasse-solide-2026";

    public ApiFactory()
        : this(Path.Combine(Path.GetTempPath(), "financereport-it", Guid.NewGuid().ToString("N")))
    {
    }

    /// <param name="keepData">Conserve le dossier de données après l'arrêt (le test le supprime lui-même).</param>
    public ApiFactory(string dataPath, bool keepData = false)
    {
        DataPath = dataPath;
        _keepData = keepData;
    }

    private readonly bool _keepData;

    public string DataPath { get; }

    public string LogPath => Path.Combine(DataPath, "logs");

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 9, 23, 7, 0, 0, TimeSpan.Zero));

    public string FilePath(string entityName) => Path.Combine(DataPath, $"{entityName}.json");

    /// <summary>Initialise le compte d'accès, se connecte et renvoie un client authentifié.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var token = await SetupAndLoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<string> SetupAndLoginAsync(HttpClient client)
    {
        var setup = await client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(Username, Password, Password));
        setup.EnsureSuccessStatusCode();
        return await LoginAsync(client);
    }

    public static async Task<string> LoginAsync(HttpClient client)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(Username, Password));
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Storage:DataPath", DataPath);
        builder.UseSetting("Logs:Path", LogPath);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Time);
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && !_keepData)
        {
            TestData.Delete(DataPath);
        }
    }
}
