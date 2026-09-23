using FinanceReport.Domain.Enums;
using FluentAssertions;

namespace FinanceReport.Domain.Tests;

public class AccountTypeTests
{
    [Theory]
    [InlineData(AccountType.Cto, AccountCategory.Titres)]
    [InlineData(AccountType.Pea, AccountCategory.Titres)]
    [InlineData(AccountType.Crypto, AccountCategory.Titres)]
    [InlineData(AccountType.Courant, AccountCategory.Especes)]
    [InlineData(AccountType.Livret, AccountCategory.Especes)]
    [InlineData(AccountType.Autre, AccountCategory.Especes)]
    public void Category_is_derived_from_type(AccountType type, AccountCategory expected)
    {
        type.GetCategory().Should().Be(expected);
    }
}
