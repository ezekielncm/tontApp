namespace Application.TontineManagement.Queries.GetTourActuel;

public sealed record TourActuelDto(
    Guid TourId,
    int Numero,
    string BeneficiaireNom,
    Guid BeneficiaireId,
    DateTime DateOuverture,
    DateTime? DateCloture,
    decimal MontantCollecte,
    decimal MontantAttendu,
    int NombrePayes,
    int NombreAttendus,
    double PourcentageComplete,
    IReadOnlyList<PayeurDto> Payeurs);

public sealed record PayeurDto(
    Guid MembreId,
    string Nom,
    string Statut);
