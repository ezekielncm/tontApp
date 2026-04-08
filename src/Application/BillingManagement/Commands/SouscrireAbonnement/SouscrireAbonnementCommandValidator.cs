namespace Application.BillingManagement.Commands.SouscrireAbonnement;

using FluentValidation;

public sealed class SouscrireAbonnementCommandValidator : AbstractValidator<SouscrireAbonnementCommand>
{
    private static readonly string[] ValidPlanCodes = ["GRATUIT", "PRO", "IMF"];

    public SouscrireAbonnementCommandValidator()
    {
        RuleFor(x => x.GestionnaireId)
            .NotEmpty().WithMessage("L'identifiant du gestionnaire est requis.");

        RuleFor(x => x.PlanCode)
            .NotEmpty().WithMessage("Le code du plan est requis.")
            .Must(code => ValidPlanCodes.Contains(code.ToUpperInvariant()))
            .WithMessage("Le code du plan doit être GRATUIT, PRO ou IMF.");

        RuleFor(x => x.NumeroTelephone)
            .NotEmpty().WithMessage("Le numéro de téléphone est requis.")
            .When(x => !string.Equals(x.PlanCode, "GRATUIT", StringComparison.OrdinalIgnoreCase));
    }
}
