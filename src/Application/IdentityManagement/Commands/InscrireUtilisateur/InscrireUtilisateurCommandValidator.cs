namespace Application.IdentityManagement.Commands.InscrireUtilisateur;

using Application.IdentityManagement.Validators;
using FluentValidation;

public sealed class InscrireUtilisateurCommandValidator : AbstractValidator<InscrireUtilisateurCommand>
{
    public InscrireUtilisateurCommandValidator()
    {
        RuleFor(x => x.Telephone).MustBeE164Telephone();

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
