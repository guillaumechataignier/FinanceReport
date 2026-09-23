using FinanceReport.Application.Dtos;
using FinanceReport.Application.Exceptions;
using FinanceReport.Application.Validators;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Calculators;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FinanceReport.Domain.Rules;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinanceReport.Application.Services;

/// <summary>Comptes (UC-03, RG-21, RG-24, RG-26, RG-27).</summary>
public sealed class AccountService(
    IRepository<Account> accounts,
    IRepository<Balance> balances,
    IRepository<Movement> movements,
    IRepository<SecurityPrice> prices,
    IReferentialRepository referentials,
    ReferentialService referentialService,
    SnapshotService snapshotService,
    IValidator<AccountRequest> validator,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<AccountService> logger)
{
    public IReadOnlyList<AccountDto> List(bool includeArchived)
    {
        var all = accounts.GetAll();
        var valuation = ValuateAll(all);
        var institutions = Institutions();
        return
        [
            .. all.Where(a => includeArchived || !a.Archived)
                .OrderBy(a => a.Archived)
                .ThenBy(a => a.Name, TextComparison.French)
                .Select(a => ToDto(a, valuation, institutions)),
        ];
    }

    public AccountDto Get(Guid id)
    {
        var all = accounts.GetAll();
        var account = Find(all, id);
        return ToDto(account, ValuateAll(all), Institutions());
    }

    public async Task<AccountDto> CreateAsync(AccountRequest request)
    {
        validator.ValidateOrThrow(request);
        var id = await unitOfWork.ExecuteAsync(() =>
        {
            var all = accounts.GetAll();
            var name = request.Name!.Trim();
            EnsureUniqueName(all, name, excludedId: null);
            referentialService.EnsureSelectableInstitution(request.InstitutionId!.Value, previousId: null);

            var now = clock.UtcNow;
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = request.Type!.Value,
                InstitutionId = request.InstitutionId.Value,
                CreatedAt = now,
                UpdatedAt = now,
            };
            accounts.Save([.. all, account]);
            snapshotService.Rebuild(clock.Today);
            return account.Id;
        });

        logger.LogInformation("Compte {Id} créé", id);
        return Get(id);
    }

    public async Task<AccountDto> UpdateAsync(Guid id, AccountRequest request)
    {
        validator.ValidateOrThrow(request);
        await unitOfWork.ExecuteAsync(() =>
        {
            var all = accounts.GetAll();
            var current = Find(all, id);
            var name = request.Name!.Trim();
            EnsureUniqueName(all, name, excludedId: id);
            referentialService.EnsureSelectableInstitution(request.InstitutionId!.Value, current.InstitutionId);

            var type = request.Type!.Value;
            if (type != current.Type && HasData(id))
            {
                // RG-21 : le type est figé dès le premier mouvement ou solde.
                throw new ConflictException("Le type d'un compte qui a déjà des mouvements ou des soldes n'est plus modifiable.");
            }

            var updated = current with { Name = name, Type = type, InstitutionId = request.InstitutionId.Value, UpdatedAt = clock.UtcNow };
            accounts.Save([.. all.Select(a => a.Id == id ? updated : a)]);
            snapshotService.Rebuild(clock.Today);
            return id;
        });

        logger.LogInformation("Compte {Id} modifié", id);
        return Get(id);
    }

    public Task<AccountDto> ArchiveAsync(Guid id) => SetArchivedAsync(id, archived: true);

    public Task<AccountDto> UnarchiveAsync(Guid id) => SetArchivedAsync(id, archived: false);

    private async Task<AccountDto> SetArchivedAsync(Guid id, bool archived)
    {
        await unitOfWork.ExecuteAsync(() =>
        {
            var all = accounts.GetAll();
            var updated = Find(all, id) with { Archived = archived, UpdatedAt = clock.UtcNow };
            accounts.Save([.. all.Select(a => a.Id == id ? updated : a)]);
            snapshotService.Rebuild(clock.Today);
            return id;
        });

        logger.LogInformation("Compte {Id} {Operation}", id, archived ? "archivé" : "désarchivé");
        return Get(id);
    }

    private bool HasData(Guid accountId) =>
        movements.GetAll().Any(m => m.AccountId == accountId) || balances.GetAll().Any(b => b.AccountId == accountId);

    private static void EnsureUniqueName(IReadOnlyList<Account> all, string name, Guid? excludedId)
    {
        if (all.Any(a => a.Id != excludedId && TextComparison.SameText(a.Name, name)))
        {
            throw new ConflictException($"Un compte nommé « {name} » existe déjà.");
        }
    }

    private static Account Find(IReadOnlyList<Account> all, Guid id) =>
        all.FirstOrDefault(a => a.Id == id) ?? throw new NotFoundException("Compte introuvable.");

    // Valeur du jour de chaque compte, archivés compris (écran Comptes).
    private Valuation ValuateAll(IReadOnlyList<Account> all) =>
        ValuationCalculator.Valuate(clock.Today, all, movements.GetAll(), balances.GetAll(), prices.GetAll(), includeArchived: true);

    private Dictionary<Guid, ReferentialItem> Institutions() =>
        referentials.For(ReferentialKind.Institutions).GetAll().ToDictionary(i => i.Id);

    private static AccountDto ToDto(Account account, Valuation valuation, Dictionary<Guid, ReferentialItem> institutions)
    {
        var value = valuation.Accounts.First(v => v.AccountId == account.Id);
        var institution = institutions.GetValueOrDefault(account.InstitutionId);
        return new AccountDto(
            account.Id,
            account.Name,
            account.Type,
            account.Type.GetCategory(),
            account.InstitutionId,
            institution?.Label ?? "",
            institution?.Archived ?? false,
            account.Archived,
            DecimalMath.Round(value.Value, Precision.Amount),
            DecimalMath.Round(value.Cash, Precision.Amount),
            value.CashDate);
    }
}
