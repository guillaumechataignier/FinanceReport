using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FinanceReport.Infrastructure.Backup;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceReport.Infrastructure.Persistence;

public sealed class ReferentialRepository : IReferentialRepository
{
    private readonly Dictionary<ReferentialKind, JsonFileStore<ReferentialItem>> _stores;

    public ReferentialRepository(IOptions<StorageOptions> options, BackupService backupService, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<JsonFileStore<ReferentialItem>>();
        _stores = Enum.GetValues<ReferentialKind>().ToDictionary(
            kind => kind,
            kind => new JsonFileStore<ReferentialItem>(EntityName(kind), options, backupService, logger));
    }

    public IRepository<ReferentialItem> For(ReferentialKind kind) => Store(kind);

    public JsonFileStore<ReferentialItem> Store(ReferentialKind kind) => _stores[kind];

    /// <summary>zones, sectors, institutions : nom du fichier et du dossier de sauvegarde.</summary>
    public static string EntityName(ReferentialKind kind) => kind switch
    {
        ReferentialKind.Zones => "zones",
        ReferentialKind.Sectors => "sectors",
        ReferentialKind.Institutions => "institutions",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
