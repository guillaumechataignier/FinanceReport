using FinanceReport.Domain.Enums;

namespace FinanceReport.Application.Dtos;

public sealed record AccountRequest(string? Name, AccountType? Type, Guid? InstitutionId);

/// <param name="CurrentValue">Valeur du jour, calculée aussi pour un compte archivé (qui reste exclu du patrimoine).</param>
/// <param name="Cash">Dernier solde (compte espèces) ou liquidités (compte titres) ; 0 sans solde.</param>
/// <param name="CashDate">Date du dernier solde, null sans solde.</param>
public sealed record AccountDto(
    Guid Id,
    string Name,
    AccountType Type,
    AccountCategory Category,
    Guid InstitutionId,
    string InstitutionName,
    bool InstitutionArchived,
    bool Archived,
    decimal CurrentValue,
    decimal Cash,
    DateOnly? CashDate);

public sealed record BalanceRequest(decimal? Amount);

public sealed record BalanceDto(Guid AccountId, DateOnly Date, decimal Amount);
