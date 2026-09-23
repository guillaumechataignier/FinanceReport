namespace FinanceReport.Domain.Abstractions;

/// <summary>
/// Sérialise les opérations métier d'écriture (verrou global) et rend leurs écritures atomiques :
/// toutes les collections modifiées sont persistées, ou aucune (TS §2.5).
/// </summary>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<T> operation, CancellationToken cancellationToken = default);
}
