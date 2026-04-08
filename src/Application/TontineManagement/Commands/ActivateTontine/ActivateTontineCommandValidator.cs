namespace Application.TontineManagement.Commands.ActivateTontine;

using FluentValidation;

public sealed class ActivateTontineCommandValidator : AbstractValidator<ActivateTontineCommand>
{
    public ActivateTontineCommandValidator()
    {
        RuleFor(x => x.TontineId)
            .NotEmpty().WithMessage("L'identifiant de la tontine est requis.");
    }
}
