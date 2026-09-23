namespace FinanceReport.Domain.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Date du jour dans le fuseau Europe/Paris (RG-14, RG-25).</summary>
    DateOnly Today { get; }
}
