using FinanceReport.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace FinanceReport.Infrastructure.Persistence;

/// <summary>
/// Verrou global des opérations d'écriture et validation « tout ou rien » des fichiers modifiés (TS §2.5) :
/// en cas d'échec, les fichiers déjà écrits sont restaurés (depuis leur sauvegarde, ou depuis le cache pour une
/// collection sans sauvegarde) et les caches rechargés.
/// </summary>
public sealed class UnitOfWork(ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private static readonly AsyncLocal<OperationContext?> CurrentContext = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    internal static OperationContext? Current => CurrentContext.Value;

    public async Task<T> ExecuteAsync<T>(Func<T> operation, CancellationToken cancellationToken = default)
    {
        if (CurrentContext.Value is not null)
        {
            // Opération imbriquée : elle fait partie de l'opération englobante.
            return operation();
        }

        await _lock.WaitAsync(cancellationToken);
        var context = new OperationContext();
        CurrentContext.Value = context;
        try
        {
            var result = operation();
            Commit(context);
            return result;
        }
        finally
        {
            CurrentContext.Value = null;
            _lock.Release();
        }
    }

    private void Commit(OperationContext context)
    {
        var staged = context.Staged.ToList();
        var written = new List<(IJsonFileStore Store, RestorePoint RestorePoint)>();
        try
        {
            foreach (var (store, items) in staged)
            {
                written.Add((store, store.WriteToDisk(items)));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec d'écriture : restauration de {Count} fichier(s)", written.Count);
            foreach (var (store, restorePoint) in written)
            {
                Restore(store, restorePoint);
            }

            foreach (var (store, _) in staged)
            {
                store.Reload();
            }

            throw;
        }

        foreach (var (store, items) in staged)
        {
            store.Accept(items);
        }
    }

    private void Restore(IJsonFileStore store, RestorePoint restorePoint)
    {
        try
        {
            store.Restore(restorePoint);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Restauration impossible de {File}", store.FilePath);
        }
    }
}
