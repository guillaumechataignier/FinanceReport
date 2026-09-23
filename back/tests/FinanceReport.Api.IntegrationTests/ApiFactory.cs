using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>API en mémoire sur un dossier de données temporaire, propre à chaque instance.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
        : this(Path.Combine(Path.GetTempPath(), "financereport-it", Guid.NewGuid().ToString("N")))
    {
    }

    public ApiFactory(string dataPath) => DataPath = dataPath;

    public string DataPath { get; }

    public string FilePath(string entityName) => Path.Combine(DataPath, $"{entityName}.json");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Storage:DataPath", DataPath);
    }
}
