namespace Application.TontineManagement.Commands.CreateTontine;

using FluentValidation;

public sealed class CreateTontineCommandValidator : AbstractValidator<CreateTontineCommand>
{
    public CreateTontineCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom de la tontine est requis.")
            .MaximumLength(100).WithMessage("Le nom ne doit pas dépasser 100 caractères.");

        RuleFor(x => x.ContributionAmount)
            .GreaterThan(0).WithMessage("Le montant de cotisation doit être supérieur à zéro.");

        RuleFor(x => x.Periodicity)
            .NotEmpty().WithMessage("La périodicité est requise.")
            .Must(p => Enum.TryParse<Domain.TontineManagement.ValueObjects.TontinePeriodicity>(p, ignoreCase: true, out _))
            .WithMessage("La périodicité doit être Weekly, Biweekly ou Monthly.");

        RuleFor(x => x.MaxMembers)
            .GreaterThanOrEqualTo(2).WithMessage("Le nombre maximum de membres doit être au moins 2.");
    }
}
