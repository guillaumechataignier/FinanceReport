using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;

namespace FinanceReport.Application.Tests.Support;

internal sealed class InMemoryRepository<T>(IEnumerable<T>? items = null) : IRepository<T>
{
    private IReadOnlyList<T> _items = [.. items ?? []];

    public IReadOnlyList<T> GetAll() => _items;

    public void Save(IReadOnlyList<T> items) => _items = [.. items];
}

internal sealed class InMemoryReferentialRepository : IReferentialRepository
{
    private readonly Dictionary<ReferentialKind, InMemoryRepository<ReferentialItem>> _repositories =
        Enum.GetValues<ReferentialKind>().ToDictionary(k => k, _ => new InMemoryRepository<ReferentialItem>());

    public IRepository<ReferentialItem> For(ReferentialKind kind) => _repositories[kind];
}

internal sealed class FixedClock(DateOnly today) : IClock
{
    public DateTimeOffset UtcNow { get; } = new(today.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);

    public DateOnly Today { get; } = today;
}
