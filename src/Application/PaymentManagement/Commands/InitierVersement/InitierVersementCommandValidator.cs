namespace Application.PaymentManagement.Commands.InitierVersement;

using Domain.PaymentManagement.ValueObjects;
using FluentValidation;

public sealed class InitierVersementCommandValidator : AbstractValidator<InitierVersementCommand>
{
    public InitierVersementCommandValidator()
    {
        RuleFor(x => x.TontineId)
            .NotEmpty().WithMessage("TontineId est requis.");

        RuleFor(x => x.TourId)
            .NotEmpty().WithMessage("TourId est requis.");

        RuleFor(x => x.PayeurId)
            .NotEmpty().WithMessage("PayeurId est requis.");

        RuleFor(x => x.NumeroTelephone)
            .NotEmpty().WithMessage("Le numéro de téléphone est requis.");

        RuleFor(x => x.Montant)
            .GreaterThanOrEqualTo(Montant.MontantMinimum)
            .WithMessage($"Le montant minimum est {Montant.MontantMinimum} FCFA.");

        RuleFor(x => x.Devise)
            .NotEmpty().WithMessage("La devise est requise.");
    }
}
