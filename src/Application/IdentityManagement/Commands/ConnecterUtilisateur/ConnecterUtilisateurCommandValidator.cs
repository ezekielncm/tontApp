namespace Application.IdentityManagement.Commands.ConnecterUtilisateur;

using FluentValidation;

public sealed class ConnecterUtilisateurCommandValidator : AbstractValidator<ConnecterUtilisateurCommand>
{
    public ConnecterUtilisateurCommandValidator()
    {
        RuleFor(x => x.Telephone)
            .NotEmpty().WithMessage("Le numéro de téléphone est requis.")
            .Matches(@"^\+[1-9]\d{1,14}$").WithMessage("Le numéro de téléphone doit être au format E.164 (ex: +22670000000).");

        RuleFor(x => x.MotDePasse)
            .NotEmpty().WithMessage("Le mot de passe est requis.");
    }
}
