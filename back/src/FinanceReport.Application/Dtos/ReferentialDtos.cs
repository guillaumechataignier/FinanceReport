namespace FinanceReport.Application.Dtos;

/// <summary>Zone, secteur (code + libellé) ou établissement (libellé seul, code null).</summary>
public sealed record ReferentialItemRequest(string? Code, string? Label);

public sealed record ReferentialItemDto(Guid Id, string? Code, string Label, bool Archived, int UsageCount);
