using FinanceReport.Application.Dtos;
using FluentValidation;

namespace FinanceReport.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Username).NotEmpty().WithMessage("L'identifiant est obligatoire.");
        RuleFor(r => r.Password).NotEmpty().WithMessage("Le mot de passe est obligatoire.");
    }
}
