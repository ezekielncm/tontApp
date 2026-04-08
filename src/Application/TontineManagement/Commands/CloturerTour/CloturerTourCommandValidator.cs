namespace Application.TontineManagement.Commands.CloturerTour;

using FluentValidation;

public sealed class CloturerTourCommandValidator : AbstractValidator<CloturerTourCommand>
{
    public CloturerTourCommandValidator()
    {
        RuleFor(x => x.TontineId)
            .NotEmpty().WithMessage("L'identifiant de la tontine est requis.");

        RuleFor(x => x.RoundId)
            .NotEmpty().WithMessage("L'identifiant du tour est requis.");
    }
}
