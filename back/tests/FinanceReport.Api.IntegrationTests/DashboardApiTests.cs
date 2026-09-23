using System.Net;
using FinanceReport.Application.Dtos;
using FinanceReport.Domain.Enums;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

public sealed class DashboardApiTests : IAsyncLifetime
{
    private readonly ApiFactory _factory = new();
    private HttpClient _client = null!;
    private ReferenceDataSet _data = null!;

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
        _data = await ReferenceDataSet.LoadAsync(_client);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact] // TC-FUNC-03, TC-FUNC-06 (TI)
    public async Task Position_shows_rounded_average_cost_and_unrealized_gain()
    {
        var positions = await Positions("");

        var s1 = positions.Positions.Should().ContainSingle().Subject;
        s1.AccountName.Should().Be("PEA Test");
        s1.SecurityCode.Should().Be("LU1681043599");
        s1.Quantity.Should().Be(9m);
        s1.AverageCost.Should().Be(103.53m);
        s1.Price.Should().Be(115.00m);
        s1.PriceDate.Should().Be(new DateOnly(2026, 9, 22));
        s1.MarketValue.Should().Be(1035.00m);
        s1.UnrealizedGain.Should().Be(103.20m);
        s1.UnrealizedGainPercent.Should().Be(11.08m);
        s1.MissingPrice.Should().BeFalse();
    }

    [Fact] // TC-FUNC-06, TC-FUNC-07
    public async Task Summary_shows_net_worth_gain_and_account_type_split()
    {
        var summary = await _client.GetJsonAsync<SummaryDto>("/api/dashboard/summary");

        summary.Date.Should().Be(new DateOnly(2026, 9, 23));
        summary.TotalNetWorth.Should().Be(6235.00m);
        summary.UnrealizedGain.Amount.Should().Be(103.20m);
        summary.UnrealizedGain.Percent.Should().Be(11.08m);
        summary.ByAccountType.Select(t => (t.Type, t.Amount, t.Percent)).Should().Equal(
            (AccountType.Livret, 5000.00m, 80.19m),
            (AccountType.Pea, 1235.00m, 19.81m));
        summary.MissingPriceCount.Should().Be(0);
        summary.MonthVariation.Should().BeNull("tous les snapshots datent de septembre");
    }

    [Fact] // TC-FUNC-05, TC-FUNC-07
    public async Task Accounts_table_shows_values_gains_and_total()
    {
        var table = await _client.GetJsonAsync<DashboardAccountsDto>("/api/dashboard/accounts");

        table.Accounts.Select(a => a.AccountId).Should().Equal(_data.B, _data.A);
        var a = table.Accounts.Single(x => x.AccountId == _data.A);
        a.Value.Should().Be(1235.00m);
        a.UnrealizedGain.Should().Be(103.20m);
        a.RealizedGain.Should().Be(97.30m);
        var b = table.Accounts.Single(x => x.AccountId == _data.B);
        b.UnrealizedGain.Should().BeNull();
        b.RealizedGain.Should().BeNull();
        table.Total.Should().Be(new DashboardAccountsTotalDto(6235.00m, 103.20m, 97.30m));

        (await _client.GetJsonAsync<DashboardAccountsDto>($"/api/dashboard/accounts?accountIds={_data.A}"))
            .Accounts.Should().ContainSingle();
    }

    [Fact] // TC-FUNC-09
    public async Task Position_without_price_is_valued_at_average_cost_and_flagged()
    {
        await _client.PostJsonAsync<MovementDto>(
            "/api/movements",
            new { type = "ACHAT", date = "2026-09-15", accountId = _data.A, securityId = _data.S2, quantity = 0.01m, unitPrice = 50000.00m, fees = 5.00m });

        var s2 = (await Positions("")).Positions.Single(p => p.SecurityId == _data.S2);
        s2.AverageCost.Should().Be(50500.00m);
        s2.MarketValue.Should().Be(505.00m);
        s2.UnrealizedGain.Should().Be(0m);
        s2.MissingPrice.Should().BeTrue();
        s2.PriceDate.Should().BeNull();
        (await _client.GetJsonAsync<SummaryDto>("/api/dashboard/summary")).MissingPriceCount.Should().Be(1);
    }

    [Fact] // TC-FUNC-23, étapes 2 et 3
    public async Task Archiving_excludes_and_unarchiving_restores_an_account()
    {
        await _client.PostJsonAsync<AccountDto>($"/api/accounts/{_data.C}/unarchive");
        (await _client.GetJsonAsync<SummaryDto>("/api/dashboard/summary")).TotalNetWorth.Should().Be(7235.00m);

        await _client.PostJsonAsync<AccountDto>($"/api/accounts/{_data.A}/archive");
        (await _client.GetJsonAsync<SummaryDto>("/api/dashboard/summary")).TotalNetWorth.Should().Be(6000.00m);
    }

    [Fact] // TC-FUNC-24
    public async Task Security_filters_exclude_cash()
    {
        var all = await Positions("");
        all.IncludesCash.Should().BeTrue();
        all.Allocation.ByAccount.Select(s => (s.Label, s.Amount)).Should().Equal(("Livret A", 5000.00m), ("PEA Test", 1235.00m));
        all.Allocation.ByZone.Select(s => (s.Key, s.Label, s.Amount)).Should().Equal(
            ("CASH", "Espèces et liquidités", 5200.00m),
            ("MONDE", "Monde", 1035.00m));
        all.Allocation.BySecurityType.Select(s => (s.Label, s.Amount, s.Percent)).Should().Equal(
            ("Espèces et liquidités", 5200.00m, 83.40m),
            ("ETF", 1035.00m, 16.60m));

        var monde = await Positions("?zones=MONDE");
        monde.IncludesCash.Should().BeFalse();
        monde.Allocation.ByAccount.Select(s => s.Amount).Should().Equal(1035.00m);
        monde.Allocation.ByZone.Select(s => (s.Label, s.Amount, s.Percent)).Should().Equal(("Monde", 1035.00m, 100.00m));

        var accountA = await Positions($"?accountIds={_data.A}");
        accountA.IncludesCash.Should().BeTrue();
        accountA.Allocation.ByAccount.Should().ContainSingle().Which.Amount.Should().Be(1235.00m);
    }

    [Fact] // FS §3.10 : filtres combinés (ET entre critères, OU dans un critère)
    public async Task Filters_combine_with_and_between_criteria_and_or_within()
    {
        (await Positions("?securityTypes=ETF,CRYPTO&zones=MONDE")).Positions.Should().ContainSingle();
        (await Positions("?securityTypes=ETF&securityTypes=ACTION")).Positions.Should().ContainSingle();
        (await Positions("?securityTypes=ACTION")).Positions.Should().BeEmpty();
        (await Positions("?zones=EUROPE&sectors=DIVERSIFIE")).Positions.Should().BeEmpty();
        (await Positions($"?accountIds={_data.B}")).Positions.Should().BeEmpty();
        await (await _client.GetAsync("/api/dashboard/positions?securityTypes=FONDS"))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "securityTypes");
    }

    [Fact] // TC-FUNC-28, étapes 4 et 7 (tableau de bord)
    public async Task Dashboard_uses_current_labels_including_archived_values()
    {
        var zones = await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/zones");
        await _client.PutJsonAsync<ReferentialItemDto>($"/api/referentials/zones/{zones.Single(z => z.Code == "MONDE").Id}", new { code = "MONDE", label = "Monde entier" });
        var sectors = await _client.GetJsonAsync<List<ReferentialItemDto>>("/api/referentials/sectors");
        await _client.PostJsonAsync<ReferentialItemDto>($"/api/referentials/sectors/{sectors.Single(s => s.Code == "DIVERSIFIE").Id}/archive");

        var positions = await Positions("");
        positions.Positions.Single().ZoneLabel.Should().Be("Monde entier");
        positions.Allocation.BySector.Single(s => s.Key == "DIVERSIFIE").Should().Be(new AllocationShareDto("DIVERSIFIE", "Diversifié", 1035.00m, 16.60m));
    }

    [Fact] // TC-FUNC-24 et UC-13 : aucune position pour les filtres
    public async Task Empty_result_has_empty_allocations()
    {
        var result = await Positions("?sectors=SANTE");

        result.Positions.Should().BeEmpty();
        result.Allocation.ByZone.Should().BeEmpty();
        result.Allocation.ByAccount.Should().BeEmpty();
    }

    private Task<DashboardPositionsDto> Positions(string query) =>
        _client.GetJsonAsync<DashboardPositionsDto>($"/api/dashboard/positions{query}");
}
