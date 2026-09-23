using Microsoft.IdentityModel.Tokens;

namespace FinanceReport.Infrastructure.Authentication;

public static class JwtSettings
{
    public const string Issuer = "FinanceReport";
    public const string Audience = "FinanceReport";
    public const string Algorithm = SecurityAlgorithms.HmacSha512;

    /// <summary>Durée de validité d'un jeton (RG-17).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public static SymmetricSecurityKey SigningKey(string base64Key) => new(Convert.FromBase64String(base64Key));
}
