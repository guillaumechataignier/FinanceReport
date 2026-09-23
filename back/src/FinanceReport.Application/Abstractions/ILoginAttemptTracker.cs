namespace FinanceReport.Application.Abstractions;

/// <summary>Compteur des échecs de connexion consécutifs et blocage de 15 minutes (RG-18).</summary>
public interface ILoginAttemptTracker
{
    /// <summary>Durée de blocage restante, ou null si la connexion est autorisée.</summary>
    TimeSpan? RemainingLockout();

    /// <summary>Enregistre un échec. Renvoie true si cet échec déclenche le blocage.</summary>
    bool RegisterFailure();

    void Reset();
}
