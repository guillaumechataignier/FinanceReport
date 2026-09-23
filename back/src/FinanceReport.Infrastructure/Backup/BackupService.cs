using System.Globalization;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceReport.Infrastructure.Backup;

/// <summary>Copie horodatée d'un fichier avant modification, avec rétention limitée (RG-19, TS §2.6).</summary>
public sealed class BackupService(IOptions<StorageOptions> options, IClock clock, ILogger<BackupService> logger)
{
    private readonly StorageOptions _options = options.Value;

    /// <summary>
    /// Copie <paramref name="filePath"/> dans <c>backups/{entity}/{entity}_{yyyyMMddTHHmmssfffZ}.json</c>.
    /// </summary>
    /// <returns>Le chemin de la copie, ou null si le fichier n'existe pas encore.</returns>
    public string? Backup(string filePath, string entityName)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var directory = Path.Combine(_options.BackupPath, entityName);
        Directory.CreateDirectory(directory);

        var backupPath = UniquePath(directory, entityName);
        File.Copy(filePath, backupPath);
        Purge(directory);
        return backupPath;
    }

    private string UniquePath(string directory, string entityName)
    {
        var timestamp = clock.UtcNow.UtcDateTime.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
        var path = Path.Combine(directory, $"{entityName}_{timestamp}.json");

        // Deux écritures dans la même milliseconde : suffixe croissant, qui conserve l'ordre de tri par nom.
        for (var i = 1; File.Exists(path); i++)
        {
            path = Path.Combine(directory, $"{entityName}_{timestamp}_{i:D3}.json");
        }

        return path;
    }

    private void Purge(string directory)
    {
        var obsolete = Directory.GetFiles(directory, "*.json")
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .Skip(_options.BackupRetention);

        foreach (var file in obsolete)
        {
            File.Delete(file);
            logger.LogDebug("Sauvegarde purgée : {File}", Path.GetFileName(file));
        }
    }
}
