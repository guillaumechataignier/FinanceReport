using FinanceReport.Application.Dtos;
using FinanceReport.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceReport.Api.Controllers;

[ApiController]
[Route("api/securities")]
public sealed class SecuritiesController(SecurityService securityService) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<SecurityDto> List([FromQuery] bool includeArchived = false) => securityService.List(includeArchived);

    [HttpGet("{id:guid}")]
    public SecurityDto Get(Guid id) => securityService.Get(id);

    [HttpPost]
    public async Task<IActionResult> Create(SecurityRequest request) =>
        StatusCode(StatusCodes.Status201Created, await securityService.CreateAsync(request));

    [HttpPut("{id:guid}")]
    public Task<SecurityDto> Update(Guid id, SecurityRequest request) => securityService.UpdateAsync(id, request);

    [HttpPost("{id:guid}/archive")]
    public Task<SecurityDto> Archive(Guid id) => securityService.ArchiveAsync(id);

    [HttpPost("{id:guid}/unarchive")]
    public Task<SecurityDto> Unarchive(Guid id) => securityService.UnarchiveAsync(id);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await securityService.DeleteAsync(id);
        return NoContent();
    }
}
