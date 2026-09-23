using FinanceReport.Application.Dtos;
using FluentValidation;

namespace FinanceReport.Application.Validators;

public sealed class SecurityRequestValidator : AbstractValidator<SecurityRequest>
{
    public SecurityRequestValidator()
    {
        RuleFor(r => r.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Le nom du support est obligatoire.")
            .MaximumLength(100).WithMessage("Le nom du support ne peut pas dépasser 100 caractères.");
        RuleFor(r => r.Code)
            .Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Le code (ISIN ou ticker) est obligatoire.")
            .MaximumLength(30).WithMessage("Le code ne peut pas dépasser 30 caractères.");
        RuleFor(r => r.Type).NotNull().WithMessage("Le type de support est obligatoire.");
        RuleFor(r => r.Zone).Must(z => !string.IsNullOrWhiteSpace(z)).WithMessage("La zone géographique est obligatoire.");
        RuleFor(r => r.Sector).Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Le secteur est obligatoire.");
    }
}
