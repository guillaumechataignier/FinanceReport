using System.Net;
using FinanceReport.Application.Dtos;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

public sealed class SnapshotApiTests
{
    private static readonly TimeSpan Paris = TimeSpan.FromHours(2);

    [Fact] // TC-FUNC-11
    public async Task Backdated_price_recomputes_later_snapshots_only()
    {
        using var factory = ApiFactory.StartingAt(new DateTimeOffset(2026, 9, 1, 10, 0, 0, Paris));
        var client = await factory.CreateAuthenticatedClientAsync();
        var data = await ReferenceDataSet.LoadAsync(client, withPrice: false);

        await factory.MoveClockAsync(client, new DateTimeOffset(2026, 9, 15, 10, 0, 0, Paris));
        await client.PutJsonAsync<BalanceDto>($"/api/accounts/{data.A}/balances/2026-09-01", new { amount = 200.00m });
        await factory.MoveClockAsync(client, new DateTimeOffset(2026, 9, 23, 10, 0, 0, Paris));
        await client.PutJsonAsync<PriceDto>($"/api/securities/{data.S1}/prices/2026-09-22", new { price = 115.00m });

        var before = await SnapshotFile.ReadAsync(factory);
        before.Keys.Should().Contain(["2026-09-01", "2026-09-15", "2026-09-23"]);

        await client.PutJsonAsync<PriceDto>($"/api/securities/{data.S1}/prices/2026-09-10", new { price = 105.00m });

        var after = await SnapshotFile.ReadAsync(factory);
        after["2026-09-01"].GetRawText().Should().Be(before["2026-09-01"].GetRawText());
        after["2026-09-15"].AccountValue(data.A).Should().Be(1145.00m);
        after["2026-09-23"].AccountValue(data.A).Should().Be(1235.00m);
    }

    [Fact] // TC-FUNC-13
    public async Task Only_one_snapshot_per_day_is_kept()
    {
        using var factory = new ApiFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var data = await ReferenceDataSet.LoadAsync(client);

        await client.PutJsonAsync<PriceDto>($"/api/securities/{data.S1}/prices/2026-09-23", new { price = 115m });
        await client.PutJsonAsync<PriceDto>($"/api/securities/{data.S1}/prices/2026-09-23", new { price = 118m });

        var snapshots = await SnapshotFile.ReadAsync(factory);
        snapshots.Keys.Should().Equal("2026-09-23");
        snapshots["2026-09-23"].Total().Should().Be(6262.00m);
    }

    [Fact] // TC-FUNC-13, variante : 23:59 puis 00:01 à Paris
    public async Task Day_boundary_follows_Paris_time()
    {
        using var factory = ApiFactory.StartingAt(new DateTimeOffset(2026, 9, 23, 23, 59, 0, Paris));
        var client = await factory.CreateAuthenticatedClientAsync();
        var data = await ReferenceDataSet.LoadAsync(client);

        factory.Time.Advance(TimeSpan.FromMinutes(2));
        await client.PutJsonAsync<PriceDto>($"/api/securities/{data.S1}/prices/2026-09-24", new { price = 118m });

        (await SnapshotFile.ReadAsync(factory)).Keys.Should().Equal("2026-09-23", "2026-09-24");
    }

    [Fact] // TC-TECH-04 par l'API
    public async Task Failed_snapshot_write_restores_movements_and_returns_500()
    {
        using var factory = new ApiFactory();
        var client = await factory.CreateAuthenticatedClientAsync();
        var data = await ReferenceDataSet.LoadAsync(client);
        var movementsBefore = await File.ReadAllTextAsync(factory.FilePath("movements"));
        Directory.CreateDirectory(factory.FilePath("snapshots") + ".tmp");

        var response = await client.PostAsync(
            "/api/movements",
            new { type = "ACHAT", date = "2026-09-20", accountId = data.A, securityId = data.S1, quantity = 1m, unitPrice = 115m, fees = 0m });

        await response.ShouldBeErrorAsync(HttpStatusCode.InternalServerError, "INTERNAL_ERROR");
        (await File.ReadAllTextAsync(factory.FilePath("movements"))).Should().Be(movementsBefore);
        (await client.GetJsonAsync<List<MovementDto>>("/api/movements")).Should().HaveCount(3);
        File.Exists(factory.FilePath("movements") + ".tmp").Should().BeFalse();
    }
}
