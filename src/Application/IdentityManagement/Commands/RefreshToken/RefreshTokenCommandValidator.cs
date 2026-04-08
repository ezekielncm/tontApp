namespace Application.IdentityManagement.Commands.RefreshToken;

using FluentValidation;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Le refresh token est requis.");
    }
}
