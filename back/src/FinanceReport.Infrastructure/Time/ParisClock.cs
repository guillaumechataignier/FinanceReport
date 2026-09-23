using FinanceReport.Domain.Abstractions;

namespace FinanceReport.Infrastructure.Time;

/// <summary>Horloge de l'application : la date du jour est celle d'Europe/Paris (RG-14).</summary>
public sealed class ParisClock(TimeProvider timeProvider) : IClock
{
    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, Paris).DateTime);
}
