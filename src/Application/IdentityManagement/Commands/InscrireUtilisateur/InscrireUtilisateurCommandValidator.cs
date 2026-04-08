namespace Application.IdentityManagement.Commands.InscrireUtilisateur;

using FluentValidation;

public sealed class InscrireUtilisateurCommandValidator : AbstractValidator<InscrireUtilisateurCommand>
{
    public InscrireUtilisateurCommandValidator()
    {
        RuleFor(x => x.Telephone)
            .NotEmpty().WithMessage("Le numéro de téléphone est requis.")
            .Matches(@"^\+[1-9]\d{1,14}$").WithMessage("Le numéro de téléphone doit être au format E.164 (ex: +22670000000).");

        RuleFor(x => x.Nom)
            .NotEmpty().WithMessage("Le nom est requis.")
            .MaximumLength(100).WithMessage("Le nom ne doit pas dépasser 100 caractères.");

        RuleFor(x => x.MotDePasse)
            .NotEmpty().WithMessage("Le mot de passe est requis.")
            .MinimumLength(8).WithMessage("Le mot de passe doit contenir au moins 8 caractères.")
            .Matches(@"[A-Z]").WithMessage("Le mot de passe doit contenir au moins une majuscule.")
            .Matches(@"[a-z]").WithMessage("Le mot de passe doit contenir au moins une minuscule.")
            .Matches(@"\d").WithMessage("Le mot de passe doit contenir au moins un chiffre.");
    }
}
