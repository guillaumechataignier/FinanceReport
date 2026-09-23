using FinanceReport.Application.Dtos;
using FluentValidation;

namespace FinanceReport.Application.Validators;

public sealed class AccountRequestValidator : AbstractValidator<AccountRequest>
{
    public AccountRequestValidator()
    {
        RuleFor(r => r.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Le nom du compte est obligatoire.")
            .MaximumLength(100).WithMessage("Le nom du compte ne peut pas dépasser 100 caractères.");
        RuleFor(r => r.Type).NotNull().WithMessage("Le type de compte est obligatoire.");
        RuleFor(r => r.InstitutionId)
            .Must(id => id is { } value && value != Guid.Empty).WithMessage("L'établissement est obligatoire.");
    }
}
