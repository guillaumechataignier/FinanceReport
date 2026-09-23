using FinanceReport.Application.Abstractions;

namespace FinanceReport.Infrastructure.Authentication;

/// <summary>BCrypt, facteur de coût 12, format <c>$2a$12$…</c> (TS §1.1).</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(WorkFactor, 'a'));

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
