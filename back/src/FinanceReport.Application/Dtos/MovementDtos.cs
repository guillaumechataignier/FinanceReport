using FinanceReport.Domain.Enums;

namespace FinanceReport.Application.Dtos;

/// <summary>
/// ACHAT/VENTE : securityId, quantity, unitPrice, fees (0 par défaut) ; amount interdit.
/// VERSEMENT/RETRAIT : amount ; les autres champs interdits (FS §3.4).
/// </summary>
public sealed record MovementRequest(
    MovementType? Type,
    DateOnly? Date,
    Guid? AccountId,
    Guid? SecurityId,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? Fees,
    decimal? Amount);

public sealed record MovementFilter(Guid? AccountId, Guid? SecurityId, MovementType? Type, DateOnly? From, DateOnly? To);

/// <param name="Total">
/// Montant de l'opération : q × prix + frais (achat), q × prix − frais (vente), montant (versement, retrait).
/// </param>
public sealed record MovementDto(
    Guid Id,
    MovementType Type,
    DateOnly Date,
    Guid AccountId,
    string AccountName,
    Guid? SecurityId,
    string? SecurityName,
    string? SecurityCode,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? Fees,
    decimal? Amount,
    decimal Total,
    long Sequence,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PriceRequest(decimal? Price);

public sealed record PriceDto(Guid SecurityId, DateOnly Date, decimal Price);
