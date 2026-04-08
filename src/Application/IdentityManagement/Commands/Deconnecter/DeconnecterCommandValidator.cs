namespace Application.IdentityManagement.Commands.Deconnecter;

using FluentValidation;

public sealed class DeconnecterCommandValidator : AbstractValidator<DeconnecterCommand>
{
    public DeconnecterCommandValidator()
    {
        RuleFor(x => x.UtilisateurId)
            .NotEmpty().WithMessage("L'identifiant de l'utilisateur est requis.");
    }
}
