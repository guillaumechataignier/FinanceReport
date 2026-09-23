using FinanceReport.Application.Abstractions;
using FinanceReport.Domain.Abstractions;

namespace FinanceReport.Infrastructure.Authentication;

/// <summary>
/// Singleton en mémoire (RG-18) : au 5e échec consécutif, blocage de 15 minutes et remise à zéro du compteur.
/// Un redémarrage de l'API lève le blocage, ce qui est acceptable pour un usage local mono-utilisateur (TS §3.6).
/// </summary>
public sealed class LoginAttemptTracker(IClock clock) : ILoginAttemptTracker
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly Lock _lock = new();
    private int _failures;
    private DateTimeOffset? _lockedUntil;

    public TimeSpan? RemainingLockout()
    {
        lock (_lock)
        {
            var remaining = _lockedUntil - clock.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : null;
        }
    }

    public bool RegisterFailure()
    {
        lock (_lock)
        {
            if (++_failures < MaxFailures)
            {
                return false;
            }

            _failures = 0;
            _lockedUntil = clock.UtcNow + LockoutDuration;
            return true;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _failures = 0;
            _lockedUntil = null;
        }
    }
}
