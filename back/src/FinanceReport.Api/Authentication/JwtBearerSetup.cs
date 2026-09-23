using FinanceReport.Api.Errors;
using FinanceReport.Application.Abstractions;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FinanceReport.Api.Authentication;

public static class JwtBearerSetup
{
    /// <summary>
    /// Authentification Bearer (RG-17) : la clé est lue dans <c>credentials.json</c> à chaque validation et la durée de vie
    /// est contrôlée avec <see cref="IClock"/>. Les contrôleurs sont protégés par <c>MapControllers().RequireAuthorization()</c>
    /// (sauf [AllowAnonymous]) ; les routes inconnues restent en 404.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<ICredentialsStore, IClock>((options, credentialsStore, clock) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = JwtSettings.Issuer,
                    ValidAudience = JwtSettings.Audience,
                    ValidAlgorithms = [JwtSettings.Algorithm],
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true,
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    IssuerSigningKeyResolver = (_, _, _, _) =>
                        credentialsStore.Get() is { } credentials ? [JwtSettings.SigningKey(credentials.JwtSigningKey)] : [],
                    LifetimeValidator = (notBefore, expires, _, _) =>
                    {
                        var now = clock.UtcNow.UtcDateTime;
                        return expires > now && (notBefore is null || notBefore <= now);
                    },
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        var message = context.AuthenticateFailure is null
                            ? "Authentification requise."
                            : "Session expirée ou jeton invalide. Veuillez vous reconnecter.";
                        return ErrorResponses.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, ErrorCodes.Unauthorized, message);
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }
}
