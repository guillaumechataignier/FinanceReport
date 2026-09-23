using FinanceReport.Domain.Enums;

namespace FinanceReport.Domain.Entities;

public sealed record Account
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required AccountType Type { get; init; }
    public required Guid InstitutionId { get; init; }
    public bool Archived { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
