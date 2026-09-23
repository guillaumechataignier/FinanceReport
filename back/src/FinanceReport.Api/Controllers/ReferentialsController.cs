using FinanceReport.Application.Dtos;
using FinanceReport.Application.Exceptions;
using FinanceReport.Application.Services;
using FinanceReport.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinanceReport.Api.Controllers;

/// <summary>Référentiels administrables ; <c>{kind}</c> ∈ zones, sectors, institutions (404 sinon).</summary>
[ApiController]
[Route("api/referentials/{kind}")]
public sealed class ReferentialsController(ReferentialService referentialService) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<ReferentialItemDto> List(string kind, [FromQuery] bool includeArchived = false) =>
        referentialService.List(ParseKind(kind), includeArchived);

    [HttpPost]
    public async Task<IActionResult> Create(string kind, ReferentialItemRequest request) =>
        StatusCode(StatusCodes.Status201Created, await referentialService.CreateAsync(ParseKind(kind), request));

    [HttpPut("{id:guid}")]
    public Task<ReferentialItemDto> Update(string kind, Guid id, ReferentialItemRequest request) =>
        referentialService.UpdateAsync(ParseKind(kind), id, request);

    [HttpPost("{id:guid}/archive")]
    public Task<ReferentialItemDto> Archive(string kind, Guid id) => referentialService.ArchiveAsync(ParseKind(kind), id);

    [HttpPost("{id:guid}/unarchive")]
    public Task<ReferentialItemDto> Unarchive(string kind, Guid id) => referentialService.UnarchiveAsync(ParseKind(kind), id);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(string kind, Guid id)
    {
        await referentialService.DeleteAsync(ParseKind(kind), id);
        return NoContent();
    }

    private static ReferentialKind ParseKind(string kind) => kind switch
    {
        "zones" => ReferentialKind.Zones,
        "sectors" => ReferentialKind.Sectors,
        "institutions" => ReferentialKind.Institutions,
        _ => throw new NotFoundException($"Référentiel inconnu : « {kind} »."),
    };
}
