namespace FinanceReport.Infrastructure.Persistence;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Dossier racine des fichiers de données.</summary>
    public string DataPath { get; set; } = "./data";

    /// <summary>Nombre de sauvegardes conservées par fichier (RG-19).</summary>
    public int BackupRetention { get; set; } = 100;

    public string BackupPath => Path.Combine(DataPath, "backups");
}
