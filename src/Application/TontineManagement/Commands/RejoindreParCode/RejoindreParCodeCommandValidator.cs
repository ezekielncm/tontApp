namespace Application.TontineManagement.Commands.RejoindreParCode;

using FluentValidation;

public sealed class RejoindreParCodeCommandValidator : AbstractValidator<RejoindreParCodeCommand>
{
    public RejoindreParCodeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Le code d'invitation est requis.")
            .Length(6).WithMessage("Le code d'invitation doit contenir exactement 6 caractères.")
            .Matches("^[A-Za-z0-9]+$").WithMessage("Le code d'invitation doit être alphanumérique.");

        RuleFor(x => x.MemberName)
            .NotEmpty().WithMessage("Le nom du membre est requis.")
            .MaximumLength(100).WithMessage("Le nom du membre ne doit pas dépasser 100 caractères.");

        RuleFor(x => x.UtilisateurId)
            .NotEmpty().WithMessage("L'identifiant de l'utilisateur est requis.");
    }
}
