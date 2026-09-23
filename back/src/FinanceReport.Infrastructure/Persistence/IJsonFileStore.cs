namespace FinanceReport.Infrastructure.Persistence;

/// <summary>Face non générique d'un <see cref="JsonFileStore{T}"/>, utilisée par <see cref="UnitOfWork"/> à la validation.</summary>
internal interface IJsonFileStore
{
    string EntityName { get; }

    string FilePath { get; }

    /// <summary>Sauvegarde puis remplace atomiquement le fichier. Renvoie le chemin de la sauvegarde, ou null.</summary>
    string? WriteToDisk(object items);

    /// <summary>Publie dans le cache les éléments écrits avec succès.</summary>
    void Accept(object items);

    /// <summary>Recharge le cache depuis le disque.</summary>
    void Reload();
}
