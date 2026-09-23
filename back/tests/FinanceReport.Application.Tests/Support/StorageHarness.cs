using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FinanceReport.Infrastructure.Backup;
using FinanceReport.Infrastructure.Persistence;
using FinanceReport.Infrastructure.Time;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace FinanceReport.Application.Tests.Support;

/// <summary>Dossier de données temporaire, horloge simulée et briques de persistance réelles.</summary>
internal sealed class StorageHarness : IDisposable
{
    public StorageHarness(int backupRetention = 100)
    {
        DataPath = Path.Combine(Path.GetTempPath(), "financereport-tests", Guid.NewGuid().ToString("N"));
        Options = Microsoft.Extensions.Options.Options.Create(new StorageOptions { DataPath = DataPath, BackupRetention = backupRetention });
        Time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 23, 7, 0, 0, TimeSpan.Zero));
        Clock = new ParisClock(Time);
        Backups = new BackupService(Options, Clock, NullLogger<BackupService>.Instance);
        UnitOfWork = new UnitOfWork(NullLogger<UnitOfWork>.Instance);
    }

    public string DataPath { get; }

    public IOptions<StorageOptions> Options { get; }

    public FakeTimeProvider Time { get; }

    public ParisClock Clock { get; }

    public BackupService Backups { get; }

    public UnitOfWork UnitOfWork { get; }

    public JsonFileStore<T> Store<T>(string entityName, bool backupEnabled = true) =>
        new(entityName, Options, Backups, NullLogger<JsonFileStore<T>>.Instance, backupEnabled);

    public Task WriteAsync<T>(JsonFileStore<T> store, IReadOnlyList<T> items) =>
        UnitOfWork.ExecuteAsync(() => { store.Save(items); return items.Count; });

    public string FilePath(string entityName) => Path.Combine(DataPath, $"{entityName}.json");

    public string[] BackupFiles(string entityName)
    {
        var directory = Path.Combine(DataPath, "backups", entityName);
        return Directory.Exists(directory)
            ? Directory.GetFiles(directory).Order(StringComparer.Ordinal).ToArray()
            : [];
    }

    public Account NewAccount(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Type = AccountType.Pea,
        InstitutionId = Guid.NewGuid(),
        CreatedAt = Clock.UtcNow,
        UpdatedAt = Clock.UtcNow,
    };

    public void Dispose()
    {
        if (Directory.Exists(DataPath))
        {
            Directory.Delete(DataPath, recursive: true);
        }
    }
}
