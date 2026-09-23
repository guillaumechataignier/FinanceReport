using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FinanceReport.Infrastructure.Backup;
using FinanceReport.Infrastructure.Persistence;
using FinanceReport.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceReport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, ParisClock>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<IUnitOfWork, UnitOfWork>();

        services.AddStore<Account>("accounts");
        services.AddStore<Balance>("balances");
        services.AddStore<Security>("securities");
        services.AddStore<Movement>("movements");
        services.AddStore<SecurityPrice>("prices");
        services.AddStore<Snapshot>("snapshots");

        services.AddSingleton<ReferentialRepository>();
        services.AddSingleton<IReferentialRepository>(sp => sp.GetRequiredService<ReferentialRepository>());
        services.AddSingleton<ReferentialSeeder>();

        return services;
    }

    private static void AddStore<T>(this IServiceCollection services, string entityName)
    {
        services.AddSingleton(sp => new JsonFileStore<T>(
            entityName,
            sp.GetRequiredService<IOptions<StorageOptions>>(),
            sp.GetRequiredService<BackupService>(),
            sp.GetRequiredService<ILogger<JsonFileStore<T>>>()));
        services.AddSingleton<IRepository<T>>(sp => sp.GetRequiredService<JsonFileStore<T>>());
    }
}
