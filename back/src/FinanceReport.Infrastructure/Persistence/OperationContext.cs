namespace FinanceReport.Infrastructure.Persistence;

/// <summary>Écritures en attente d'une opération <see cref="UnitOfWork"/>, dans l'ordre de leur première modification.</summary>
internal sealed class OperationContext
{
    private readonly OrderedDictionary<IJsonFileStore, object> _staged = [];

    public IEnumerable<KeyValuePair<IJsonFileStore, object>> Staged => _staged;

    public void Stage(IJsonFileStore store, object items) => _staged[store] = items;

    public bool TryGetStaged(IJsonFileStore store, out object? items) => _staged.TryGetValue(store, out items);
}
