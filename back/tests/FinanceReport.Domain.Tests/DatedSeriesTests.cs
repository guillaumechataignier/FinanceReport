using FinanceReport.Domain.Calculators;
using FluentAssertions;

namespace FinanceReport.Domain.Tests;

public class DatedSeriesTests
{
    private static readonly DatedSeries<string> Series = DatedSeries<string>.Create(
        new[] { ("k", 10, 1m), ("k", 20, 2m), ("k", 5, 0.5m), ("other", 1, 9m) },
        x => x.Item1,
        x => new DateOnly(2026, 1, x.Item2),
        x => x.Item3);

    [Theory]
    [InlineData(4, null)]
    [InlineData(5, 0.5)]
    [InlineData(9, 0.5)]
    [InlineData(10, 1.0)]
    [InlineData(19, 1.0)]
    [InlineData(31, 2.0)]
    public void Latest_returns_last_value_on_or_before_date(int day, double? expected)
    {
        var latest = Series.Latest("k", new DateOnly(2026, 1, day));

        latest?.Value.Should().Be((decimal?)expected);
        (latest is null).Should().Be(expected is null);
    }

    [Fact]
    public void Latest_returns_null_for_unknown_key()
    {
        Series.Latest("missing", DateOnly.MaxValue).Should().BeNull();
    }
}
