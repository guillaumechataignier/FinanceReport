using FinanceReport.Application.Dtos;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>Jeu de données de référence (TestPlan §1.3), entièrement chargé par l'API.</summary>
internal sealed class ReferenceDataSet
{
    public Guid E1 { get; private set; }
    public Guid E2 { get; private set; }
    public Guid A { get; private set; }
    public Guid B { get; private set; }
    public Guid C { get; private set; }
    public Guid S1 { get; private set; }
    public Guid S2 { get; private set; }
    public Guid M1 { get; private set; }
    public Guid M2 { get; private set; }
    public Guid M3 { get; private set; }

    /// <param name="withPrice">Saisit le cours de S1 au 22/09/2026 (impossible si l'horloge simulée est antérieure).</param>
    public static async Task<ReferenceDataSet> LoadAsync(HttpClient client, bool withPrice = true)
    {
        var data = new ReferenceDataSet();

        data.E1 = (await client.PostJsonAsync<ReferentialItemDto>("/api/referentials/institutions", new { label = "Boursorama" })).Id;
        data.E2 = (await client.PostJsonAsync<ReferentialItemDto>("/api/referentials/institutions", new { label = "Banque Test" })).Id;

        data.A = await CreateAccount(client, "PEA Test", "PEA", data.E1);
        data.B = await CreateAccount(client, "Livret A", "LIVRET", data.E2);
        data.C = await CreateAccount(client, "Ancien CTO", "CTO", data.E1);

        data.S1 = await CreateSecurity(client, "ETF Monde", "LU1681043599", "ETF", "MONDE", "DIVERSIFIE");
        data.S2 = await CreateSecurity(client, "Bitcoin", "BTC", "CRYPTO", "MONDE", "NON_APPLICABLE");

        data.M1 = await CreateTrade(client, "ACHAT", "2026-01-10", data.A, data.S1, 10m, 100.00m, 2.00m);
        data.M2 = await CreateTrade(client, "ACHAT", "2026-02-10", data.A, data.S1, 5m, 110.00m, 1.00m);
        data.M3 = await CreateTrade(client, "VENTE", "2026-03-10", data.A, data.S1, 6m, 120.00m, 1.50m);

        await client.PutJsonAsync<BalanceDto>($"/api/accounts/{data.B}/balances/2026-09-01", new { amount = 5000.00m });
        await client.PutJsonAsync<BalanceDto>($"/api/accounts/{data.C}/balances/2026-01-01", new { amount = 1000.00m });
        await client.PutJsonAsync<BalanceDto>($"/api/accounts/{data.A}/balances/2026-09-01", new { amount = 200.00m });
        await client.PostJsonAsync<AccountDto>($"/api/accounts/{data.C}/archive");

        if (withPrice)
        {
            await client.PutJsonAsync<PriceDto>($"/api/securities/{data.S1}/prices/2026-09-22", new { price = 115.00m });
        }

        return data;
    }

    public static async Task<Guid> CreateTrade(
        HttpClient client, string type, string date, Guid accountId, Guid securityId, decimal quantity, decimal unitPrice, decimal fees) =>
        (await client.PostJsonAsync<MovementDto>(
            "/api/movements",
            new { type, date, accountId, securityId, quantity, unitPrice, fees })).Id;

    private static async Task<Guid> CreateAccount(HttpClient client, string name, string type, Guid institutionId) =>
        (await client.PostJsonAsync<AccountDto>("/api/accounts", new { name, type, institutionId })).Id;

    private static async Task<Guid> CreateSecurity(HttpClient client, string name, string code, string type, string zone, string sector) =>
        (await client.PostJsonAsync<SecurityDto>("/api/securities", new { name, code, type, zone, sector })).Id;
}
