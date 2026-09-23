using FinanceReport.Infrastructure.Time;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace FinanceReport.Application.Tests.Persistence;

public class ParisClockTests
{
    [Theory] // RG-14, TC-FUNC-13 variante : le jour change à minuit heure de Paris
    [InlineData("2026-09-23T21:59:00Z", 2026, 9, 23)] // 23:59 à Paris (UTC+2)
    [InlineData("2026-09-23T22:01:00Z", 2026, 9, 24)] // 00:01 à Paris
    [InlineData("2026-12-31T22:59:00Z", 2026, 12, 31)] // 23:59 à Paris (UTC+1)
    [InlineData("2026-12-31T23:01:00Z", 2027, 1, 1)]
    public void Today_is_the_calendar_date_in_Paris(string utc, int year, int month, int day)
    {
        var clock = new ParisClock(new FakeTimeProvider(DateTimeOffset.Parse(utc, System.Globalization.CultureInfo.InvariantCulture)));

        clock.Today.Should().Be(new DateOnly(year, month, day));
    }
}
