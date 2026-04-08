namespace Application.IdentityManagement.Validators;

using FluentValidation;

public static class TelephoneValidationExtensions
{
    private const string E164Pattern = @"^\+[1-9]\d{1,14}$";
    private const string E164Message = "Le numéro de téléphone doit être au format E.164 (ex: +22670000000).";

    public static IRuleBuilderOptions<T, string> MustBeE164Telephone<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Le numéro de téléphone est requis.")
            .Matches(E164Pattern).WithMessage(E164Message);
    }
}
