using FinanceReport.Application.Dtos;
using FluentValidation;

namespace FinanceReport.Application.Validators;

/// <summary>RG-16 : identifiant de 3 à 50 caractères, mot de passe de 12 caractères minimum, confirmation identique.</summary>
public sealed class SetupRequestValidator : AbstractValidator<SetupRequest>
{
    public SetupRequestValidator()
    {
        RuleFor(r => r.Username)
            .NotEmpty().WithMessage("L'identifiant est obligatoire.")
            .Length(3, 50).WithMessage("L'identifiant doit contenir de 3 à 50 caractères.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Le mot de passe est obligatoire.")
            .MinimumLength(12).WithMessage("Le mot de passe doit contenir au moins 12 caractères.");

        RuleFor(r => r.PasswordConfirmation)
            .Equal(r => r.Password).WithMessage("La confirmation est différente du mot de passe.");
    }
}
