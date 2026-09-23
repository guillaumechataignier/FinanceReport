using System.Security.Claims;
using System.Security.Cryptography;
using FinanceReport.Application.Abstractions;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FinanceReport.Infrastructure.Authentication;

/// <summary>Jetons HS512 : <c>sub</c> = identifiant, <c>exp</c> = maintenant + 8 h, <c>iss</c>/<c>aud</c> = FinanceReport.</summary>
public sealed class JwtTokenService(IClock clock) : IJwtTokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public string GenerateSigningKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public (string Token, DateTimeOffset ExpiresAt) Issue(Credentials credentials)
    {
        var now = clock.UtcNow;
        var expiresAt = now.Add(JwtSettings.Lifetime);
        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, credentials.Username)]),
            Issuer = JwtSettings.Issuer,
            Audience = JwtSettings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(JwtSettings.SigningKey(credentials.JwtSigningKey), JwtSettings.Algorithm),
        });
        return (token, expiresAt);
    }
}
