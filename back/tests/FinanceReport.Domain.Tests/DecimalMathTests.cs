using FinanceReport.Domain.Enums;
using FinanceReport.Domain.Rules;
using FluentAssertions;

namespace FinanceReport.Domain.Tests;

public class DecimalMathTests
{
    [Theory] // RG-02
    [InlineData("100", 0)]
    [InlineData("1.50", 1)]
    [InlineData("1.999", 3)]
    [InlineData("1.12345678", 8)]
    [InlineData("1.123456789", 9)]
    [InlineData("0.00012345", 8)]
    [InlineData("58000.12345678", 8)]
    [InlineData("-100.001", 3)]
    [InlineData("0", 0)]
    public void DecimalPlaces_counts_significant_decimals(string value, int expected)
    {
        DecimalMath.DecimalPlaces(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
            .Should().Be(expected);
    }

    [Theory] // TC-FUNC-25
    [InlineData("0.125", 2, "0.13")]
    [InlineData("-0.125", 2, "-0.13")]
    [InlineData("0.124", 2, "0.12")]
    [InlineData("103.53333333333333333333333333", 2, "103.53")]
    [InlineData("0.123456785", 8, "0.12345679")]
    public void Round_rounds_half_away_from_zero(string value, int decimals, string expected)
    {
        var parse = (string s) => decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

        DecimalMath.Round(parse(value), decimals).Should().Be(parse(expected));
    }

    [Fact]
    public void Round_keeps_null()
    {
        DecimalMath.Round((decimal?)null, 2).Should().BeNull();
    }

    [Theory]
    [InlineData(SecurityType.Etf, 2)]
    [InlineData(SecurityType.Action, 2)]
    [InlineData(SecurityType.Obligation, 2)]
    [InlineData(SecurityType.Crypto, 8)]
    public void Price_precision_depends_on_security_type(SecurityType type, int expected)
    {
        Precision.Price(type).Should().Be(expected);
    }
}
