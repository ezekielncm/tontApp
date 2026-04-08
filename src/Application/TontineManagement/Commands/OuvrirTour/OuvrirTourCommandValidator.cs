namespace Application.TontineManagement.Commands.OuvrirTour;

using FluentValidation;

public sealed class OuvrirTourCommandValidator : AbstractValidator<OuvrirTourCommand>
{
    public OuvrirTourCommandValidator()
    {
        RuleFor(x => x.TontineId)
            .NotEmpty().WithMessage("L'identifiant de la tontine est requis.");
    }
}
