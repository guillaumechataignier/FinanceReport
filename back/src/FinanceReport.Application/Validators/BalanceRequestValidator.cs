using FinanceReport.Application.Dtos;
using FinanceReport.Domain.Rules;
using FluentValidation;

namespace FinanceReport.Application.Validators;

/// <summary>Montant obligatoire, 2 décimales au plus (RG-02) ; un solde négatif est autorisé (FS §3.2).</summary>
public sealed class BalanceRequestValidator : AbstractValidator<BalanceRequest>
{
    public BalanceRequestValidator()
    {
        RuleFor(r => r.Amount)
            .NotNull().WithMessage("Le montant est obligatoire.")
            .MaxDecimals(Precision.Amount, "Le montant");
    }
}
