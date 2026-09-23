using FinanceReport.Application.Services;
using FinanceReport.Application.Tests.Support;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FluentAssertions;

namespace FinanceReport.Application.Tests.Services;

/// <summary>TC-FUNC-14 : variation du mois. Le patrimoine du jour vaut 6 235,00 (un livret).</summary>
public class DashboardServiceTests
{
    private static readonly DateOnly Today = new(2026, 9, 23);
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 8, 0, 0, TimeSpan.Zero);

    [Fact] // a
    public void Variation_uses_last_snapshot_before_first_of_month()
    {
        var summary = Service(Snapshot(2026, 8, 31, 6000m), Snapshot(2026, 9, 23, 6235m)).Summary();

        summary.MonthVariation.Should().NotBeNull();
        summary.MonthVariation!.Amount.Should().Be(235.00m);
        summary.MonthVariation.Percent.Should().Be(3.92m);
        summary.MonthVariation.ReferenceDate.Should().Be(new DateOnly(2026, 8, 31));
    }

    [Fact] // b
    public void Reference_can_be_older_than_last_day_of_previous_month()
    {
        var summary = Service(Snapshot(2026, 7, 1, 1m), Snapshot(2026, 8, 15, 5800m), Snapshot(2026, 9, 23, 6235m)).Summary();

        summary.MonthVariation!.ReferenceDate.Should().Be(new DateOnly(2026, 8, 15));
        summary.MonthVariation.Amount.Should().Be(435.00m);
    }

    [Fact] // c
    public void Variation_is_null_without_snapshot_before_the_month()
    {
        Service(Snapshot(2026, 9, 1, 6000m), Snapshot(2026, 9, 23, 6235m)).Summary().MonthVariation.Should().BeNull();
    }

    [Fact] // d
    public void Percent_is_null_when_reference_is_zero()
    {
        var summary = Service(Snapshot(2026, 8, 31, 0m)).Summary();

        summary.MonthVariation!.Amount.Should().Be(6235.00m);
        summary.MonthVariation.Percent.Should().BeNull();
    }

    [Fact]
    public void Summary_without_position_has_no_gain_percent()
    {
        var summary = Service().Summary();

        summary.TotalNetWorth.Should().Be(6235.00m);
        summary.UnrealizedGain.Amount.Should().Be(0m);
        summary.UnrealizedGain.Percent.Should().BeNull();
        summary.ByAccountType.Should().ContainSingle().Which.Percent.Should().Be(100m);
    }

    [Theory] // UC-12 : période inconnue
    [InlineData(null)]
    [InlineData("2A")]
    [InlineData("1m")]
    public void Unknown_period_is_rejected(string? period)
    {
        var act = () => Service().History(period);

        act.Should().Throw<Exceptions.ValidationException>();
    }

    private static DashboardService Service(params Snapshot[] snapshots)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "Livret",
            Type = AccountType.Livret,
            InstitutionId = Guid.NewGuid(),
            CreatedAt = Now,
            UpdatedAt = Now,
        };

        return new DashboardService(
            new InMemoryRepository<Account>([account]),
            new InMemoryRepository<Balance>([new Balance { AccountId = account.Id, Date = new DateOnly(2026, 9, 1), Amount = 6235m, UpdatedAt = Now }]),
            new InMemoryRepository<Movement>(),
            new InMemoryRepository<SecurityPrice>(),
            new InMemoryRepository<Security>(),
            new InMemoryRepository<Snapshot>(snapshots),
            new InMemoryReferentialRepository(),
            new FixedClock(Today));
    }

    private static Snapshot Snapshot(int year, int month, int day, decimal total) => new()
    {
        Date = new DateOnly(year, month, day),
        TotalNetWorth = total,
        ComputedAt = Now,
        Accounts = [],
        Positions = [],
    };
}
