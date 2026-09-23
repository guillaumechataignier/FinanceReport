namespace FinanceReport.Domain.Entities;

public sealed record Credentials
{
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }

    /// <summary>Clé de signature JWT (base64, 64 octets), propre à l'installation.</summary>
    public required string JwtSigningKey { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
