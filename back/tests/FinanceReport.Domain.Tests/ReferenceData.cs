using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;

namespace FinanceReport.Domain.Tests;

/// <summary>Jeu de données de référence (TestPlan §1.3). Date du jour simulée : 23/09/2026.</summary>
internal static class ReferenceData
{
    public static readonly DateOnly Today = new(2026, 9, 23);
    private static readonly DateTimeOffset Timestamp = new(2026, 9, 23, 7, 0, 0, TimeSpan.Zero);

    public static readonly Guid E1 = Id(0xE1);
    public static readonly Guid E2 = Id(0xE2);
    public static readonly Guid AccountA = Id(0xA);
    public static readonly Guid AccountB = Id(0xB);
    public static readonly Guid AccountC = Id(0xC);
    public static readonly Guid S1 = Id(0x51);
    public static readonly Guid S2 = Id(0x52);
    public static readonly Guid M1 = Id(0x101);
    public static readonly Guid M2 = Id(0x102);
    public static readonly Guid M3 = Id(0x103);

    public static IReadOnlyList<Account> Accounts =>
    [
        Account(AccountA, "PEA Test", AccountType.Pea, E1),
        Account(AccountB, "Livret A", AccountType.Livret, E2),
        Account(AccountC, "Ancien CTO", AccountType.Cto, E1, archived: true),
    ];

    public static IReadOnlyList<Movement> Movements =>
    [
        Buy(M1, new DateOnly(2026, 1, 10), AccountA, S1, 10m, 100.00m, 2.00m, sequence: 1),
        Buy(M2, new DateOnly(2026, 2, 10), AccountA, S1, 5m, 110.00m, 1.00m, sequence: 2),
        Sell(M3, new DateOnly(2026, 3, 10), AccountA, S1, 6m, 120.00m, 1.50m, sequence: 3),
    ];

    public static IReadOnlyList<Balance> Balances =>
    [
        Balance(AccountB, new DateOnly(2026, 9, 1), 5000.00m),
        Balance(AccountC, new DateOnly(2026, 1, 1), 1000.00m),
        Balance(AccountA, new DateOnly(2026, 9, 1), 200.00m),
    ];

    public static IReadOnlyList<SecurityPrice> Prices =>
    [
        Price(S1, new DateOnly(2026, 9, 22), 115.00m),
    ];

    public static Account Account(Guid id, string name, AccountType type, Guid institutionId, bool archived = false) => new()
    {
        Id = id,
        Name = name,
        Type = type,
        InstitutionId = institutionId,
        Archived = archived,
        CreatedAt = Timestamp,
        UpdatedAt = Timestamp,
    };

    public static Movement Buy(Guid id, DateOnly date, Guid accountId, Guid securityId, decimal quantity, decimal unitPrice, decimal fees, long sequence) =>
        Trade(MovementType.Achat, id, date, accountId, securityId, quantity, unitPrice, fees, sequence);

    public static Movement Sell(Guid id, DateOnly date, Guid accountId, Guid securityId, decimal quantity, decimal unitPrice, decimal fees, long sequence) =>
        Trade(MovementType.Vente, id, date, accountId, securityId, quantity, unitPrice, fees, sequence);

    public static Movement CashMovement(MovementType type, DateOnly date, Guid accountId, decimal amount, long sequence) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Date = date,
        AccountId = accountId,
        Amount = amount,
        Sequence = sequence,
        CreatedAt = Timestamp,
        UpdatedAt = Timestamp,
    };

    public static Balance Balance(Guid accountId, DateOnly date, decimal amount) => new()
    {
        AccountId = accountId,
        Date = date,
        Amount = amount,
        UpdatedAt = Timestamp,
    };

    public static SecurityPrice Price(Guid securityId, DateOnly date, decimal price) => new()
    {
        SecurityId = securityId,
        Date = date,
        Price = price,
        UpdatedAt = Timestamp,
    };

    public static Guid Id(int value) => new(value, 0, 0, new byte[8]);

    private static Movement Trade(MovementType type, Guid id, DateOnly date, Guid accountId, Guid securityId, decimal quantity, decimal unitPrice, decimal fees, long sequence) => new()
    {
        Id = id,
        Type = type,
        Date = date,
        AccountId = accountId,
        SecurityId = securityId,
        Quantity = quantity,
        UnitPrice = unitPrice,
        Fees = fees,
        Sequence = sequence,
        CreatedAt = Timestamp,
        UpdatedAt = Timestamp,
    };
}
