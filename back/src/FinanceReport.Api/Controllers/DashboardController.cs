using FinanceReport.Application.Dtos;
using FinanceReport.Application.Exceptions;
using FinanceReport.Application.Services;
using FinanceReport.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinanceReport.Api.Controllers;

/// <summary>
/// Restitutions. Les filtres multiples acceptent des valeurs répétées (<c>zones=A&amp;zones=B</c>)
/// ou séparées par des virgules (<c>zones=A,B</c>).
/// </summary>
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public SummaryDto Summary() => dashboardService.Summary();

    [HttpGet("history")]
    public HistoryDto History([FromQuery] string? period) => dashboardService.History(period);

    [HttpGet("positions")]
    public DashboardPositionsDto Positions(
        [FromQuery] string[]? accountIds,
        [FromQuery] string[]? securityTypes,
        [FromQuery] string[]? zones,
        [FromQuery] string[]? sectors) =>
        dashboardService.Positions(new DashboardFilter(
            ParseGuids(accountIds, "accountIds"),
            ParseSecurityTypes(securityTypes),
            Split(zones),
            Split(sectors)));

    [HttpGet("accounts")]
    public DashboardAccountsDto Accounts([FromQuery] string[]? accountIds) =>
        dashboardService.Accounts(ParseGuids(accountIds, "accountIds"));

    private static List<string> Split(string[]? values) =>
        [.. (values ?? []).SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Distinct()];

    private static List<Guid> ParseGuids(string[]? values, string field) =>
    [
        .. Split(values).Select(v => Guid.TryParse(v, out var id)
            ? id
            : throw ValidationException.ForField(field, $"Identifiant invalide : « {v} »."))
    ];

    private static List<SecurityType> ParseSecurityTypes(string[]? values) =>
    [
        .. Split(values).Select(v => Enum.GetValues<SecurityType>()
            .Cast<SecurityType?>()
            .FirstOrDefault(t => string.Equals(t.ToString(), v, StringComparison.OrdinalIgnoreCase))
            ?? throw ValidationException.ForField("securityTypes", $"Type de support inconnu : « {v} »."))
    ];
}
