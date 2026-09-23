using System.Net;
using FinanceReport.Application.Dtos;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

public sealed class HistoryApiTests
{
    private static readonly TimeSpan Paris = TimeSpan.FromHours(2);

    [Fact] // TC-FUNC-26
    public async Task History_returns_snapshots_of_the_period_in_date_order()
    {
        using var factory = ApiFactory.StartingAt(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(1)));
        var client = await factory.CreateAuthenticatedClientAsync();
        var institution = await client.PostJsonAsync<ReferentialItemDto>("/api/referentials/institutions", new { label = "Banque" });
        var account = await client.PostJsonAsync<AccountDto>("/api/accounts", new { name = "Livret", type = "LIVRET", institutionId = institution.Id });

        foreach (var (month, day, amount) in new[] { (8, 20, 100m), (9, 1, 200m), (9, 23, 300m) })
        {
            await factory.MoveClockAsync(client, new DateTimeOffset(2026, month, day, 10, 0, 0, Paris));
            await client.PutJsonAsync<BalanceDto>($"/api/accounts/{account.Id}/balances/2026-{month:D2}-{day:D2}", new { amount });
        }

        (await History(client, "1M")).Select(p => p.Date).Should().Equal(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 23));
        (await History(client, "1A")).Should().HaveCount(4);
        var all = await History(client, "ALL");
        all.Select(p => (p.Date, p.TotalNetWorth)).Should().Equal(
            (new DateOnly(2026, 1, 1), 0m),
            (new DateOnly(2026, 8, 20), 100m),
            (new DateOnly(2026, 9, 1), 200m),
            (new DateOnly(2026, 9, 23), 300m));

        await (await client.GetAsync("/api/dashboard/history?period=6M")).ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "period");
    }

    private static async Task<IReadOnlyList<HistoryPointDto>> History(HttpClient client, string period) =>
        (await client.GetJsonAsync<HistoryDto>($"/api/dashboard/history?period={period}")).Points;
}
