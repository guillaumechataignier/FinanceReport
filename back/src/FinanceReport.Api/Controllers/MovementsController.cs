using FinanceReport.Application.Dtos;
using FinanceReport.Application.Services;
using FinanceReport.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinanceReport.Api.Controllers;

[ApiController]
[Route("api/movements")]
public sealed class MovementsController(MovementService movementService) : ControllerBase
{
    /// <summary>Historique filtré, trié par date décroissante (UC-09).</summary>
    [HttpGet]
    public IReadOnlyList<MovementDto> List(
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? securityId,
        [FromQuery] MovementType? type,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to) =>
        movementService.List(new MovementFilter(accountId, securityId, type, from, to));

    [HttpGet("{id:guid}")]
    public MovementDto Get(Guid id) => movementService.Get(id);

    [HttpPost]
    public async Task<IActionResult> Create(MovementRequest request) =>
        StatusCode(StatusCodes.Status201Created, await movementService.CreateAsync(request));

    [HttpPut("{id:guid}")]
    public Task<MovementDto> Update(Guid id, MovementRequest request) => movementService.UpdateAsync(id, request);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await movementService.DeleteAsync(id);
        return NoContent();
    }
}
