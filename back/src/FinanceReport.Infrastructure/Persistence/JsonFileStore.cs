using System.Text.Json;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Infrastructure.Backup;
using FinanceReport.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceReport.Infrastructure.Persistence;

/// <summary>
/// Collection persistée dans <c>{DataPath}/{entity}.json</c>, chargée au premier accès puis gardée en cache (TS §2.5).
/// Les écritures passent par <see cref="UnitOfWork"/> : dans une opération, <see cref="GetAll"/> voit les éléments en attente.
/// Une collection sans sauvegarde (<c>snapshots</c>, entièrement recalculable) est restaurée depuis son cache en cas d'échec.
/// </summary>
public sealed class JsonFileStore<T> : IRepository<T>, IJsonFileStore
{
    private readonly BackupService _backupService;
    private readonly bool _backupEnabled;
    private readonly ILogger _logger;
    private readonly Lock _loadLock = new();
    private IReadOnlyList<T>? _items;

    /// <param name="backupEnabled">Copie horodatée avant chaque écriture (RG-19) ; désactivée pour les snapshots.</param>
    public JsonFileStore(
        string entityName,
        IOptions<StorageOptions> options,
        BackupService backupService,
        ILogger<JsonFileStore<T>> logger,
        bool backupEnabled = true)
    {
        EntityName = entityName;
        FilePath = Path.Combine(options.Value.DataPath, $"{entityName}.json");
        _backupService = backupService;
        _backupEnabled = backupEnabled;
        _logger = logger;
    }

    public string EntityName { get; }

    public string FilePath { get; }

    public bool FileExists => File.Exists(FilePath);

    /// <summary>Incrémentée à chaque changement du cache, pour invalider les index dérivés.</summary>
    public long Version { get; private set; }

    public IReadOnlyList<T> GetAll()
    {
        if (UnitOfWork.Current is { } context && context.TryGetStaged(this, out var staged))
        {
            return (IReadOnlyList<T>)staged!;
        }

        return _items ?? Load();
    }

    public void Save(IReadOnlyList<T> items)
    {
        var context = UnitOfWork.Current
            ?? throw new InvalidOperationException($"Écriture de « {EntityName} » hors d'une opération IUnitOfWork.");
        context.Stage(this, items.ToArray());
    }

    RestorePoint IJsonFileStore.WriteToDisk(object items)
    {
        var list = (IReadOnlyList<T>)items;
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        var fileExisted = File.Exists(FilePath);
        if (fileExisted && !_backupEnabled)
        {
            // Sans sauvegarde, l'état antérieur doit être en cache avant d'écraser le fichier.
            GetCommitted();
        }

        var restorePoint = new RestorePoint(fileExisted, _backupEnabled ? _backupService.Backup(FilePath, EntityName) : null);
        WriteAtomically(list);

        _logger.LogInformation("Fichier {Entity} écrit : {Count} élément(s)", EntityName, list.Count);
        return restorePoint;
    }

    void IJsonFileStore.Restore(RestorePoint restorePoint)
    {
        if (!restorePoint.FileExisted)
        {
            File.Delete(FilePath);
        }
        else if (restorePoint.BackupPath is not null)
        {
            File.Copy(restorePoint.BackupPath, FilePath, overwrite: true);
        }
        else
        {
            // Sans sauvegarde, le cache contient encore l'état validé avant l'opération.
            WriteAtomically(GetCommitted());
        }
    }

    void IJsonFileStore.Accept(object items)
    {
        _items = (IReadOnlyList<T>)items;
        Version++;
    }

    void IJsonFileStore.Reload()
    {
        lock (_loadLock)
        {
            _items = null;
        }

        Load();
        Version++;
    }

    private IReadOnlyList<T> GetCommitted() => _items ?? Load();

    private void WriteAtomically(IReadOnlyList<T> items)
    {
        var tempPath = FilePath + ".tmp";
        try
        {
            using (var stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, new JsonEnvelope<T>(JsonEnvelope<T>.CurrentSchemaVersion, items), JsonDefaults.Options);
            }

            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private IReadOnlyList<T> Load()
    {
        lock (_loadLock)
        {
            if (_items is not null)
            {
                return _items;
            }

            if (!File.Exists(FilePath))
            {
                return _items = [];
            }

            using var stream = File.OpenRead(FilePath);
            var envelope = JsonSerializer.Deserialize<JsonEnvelope<T>>(stream, JsonDefaults.Options)
                ?? throw new InvalidDataException($"Fichier vide ou invalide : {FilePath}");

            if (envelope.SchemaVersion != JsonEnvelope<T>.CurrentSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Version de schéma {envelope.SchemaVersion} non prise en charge dans {FilePath}.");
            }

            return _items = envelope.Items ?? [];
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Le fichier temporaire sera écrasé à la prochaine écriture.
        }
    }
}
