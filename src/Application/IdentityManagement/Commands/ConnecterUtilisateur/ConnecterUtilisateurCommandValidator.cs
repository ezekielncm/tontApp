namespace Application.IdentityManagement.Commands.ConnecterUtilisateur;

using Application.IdentityManagement.Validators;
using FluentValidation;

public sealed class ConnecterUtilisateurCommandValidator : AbstractValidator<ConnecterUtilisateurCommand>
{
    public ConnecterUtilisateurCommandValidator()
    {
        RuleFor(x => x.Telephone).MustBeE164Telephone();

        RuleFor(x => x.MotDePasse)
            .NotEmpty().WithMessage("Le mot de passe est requis.");
    }
}
