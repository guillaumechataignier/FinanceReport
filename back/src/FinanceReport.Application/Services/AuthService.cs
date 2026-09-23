using FinanceReport.Application.Abstractions;
using FinanceReport.Application.Dtos;
using FinanceReport.Application.Exceptions;
using FinanceReport.Application.Validators;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinanceReport.Application.Services;

/// <summary>Initialisation du compte d'accès unique et connexion (TS §3.6, RG-16 à RG-18).</summary>
public sealed class AuthService(
    ICredentialsStore credentialsStore,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokenService,
    ILoginAttemptTracker attemptTracker,
    IValidator<SetupRequest> setupValidator,
    IValidator<LoginRequest> loginValidator,
    IClock clock,
    ILogger<AuthService> logger)
{
    private const string InvalidCredentialsMessage = "Identifiant ou mot de passe incorrect";

    public AuthStatusResponse Status() => new(credentialsStore.Get() is not null);

    public SetupResponse Setup(SetupRequest request)
    {
        if (credentialsStore.Get() is not null)
        {
            throw AlreadyInitialized();
        }

        setupValidator.ValidateOrThrow(request);

        var credentials = new Credentials
        {
            Username = request.Username!,
            PasswordHash = passwordHasher.Hash(request.Password!),
            JwtSigningKey = tokenService.GenerateSigningKey(),
            CreatedAt = clock.UtcNow,
        };

        if (!credentialsStore.TryCreate(credentials))
        {
            throw AlreadyInitialized();
        }

        logger.LogInformation("Compte d'accès créé");
        return new SetupResponse(credentials.Username);
    }

    public LoginResponse Login(LoginRequest request)
    {
        if (attemptTracker.RemainingLockout() is { } remaining)
        {
            logger.LogWarning("Connexion refusée : blocage en cours ({Seconds} s restantes)", (int)Math.Ceiling(remaining.TotalSeconds));
            throw new AccountLockedException((int)Math.Ceiling(remaining.TotalSeconds));
        }

        loginValidator.ValidateOrThrow(request);

        var credentials = credentialsStore.Get();
        if (credentials is null || !IsValid(credentials, request))
        {
            var locked = attemptTracker.RegisterFailure();
            logger.LogWarning("Échec de connexion");
            if (locked)
            {
                logger.LogWarning("Connexion bloquée pour 15 minutes après 5 échecs consécutifs");
            }

            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        attemptTracker.Reset();
        var (token, expiresAt) = tokenService.Issue(credentials);
        logger.LogInformation("Connexion réussie");
        return new LoginResponse(token, expiresAt);
    }

    private bool IsValid(Credentials credentials, LoginRequest request)
    {
        // Le hash est vérifié même si l'identifiant diffère, pour ne pas révéler lequel est faux par le temps de réponse.
        var passwordMatches = passwordHasher.Verify(request.Password!, credentials.PasswordHash);
        return string.Equals(credentials.Username, request.Username, StringComparison.Ordinal) && passwordMatches;
    }

    private static ConflictException AlreadyInitialized() => new("L'application est déjà initialisée.");
}
