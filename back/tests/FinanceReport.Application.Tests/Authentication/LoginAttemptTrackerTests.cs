using FinanceReport.Infrastructure.Authentication;
using FinanceReport.Infrastructure.Time;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace FinanceReport.Application.Tests.Authentication;

public class LoginAttemptTrackerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 23, 7, 0, 0, TimeSpan.Zero));
    private readonly LoginAttemptTracker _tracker;

    public LoginAttemptTrackerTests() => _tracker = new LoginAttemptTracker(new ParisClock(_time));

    [Fact] // TC-FUNC-16, étape 1
    public void Four_failures_then_reset_do_not_lock()
    {
        for (var i = 0; i < 4; i++)
        {
            _tracker.RegisterFailure().Should().BeFalse();
        }

        _tracker.RemainingLockout().Should().BeNull();
        _tracker.Reset();

        for (var i = 0; i < 4; i++)
        {
            _tracker.RegisterFailure().Should().BeFalse("le compteur a été remis à zéro");
        }
    }

    [Fact] // TC-FUNC-16, étapes 2 à 4
    public void Fifth_failure_locks_for_15_minutes()
    {
        for (var i = 0; i < 4; i++)
        {
            _tracker.RegisterFailure();
        }

        _tracker.RegisterFailure().Should().BeTrue();
        _tracker.RemainingLockout().Should().Be(TimeSpan.FromMinutes(15));

        _time.Advance(new TimeSpan(0, 14, 59));
        _tracker.RemainingLockout().Should().Be(TimeSpan.FromSeconds(1));

        _time.Advance(TimeSpan.FromSeconds(2));
        _tracker.RemainingLockout().Should().BeNull();
    }

    [Fact]
    public void Counter_restarts_after_lockout()
    {
        for (var i = 0; i < 5; i++)
        {
            _tracker.RegisterFailure();
        }

        _time.Advance(TimeSpan.FromMinutes(16));

        _tracker.RegisterFailure().Should().BeFalse("le compteur repart de zéro après le blocage");
    }
}
