namespace Application.BillingManagement.Queries.GetAbonnementByGestionnaire;

public sealed record AbonnementDto(
    Guid Id,
    string GestionnaireId,
    string Plan,
    string Statut,
    decimal MontantMensuel,
    string Currency,
    DateTime DateDebut,
    DateTime DateFin,
    DateTime CreatedAt);
