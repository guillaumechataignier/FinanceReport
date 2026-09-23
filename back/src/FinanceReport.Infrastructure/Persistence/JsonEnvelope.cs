namespace FinanceReport.Infrastructure.Persistence;

/// <summary>Enveloppe commune des fichiers de données (TS §2.1).</summary>
internal sealed record JsonEnvelope<T>(int SchemaVersion, IReadOnlyList<T> Items)
{
    public const int CurrentSchemaVersion = 1;
}
