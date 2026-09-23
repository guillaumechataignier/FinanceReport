using FinanceReport.Application.Dtos;
using FinanceReport.Application.Services;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>
/// Jeu de données de référence (TestPlan §1.3), chargé par l'API. Les mouvements et le cours de S1 sont écrits
/// directement dans les dépôts tant que les endpoints de mouvements et de cours n'existent pas (phase 5).
/// </summary>
internal sealed class ReferenceDataSet
{
    public Guid E1 { get; private set; }
    public Guid E2 { get; private set; }
    public Guid A { get; private set; }
    public Guid B { get; private set; }
    public Guid C { get; private set; }
    public Guid S1 { get; private set; }
    public Guid S2 { get; private set; }
    public Guid M1 { get; } = Guid.NewGuid();
    public Guid M2 { get; } = Guid.NewGuid();
    public Guid M3 { get; } = Guid.NewGuid();

    public static async Task<ReferenceDataSet> LoadAsync(ApiFactory factory, HttpClient client)
    {
        var data = new ReferenceDataSet();

        data.E1 = (await client.PostJsonAsync<ReferentialItemDto>("/api/referentials/institutions", new { label = "Boursorama" })).Id;
        data.E2 = (await client.PostJsonAsync<ReferentialItemDto>("/api/referentials/institutions", new { label = "Banque Test" })).Id;

        data.A = await CreateAccount(client, "PEA Test", "PEA", data.E1);
        data.B = await CreateAccount(client, "Livret A", "LIVRET", data.E2);
        data.C = await CreateAccount(client, "Ancien CTO", "CTO", data.E1);

        data.S1 = await CreateSecurity(client, "ETF Monde", "LU1681043599", "ETF", "MONDE", "DIVERSIFIE");
        data.S2 = await CreateSecurity(client, "Bitcoin", "BTC", "CRYPTO", "MONDE", "NON_APPLICABLE");

        await client.PutJsonAsync<BalanceDto>($"/api/accounts/{data.B}/balances/2026-09-01", new { amount = 5000.00m });
        await client.PutJsonAsync<BalanceDto>($"/api/accounts/{data.C}/balances/2026-01-01", new { amount = 1000.00m });
        await client.PutJsonAsync<BalanceDto>($"/api/accounts/{data.A}/balances/2026-09-01", new { amount = 200.00m });
        await client.PostJsonAsync<AccountDto>($"/api/accounts/{data.C}/archive");

        await data.SeedMovementsAndPriceAsync(factory);
        return data;
    }

    private async Task SeedMovementsAndPriceAsync(ApiFactory factory)
    {
        var services = factory.Services;
        var now = factory.Time.GetUtcNow();
        Movement Trade(Guid id, MovementType type, DateOnly date, decimal quantity, decimal price, decimal fees, long sequence) => new()
        {
            Id = id,
            Type = type,
            Date = date,
            AccountId = A,
            SecurityId = S1,
            Quantity = quantity,
            UnitPrice = price,
            Fees = fees,
            Sequence = sequence,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await services.GetRequiredService<IUnitOfWork>().ExecuteAsync(() =>
        {
            services.GetRequiredService<IRepository<Movement>>().Save(
            [
                Trade(M1, MovementType.Achat, new DateOnly(2026, 1, 10), 10m, 100.00m, 2.00m, 1),
                Trade(M2, MovementType.Achat, new DateOnly(2026, 2, 10), 5m, 110.00m, 1.00m, 2),
                Trade(M3, MovementType.Vente, new DateOnly(2026, 3, 10), 6m, 120.00m, 1.50m, 3),
            ]);
            services.GetRequiredService<IRepository<SecurityPrice>>().Save(
            [
                new SecurityPrice { SecurityId = S1, Date = new DateOnly(2026, 9, 22), Price = 115.00m, UpdatedAt = now },
            ]);
            services.GetRequiredService<SnapshotService>().Rebuild(new DateOnly(2026, 1, 10));
            return 0;
        });
    }

    private static async Task<Guid> CreateAccount(HttpClient client, string name, string type, Guid institutionId) =>
        (await client.PostJsonAsync<AccountDto>("/api/accounts", new { name, type, institutionId })).Id;

    private static async Task<Guid> CreateSecurity(HttpClient client, string name, string code, string type, string zone, string sector) =>
        (await client.PostJsonAsync<SecurityDto>("/api/securities", new { name, code, type, zone, sector })).Id;
}
