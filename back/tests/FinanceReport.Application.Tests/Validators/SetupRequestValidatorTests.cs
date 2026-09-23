using FinanceReport.Application.Dtos;
using FinanceReport.Application.Validators;
using FluentAssertions;

namespace FinanceReport.Application.Tests.Validators;

public class SetupRequestValidatorTests
{
    private readonly SetupRequestValidator _validator = new();

    [Theory] // RG-16
    [InlineData("gui", "123456789012", "123456789012", true)]
    [InlineData("gu", "123456789012", "123456789012", false)]
    [InlineData(null, "123456789012", "123456789012", false)]
    [InlineData("guillaume", "12345678901", "12345678901", false)]
    [InlineData("guillaume", "123456789012", "123456789013", false)]
    [InlineData("guillaume", null, null, false)]
    public void Validates_username_password_and_confirmation(string? username, string? password, string? confirmation, bool valid)
    {
        _validator.Validate(new SetupRequest(username, password, confirmation)).IsValid.Should().Be(valid);
    }

    [Fact]
    public void Username_is_limited_to_50_characters()
    {
        _validator.Validate(new SetupRequest(new string('a', 50), "123456789012", "123456789012")).IsValid.Should().BeTrue();
        _validator.Validate(new SetupRequest(new string('a', 51), "123456789012", "123456789012")).IsValid.Should().BeFalse();
    }
}
