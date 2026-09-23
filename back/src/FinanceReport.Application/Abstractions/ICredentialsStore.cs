using FinanceReport.Domain.Entities;

namespace FinanceReport.Application.Abstractions;

/// <summary>Fichier <c>credentials.json</c> : jamais sauvegardé, car il contient la clé JWT (TS §2.6).</summary>
public interface ICredentialsStore
{
    Credentials? Get();

    /// <summary>Crée le fichier s'il n'existe pas. Renvoie false s'il existe déjà.</summary>
    bool TryCreate(Credentials credentials);
}
