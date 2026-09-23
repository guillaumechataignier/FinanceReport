using FinanceReport.Application.Dtos;
using FluentValidation;

namespace FinanceReport.Application.Validators;

/// <summary>Cours obligatoire et strictement positif ; sa précision dépend du type de support (contrôlée par le service).</summary>
public sealed class PriceRequestValidator : AbstractValidator<PriceRequest>
{
    public PriceRequestValidator()
    {
        RuleFor(r => r.Price)
            .NotNull().WithMessage("Le cours est obligatoire.")
            .GreaterThan(0m).WithMessage("Le cours doit être strictement positif.");
    }
}
