using FinanceReport.Application.Dtos;
using FinanceReport.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceReport.Api.Controllers;

[ApiController]
[Route("api/securities/{securityId:guid}/prices")]
public sealed class PricesController(PriceService priceService) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<PriceDto> List(Guid securityId) => priceService.List(securityId);

    [HttpPut("{date}")]
    public Task<PriceDto> Upsert(Guid securityId, DateOnly date, PriceRequest request) =>
        priceService.UpsertAsync(securityId, date, request);

    [HttpDelete("{date}")]
    public async Task<IActionResult> Delete(Guid securityId, DateOnly date)
    {
        await priceService.DeleteAsync(securityId, date);
        return NoContent();
    }
}
