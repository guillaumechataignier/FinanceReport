namespace FinanceReport.Domain.Abstractions;

/// <summary>Collection d'entités persistée comme un tout (un fichier JSON par collection).</summary>
public interface IRepository<T>
{
    IReadOnlyList<T> GetAll();

    /// <summary>Remplace la collection. À appeler dans une opération <see cref="IUnitOfWork"/> : l'écriture a lieu à sa validation.</summary>
    void Save(IReadOnlyList<T> items);
}
