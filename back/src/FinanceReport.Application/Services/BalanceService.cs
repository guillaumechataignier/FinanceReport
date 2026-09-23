using FinanceReport.Application.Dtos;
using FinanceReport.Application.Exceptions;
using FinanceReport.Application.Validators;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FluentValidation;
using ValidationException = FinanceReport.Application.Exceptions.ValidationException;
using Microsoft.Extensions.Logging;

namespace FinanceReport.Application.Services;

/// <summary>Soldes des comptes : upsert sur (compte, date) et recalcul des snapshots (UC-04, RG-13).</summary>
public sealed class BalanceService(
    IRepository<Account> accounts,
    IRepository<Balance> balances,
    SnapshotService snapshotService,
    IValidator<BalanceRequest> validator,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<BalanceService> logger)
{
    public IReadOnlyList<BalanceDto> List(Guid accountId)
    {
        FindAccount(accountId);
        return
        [
            .. balances.GetAll()
                .Where(b => b.AccountId == accountId)
                .OrderByDescending(b => b.Date)
                .Select(ToDto),
        ];
    }

    public async Task<BalanceDto> UpsertAsync(Guid accountId, DateOnly date, BalanceRequest request)
    {
        validator.ValidateOrThrow(request);
        var balance = await unitOfWork.ExecuteAsync(() =>
        {
            EnsureWritable(FindAccount(accountId));
            DateRules.EnsureNotFuture(date, clock.Today);

            var saved = new Balance { AccountId = accountId, Date = date, Amount = request.Amount!.Value, UpdatedAt = clock.UtcNow };
            balances.Save([.. balances.GetAll().Where(b => !IsKey(b, accountId, date)), saved]);
            snapshotService.Rebuild(date);
            return saved;
        });

        logger.LogInformation("Solde du compte {AccountId} au {Date:yyyy-MM-dd} enregistré", accountId, date);
        return ToDto(balance);
    }

    public async Task DeleteAsync(Guid accountId, DateOnly date)
    {
        await unitOfWork.ExecuteAsync(() =>
        {
            EnsureWritable(FindAccount(accountId));
            var all = balances.GetAll();
            if (!all.Any(b => IsKey(b, accountId, date)))
            {
                throw new NotFoundException("Aucun solde à cette date pour ce compte.");
            }

            balances.Save([.. all.Where(b => !IsKey(b, accountId, date))]);
            snapshotService.Rebuild(date);
            return date;
        });

        logger.LogInformation("Solde du compte {AccountId} au {Date:yyyy-MM-dd} supprimé", accountId, date);
    }

    private Account FindAccount(Guid accountId) =>
        accounts.GetAll().FirstOrDefault(a => a.Id == accountId) ?? throw new NotFoundException("Compte introuvable.");

    // RG-27 : un compte archivé est exclu des listes de saisie.
    private static void EnsureWritable(Account account)
    {
        if (account.Archived)
        {
            throw ValidationException.ForField("accountId", "Le compte est archivé : désarchivez-le pour saisir un solde.");
        }
    }

    private static bool IsKey(Balance balance, Guid accountId, DateOnly date) => balance.AccountId == accountId && balance.Date == date;

    private static BalanceDto ToDto(Balance balance) => new(balance.AccountId, balance.Date, balance.Amount);
}
