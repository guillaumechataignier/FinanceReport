using FinanceReport.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceReport.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AuthService>(ServiceLifetime.Singleton);
        services.AddSingleton<AuthService>();
        services.AddSingleton<SnapshotService>();
        services.AddSingleton<ReferentialService>();
        services.AddSingleton<AccountService>();
        services.AddSingleton<BalanceService>();
        services.AddSingleton<SecurityService>();
        services.AddSingleton<MovementService>();
        services.AddSingleton<PriceService>();
        return services;
    }
}
