using FinanceReport.Domain.Enums;

namespace FinanceReport.Application.Dtos;

public sealed record SecurityRequest(string? Name, string? Code, SecurityType? Type, string? Zone, string? Sector);

/// <param name="ZoneLabel">Libellé courant de la zone, archivée comprise.</param>
/// <param name="LastPrice">Dernier cours saisi, null sans cours.</param>
public sealed record SecurityDto(
    Guid Id,
    string Name,
    string Code,
    SecurityType Type,
    string Zone,
    string ZoneLabel,
    bool ZoneArchived,
    string Sector,
    string SectorLabel,
    bool SectorArchived,
    bool Archived,
    decimal? LastPrice,
    DateOnly? LastPriceDate);
