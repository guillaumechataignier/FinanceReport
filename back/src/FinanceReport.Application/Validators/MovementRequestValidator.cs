using FinanceReport.Application.Dtos;
using FinanceReport.Domain.Enums;
using FinanceReport.Domain.Rules;
using FluentValidation;

namespace FinanceReport.Application.Validators;

/// <summary>
/// Champs obligatoires et interdits selon le type (FS §3.4, RG-02). La précision du prix unitaire, qui dépend du type
/// de support, est contrôlée par le service.
/// </summary>
public sealed class MovementRequestValidator : AbstractValidator<MovementRequest>
{
    private const string Forbidden = "Ce champ est interdit pour ce type de mouvement.";

    public MovementRequestValidator()
    {
        RuleFor(r => r.Type).NotNull().WithMessage("Le type de mouvement est obligatoire.");
        RuleFor(r => r.Date).NotNull().WithMessage("La date est obligatoire.");
        RuleFor(r => r.AccountId).Must(id => id is { } v && v != Guid.Empty).WithMessage("Le compte est obligatoire.");

        When(r => r.Type is { } t && t.IsTrade(), () =>
        {
            RuleFor(r => r.SecurityId).Must(id => id is { } v && v != Guid.Empty).WithMessage("Le support est obligatoire.");
            RuleFor(r => r.Quantity)
                .NotNull().WithMessage("La quantité est obligatoire.")
                .GreaterThan(0m).WithMessage("La quantité doit être strictement positive.")
                .MaxDecimals(Precision.Quantity, "La quantité");
            RuleFor(r => r.UnitPrice)
                .NotNull().WithMessage("Le prix unitaire est obligatoire.")
                .GreaterThan(0m).WithMessage("Le prix unitaire doit être strictement positif.");
            RuleFor(r => r.Fees)
                .GreaterThanOrEqualTo(0m).WithMessage("Les frais ne peuvent pas être négatifs.")
                .MaxDecimals(Precision.Amount, "Les frais");
            RuleFor(r => r.Amount).Null().WithMessage(Forbidden);
        });

        When(r => r.Type is { } t && !t.IsTrade(), () =>
        {
            RuleFor(r => r.Amount)
                .NotNull().WithMessage("Le montant est obligatoire.")
                .GreaterThan(0m).WithMessage("Le montant doit être strictement positif.")
                .MaxDecimals(Precision.Amount, "Le montant");
            RuleFor(r => r.SecurityId).Null().WithMessage(Forbidden);
            RuleFor(r => r.Quantity).Null().WithMessage(Forbidden);
            RuleFor(r => r.UnitPrice).Null().WithMessage(Forbidden);
            RuleFor(r => r.Fees).Null().WithMessage(Forbidden);
        });
    }
}
