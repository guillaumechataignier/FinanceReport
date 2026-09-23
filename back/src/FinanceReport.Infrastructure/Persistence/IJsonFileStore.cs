namespace FinanceReport.Infrastructure.Persistence;

/// <summary>Face non générique d'un <see cref="JsonFileStore{T}"/>, utilisée par <see cref="UnitOfWork"/> à la validation.</summary>
internal interface IJsonFileStore
{
    string EntityName { get; }

    string FilePath { get; }

    /// <summary>Sauvegarde (si activée) puis remplace atomiquement le fichier.</summary>
    /// <returns>De quoi remettre le fichier dans son état antérieur si l'opération échoue plus loin.</returns>
    RestorePoint WriteToDisk(object items);

    /// <summary>Remet le fichier dans l'état qu'il avait avant <see cref="WriteToDisk"/>.</summary>
    void Restore(RestorePoint restorePoint);

    /// <summary>Publie dans le cache les éléments écrits avec succès.</summary>
    void Accept(object items);

    /// <summary>Recharge le cache depuis le disque.</summary>
    void Reload();
}

/// <param name="FileExisted">Le fichier existait avant l'écriture ; sinon, la restauration le supprime.</param>
/// <param name="BackupPath">Copie prise avant l'écriture, ou null (fichier absent, ou collection sans sauvegarde).</param>
internal sealed record RestorePoint(bool FileExisted, string? BackupPath);
