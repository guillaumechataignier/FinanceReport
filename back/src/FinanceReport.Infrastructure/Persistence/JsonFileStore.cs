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
/// </summary>
public sealed class JsonFileStore<T> : IRepository<T>, IJsonFileStore
{
    private readonly BackupService _backupService;
    private readonly ILogger _logger;
    private readonly Lock _loadLock = new();
    private IReadOnlyList<T>? _items;

    public JsonFileStore(string entityName, IOptions<StorageOptions> options, BackupService backupService, ILogger<JsonFileStore<T>> logger)
    {
        EntityName = entityName;
        FilePath = Path.Combine(options.Value.DataPath, $"{entityName}.json");
        _backupService = backupService;
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

    string? IJsonFileStore.WriteToDisk(object items)
    {
        var list = (IReadOnlyList<T>)items;
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        var backupPath = _backupService.Backup(FilePath, EntityName);
        var tempPath = FilePath + ".tmp";
        try
        {
            using (var stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, new JsonEnvelope<T>(JsonEnvelope<T>.CurrentSchemaVersion, list), JsonDefaults.Options);
            }

            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }

        _logger.LogInformation("Fichier {Entity} écrit : {Count} élément(s)", EntityName, list.Count);
        return backupPath;
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
