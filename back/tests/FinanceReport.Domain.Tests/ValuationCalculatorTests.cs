using FinanceReport.Domain.Calculators;
using FinanceReport.Domain.Enums;
using FluentAssertions;
using static FinanceReport.Domain.Tests.ReferenceData;

namespace FinanceReport.Domain.Tests;

public class ValuationCalculatorTests
{
    private static Valuation ValuateReference(DateOnly? asOf = null) =>
        ValuationCalculator.Valuate(asOf ?? Today, Accounts, Movements, Balances, Prices);

    [Fact] // TC-FUNC-06
    public void Unrealized_gain_uses_latest_price()
    {
        var position = ValuateReference().Positions.Should().ContainSingle().Subject;

        position.Price.Should().Be(115m);
        position.PriceDate.Should().Be(new DateOnly(2026, 9, 22));
        position.MissingPrice.Should().BeFalse();
        position.MarketValue.Should().Be(1035m);
        Math.Round(position.UnrealizedGain, 2).Should().Be(103.20m);
        Math.Round(position.CostBasis, 2).Should().Be(931.80m);
        Math.Round(position.UnrealizedGain / position.CostBasis * 100m, 2).Should().Be(11.08m);
    }

    [Fact] // TC-FUNC-07
    public void Account_values_and_net_worth_exclude_archived_accounts()
    {
        var valuation = ValuateReference();

        valuation.Accounts.Select(a => a.AccountId).Should().BeEquivalentTo([AccountA, AccountB]);
        var a = valuation.Accounts.Single(x => x.AccountId == AccountA);
        var b = valuation.Accounts.Single(x => x.AccountId == AccountB);

        a.Value.Should().Be(1235m);
        a.Cash.Should().Be(200m);
        a.CashDate.Should().Be(new DateOnly(2026, 9, 1));
        Math.Round(a.UnrealizedGain, 2).Should().Be(103.20m);
        Math.Round(a.RealizedGain, 2).Should().Be(97.30m);

        b.Value.Should().Be(5000m);
        b.Type.Should().Be(AccountType.Livret);
        b.RealizedGain.Should().Be(0m);

        valuation.TotalNetWorth.Should().Be(6235m);
    }

    [Fact] // TC-FUNC-23 : un compte désarchivé est réintégré
    public void Unarchived_account_is_included_again()
    {
        var accounts = Accounts.Select(a => a with { Archived = false });

        var valuation = ValuationCalculator.Valuate(Today, accounts, Movements, Balances, Prices);

        valuation.TotalNetWorth.Should().Be(7235m);
    }

    [Fact] // TC-FUNC-09
    public void Position_without_price_is_valued_at_average_cost()
    {
        var movements = Movements.Append(
            Buy(Id(0x300), new DateOnly(2026, 9, 15), AccountA, S2, 0.01m, 50000.00m, 5.00m, sequence: 4));

        var valuation = ValuationCalculator.Valuate(Today, Accounts, movements, Balances, Prices);
        var s2 = valuation.Positions.Single(p => p.SecurityId == S2);

        s2.AverageCost.Should().Be(50500m);
        s2.Price.Should().Be(50500m);
        s2.PriceDate.Should().BeNull();
        s2.MarketValue.Should().Be(505m);
        s2.UnrealizedGain.Should().Be(0m);
        s2.MissingPrice.Should().BeTrue();
        valuation.Positions.Count(p => p.MissingPrice).Should().Be(1);
    }

    [Theory] // TC-FUNC-19
    [InlineData(2026, 9, 9, 100)]
    [InlineData(2026, 9, 10, 110)]
    [InlineData(2026, 9, 21, 110)]
    [InlineData(2026, 9, 23, 115)]
    public void Retained_price_is_the_latest_on_or_before_the_valuation_date(int year, int month, int day, int expected)
    {
        var prices = new[]
        {
            Price(S1, new DateOnly(2026, 9, 1), 100m),
            Price(S1, new DateOnly(2026, 9, 22), 115m),
            Price(S1, new DateOnly(2026, 9, 10), 110m),
        };

        var valuation = ValuationCalculator.Valuate(new DateOnly(year, month, day), Accounts, Movements, Balances, prices);

        valuation.Positions.Single().Price.Should().Be(expected);
    }

    [Theory] // TC-FUNC-19, soldes
    [InlineData(2026, 8, 31, 0)]
    [InlineData(2026, 9, 1, 5000)]
    public void Retained_balance_is_the_latest_on_or_before_the_valuation_date(int year, int month, int day, int expected)
    {
        var valuation = ValuateReference(new DateOnly(year, month, day));

        valuation.Accounts.Single(a => a.AccountId == AccountB).Value.Should().Be(expected);
    }

    [Fact] // TC-FUNC-18
    public void Deposits_do_not_change_cash()
    {
        var movements = Movements.Append(
            CashMovement(MovementType.Versement, new DateOnly(2026, 9, 20), AccountA, 1000m, sequence: 4));

        var valuation = ValuationCalculator.Valuate(Today, Accounts, movements, Balances, Prices);
        var a = valuation.Accounts.Single(x => x.AccountId == AccountA);

        a.Cash.Should().Be(200m);
        a.Value.Should().Be(1235m);
    }

    [Fact]
    public void Closed_positions_are_not_listed_but_keep_their_realized_gain()
    {
        var movements = Movements.Append(
            Sell(Id(0x104), new DateOnly(2026, 4, 1), AccountA, S1, 9m, 125.00m, 0m, sequence: 4));

        var valuation = ValuationCalculator.Valuate(Today, Accounts, movements, Balances, Prices);
        var a = valuation.Accounts.Single(x => x.AccountId == AccountA);

        valuation.Positions.Should().BeEmpty();
        a.Value.Should().Be(200m);
        Math.Round(a.RealizedGain, 2).Should().Be(290.50m);
    }

    [Fact] // RG-09 : 0 sans compte
    public void Net_worth_is_zero_without_accounts()
    {
        var valuation = ValuationCalculator.Valuate(Today, [], [], [], []);

        valuation.TotalNetWorth.Should().Be(0m);
        valuation.Accounts.Should().BeEmpty();
    }
}
