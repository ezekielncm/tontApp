namespace Application.TontineManagement.Commands.AddMember;

using FluentValidation;

public sealed class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.TontineId)
            .NotEmpty().WithMessage("L'identifiant de la tontine est requis.");

        RuleFor(x => x.MemberName)
            .NotEmpty().WithMessage("Le nom du membre est requis.")
            .MaximumLength(100).WithMessage("Le nom du membre ne doit pas dépasser 100 caractères.");
    }
}
