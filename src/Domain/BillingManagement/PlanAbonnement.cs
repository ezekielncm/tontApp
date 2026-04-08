namespace Domain.BillingManagement;

using Domain.BillingManagement.ValueObjects;
using Domain.Common;

/// <summary>
/// Represents a subscription plan with its limits and pricing.
/// Seed data: Gratuit, Pro, IMF.
/// </summary>
public sealed class PlanAbonnement : Entity<PlanAbonnementId>
{
    public string Nom { get; private set; }
    public string Code { get; private set; }
    public decimal PrixMensuel { get; private set; }
    public string Devise { get; private set; }
    public int MaxTontines { get; private set; }
    public int MaxMembresParTontine { get; private set; }
    public string? Description { get; private set; }
    public bool EstActif { get; private set; }

    private PlanAbonnement() : base()
    {
        Nom = string.Empty;
        Code = string.Empty;
        Devise = "XOF";
    }

    private PlanAbonnement(
        PlanAbonnementId id,
        string nom,
        string code,
        decimal prixMensuel,
        string devise,
        int maxTontines,
        int maxMembresParTontine,
        string? description) : base(id)
    {
        Nom = nom;
        Code = code;
        PrixMensuel = prixMensuel;
        Devise = devise;
        MaxTontines = maxTontines;
        MaxMembresParTontine = maxMembresParTontine;
        Description = description;
        EstActif = true;
    }

    public static PlanAbonnement Create(
        string nom,
        string code,
        decimal prixMensuel,
        string devise,
        int maxTontines,
        int maxMembresParTontine,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Plan name cannot be empty.", nameof(nom));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Plan code cannot be empty.", nameof(code));

        if (prixMensuel < 0)
            throw new ArgumentException("Monthly price cannot be negative.", nameof(prixMensuel));

        if (maxTontines < 0)
            throw new ArgumentException("Max tontines cannot be negative.", nameof(maxTontines));

        if (maxMembresParTontine < 0)
            throw new ArgumentException("Max members per tontine cannot be negative.", nameof(maxMembresParTontine));

        return new PlanAbonnement(
            PlanAbonnementId.Create(),
            nom,
            code,
            prixMensuel,
            devise,
            maxTontines,
            maxMembresParTontine,
            description);
    }

    /// <summary>
    /// Creates a plan with a pre-defined ID (for seed data).
    /// </summary>
    public static PlanAbonnement CreateWithId(
        Guid id,
        string nom,
        string code,
        decimal prixMensuel,
        string devise,
        int maxTontines,
        int maxMembresParTontine,
        string? description = null)
    {
        return new PlanAbonnement(
            PlanAbonnementId.From(id),
            nom,
            code,
            prixMensuel,
            devise,
            maxTontines,
            maxMembresParTontine,
            description);
    }

    public void Desactiver() => EstActif = false;
    public void Activer() => EstActif = true;

    /// <summary>
    /// Well-known plan codes for comparison in business rules.
    /// </summary>
    public static class Codes
    {
        public const string Gratuit = "GRATUIT";
        public const string Pro = "PRO";
        public const string Imf = "IMF";
    }

    /// <summary>
    /// Well-known seed plan IDs (stable GUIDs for FK referencing).
    /// </summary>
    public static class SeedIds
    {
        public static readonly Guid Gratuit = new("00000000-0000-0000-0000-000000000001");
        public static readonly Guid Pro = new("00000000-0000-0000-0000-000000000002");
        public static readonly Guid Imf = new("00000000-0000-0000-0000-000000000003");
    }
}
