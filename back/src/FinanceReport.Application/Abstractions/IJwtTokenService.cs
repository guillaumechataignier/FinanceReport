using FinanceReport.Domain.Entities;

namespace FinanceReport.Application.Abstractions;

public interface IJwtTokenService
{
    /// <summary>Génère une clé de signature aléatoire (64 octets, base64).</summary>
    string GenerateSigningKey();

    (string Token, DateTimeOffset ExpiresAt) Issue(Credentials credentials);
}
