namespace Application.TontineManagement.Commands.GenererCodeInvitation;

using FluentValidation;

public sealed class GenererCodeInvitationCommandValidator : AbstractValidator<GenererCodeInvitationCommand>
{
    public GenererCodeInvitationCommandValidator()
    {
        RuleFor(x => x.TontineId)
            .NotEmpty().WithMessage("L'identifiant de la tontine est requis.");

        RuleFor(x => x.NombreUsagesMax)
            .GreaterThanOrEqualTo(1).WithMessage("Le nombre d'usages maximum doit être au moins 1.");

        RuleFor(x => x.ExpirationJours)
            .GreaterThanOrEqualTo(1).WithMessage("La durée d'expiration doit être d'au moins 1 jour.")
            .LessThanOrEqualTo(90).WithMessage("La durée d'expiration ne peut pas dépasser 90 jours.");
    }
}
