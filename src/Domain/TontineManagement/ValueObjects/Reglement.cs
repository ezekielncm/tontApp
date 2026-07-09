namespace Domain.TontineManagement.ValueObjects;

using Domain.Common;

/// <summary>
/// Immutable value object representing the rules of a tontine.
/// Once the tontine is activated, the Reglement cannot be modified.
/// </summary>
public sealed class Reglement : ValueObject
{
    public required ContributionAmount ContributionAmount { get; init; }
    public required TontinePeriodicity Periodicity { get; init; }
    public required int MaxMembers { get; init; }
    public required ModeAttribution ModeAttribution { get; init; }
    public required int MinMembresActivation { get; init; }

    // Required by EF Core for owned type binding
    private Reglement() { ContributionAmount = null!; }

    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    private Reglement(
        ContributionAmount contributionAmount,
        TontinePeriodicity periodicity,
        int maxMembers,
        ModeAttribution modeAttribution,
        int minMembresActivation)
    {
        ContributionAmount = contributionAmount;
        Periodicity = periodicity;
        MaxMembers = maxMembers;
        ModeAttribution = modeAttribution;
        MinMembresActivation = minMembresActivation;
    }

    public static Reglement Create(
        ContributionAmount contributionAmount,
        TontinePeriodicity periodicity,
        int maxMembers,
        ModeAttribution modeAttribution = ModeAttribution.Sequentiel,
        int minMembresActivation = 3)
    {
        if (contributionAmount is null)
            throw new ArgumentNullException(nameof(contributionAmount));

        if (maxMembers < 2)
            throw new ArgumentException("A tontine must allow at least 2 members.", nameof(maxMembers));

        if (minMembresActivation < 2)
            throw new ArgumentException("Minimum members for activation must be at least 2.", nameof(minMembresActivation));

        if (minMembresActivation > maxMembers)
            throw new ArgumentException("Minimum members for activation cannot exceed max members.", nameof(minMembresActivation));

        return new Reglement(contributionAmount, periodicity, maxMembers, modeAttribution, minMembresActivation);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ContributionAmount;
        yield return Periodicity;
        yield return MaxMembers;
        yield return ModeAttribution;
        yield return MinMembresActivation;
    }

    public override string ToString() =>
        $"Reglement: {ContributionAmount}, {Periodicity}, MaxMembers={MaxMembers}, Mode={ModeAttribution}, MinActivation={MinMembresActivation}";
}
