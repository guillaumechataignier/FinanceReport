using Serilog;
using Serilog.Events;

namespace FinanceReport.Api.Logging;

/// <summary>Journalisation Serilog (TS §4.1) : fichier journalier conservé 31 jours, console en développement.</summary>
public static class LoggingSetup
{
    public const string OutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    public static void Configure(LoggerConfiguration logger, IConfiguration configuration, IHostEnvironment environment)
    {
        var logPath = configuration.GetValue<string>("Logs:Path") ?? "./logs";

        logger
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.File(
                Path.Combine(logPath, "financereport-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                shared: false,
                outputTemplate: OutputTemplate);

        if (environment.IsDevelopment())
        {
            logger.WriteTo.Console(outputTemplate: OutputTemplate);
        }
    }
}
