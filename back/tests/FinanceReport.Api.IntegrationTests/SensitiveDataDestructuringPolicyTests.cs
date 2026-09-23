using FinanceReport.Api.Logging;
using FluentAssertions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace FinanceReport.Api.IntegrationTests;

public class SensitiveDataDestructuringPolicyTests
{
    [Fact] // RG-20
    public void Sensitive_properties_are_masked_when_destructured()
    {
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Requête {@Request}", new { Username = "guillaume", Password = "secret-123", PasswordConfirmation = "secret-123", Token = "eyJ", PasswordHash = "$2a$12$x" });

        var rendered = sink.Events.Single().RenderMessage();
        rendered.Should().Contain("guillaume");
        rendered.Should().NotContain("secret-123").And.NotContain("eyJ").And.NotContain("$2a$");
    }

    [Theory]
    [InlineData("password", true)]
    [InlineData("PasswordConfirmation", true)]
    [InlineData("token", true)]
    [InlineData("passwordHash", true)]
    [InlineData("JwtSigningKey", true)]
    [InlineData("username", false)]
    [InlineData("amount", false)]
    public void Detects_sensitive_property_names(string name, bool expected)
    {
        SensitiveDataDestructuringPolicy.IsSensitive(name).Should().Be(expected);
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
