using System.Text.Json;
using FinanceReport.Application.Abstractions;
using FinanceReport.Domain.Entities;
using FinanceReport.Infrastructure.Persistence;
using FinanceReport.Infrastructure.Serialization;
using Microsoft.Extensions.Options;

namespace FinanceReport.Infrastructure.Authentication;

/// <summary>
/// <c>credentials.json</c>, sans enveloppe ni sauvegarde (TS §2.2, §2.6). Mis en cache une fois lu :
/// la suppression du fichier (mot de passe oublié) prend effet au redémarrage.
/// </summary>
public sealed class CredentialsStore(IOptions<StorageOptions> options) : ICredentialsStore
{
    private readonly string _filePath = Path.Combine(options.Value.DataPath, "credentials.json");
    private readonly Lock _lock = new();
    private Credentials? _cached;

    public Credentials? Get()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        lock (_lock)
        {
            if (_cached is null && File.Exists(_filePath))
            {
                using var stream = File.OpenRead(_filePath);
                _cached = JsonSerializer.Deserialize<Credentials>(stream, JsonDefaults.Options);
            }

            return _cached;
        }
    }

    public bool TryCreate(Credentials credentials)
    {
        lock (_lock)
        {
            if (File.Exists(_filePath))
            {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(credentials, JsonDefaults.Options));
            try
            {
                File.Move(tempPath, _filePath, overwrite: false);
            }
            catch (IOException) when (File.Exists(_filePath))
            {
                File.Delete(tempPath);
                return false;
            }

            _cached = credentials;
            return true;
        }
    }
}
