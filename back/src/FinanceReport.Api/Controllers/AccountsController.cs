using FinanceReport.Application.Dtos;
using FinanceReport.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceReport.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(AccountService accountService, BalanceService balanceService) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<AccountDto> List([FromQuery] bool includeArchived = false) => accountService.List(includeArchived);

    [HttpGet("{id:guid}")]
    public AccountDto Get(Guid id) => accountService.Get(id);

    [HttpPost]
    public async Task<IActionResult> Create(AccountRequest request) =>
        StatusCode(StatusCodes.Status201Created, await accountService.CreateAsync(request));

    [HttpPut("{id:guid}")]
    public Task<AccountDto> Update(Guid id, AccountRequest request) => accountService.UpdateAsync(id, request);

    [HttpPost("{id:guid}/archive")]
    public Task<AccountDto> Archive(Guid id) => accountService.ArchiveAsync(id);

    [HttpPost("{id:guid}/unarchive")]
    public Task<AccountDto> Unarchive(Guid id) => accountService.UnarchiveAsync(id);

    [HttpGet("{id:guid}/balances")]
    public IReadOnlyList<BalanceDto> Balances(Guid id) => balanceService.List(id);

    [HttpPut("{id:guid}/balances/{date}")]
    public Task<BalanceDto> UpsertBalance(Guid id, DateOnly date, BalanceRequest request) =>
        balanceService.UpsertAsync(id, date, request);

    [HttpDelete("{id:guid}/balances/{date}")]
    public async Task<IActionResult> DeleteBalance(Guid id, DateOnly date)
    {
        await balanceService.DeleteAsync(id, date);
        return NoContent();
    }
}
