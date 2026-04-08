namespace Domain.NotificationManagement.ValueObjects;

/// <summary>
/// French SMS templates for each notification type.
/// All templates are &lt; 160 characters to avoid concatenated SMS (cost x2).
/// </summary>
public static class SmsTemplate
{
    /// <summary>Tour ouvert → notifier le bénéficiaire</summary>
    public static string TourOuvert(string nomTontine, int numeroTour)
        => Truncate($"TontinesApp: Tour {numeroTour} de {nomTontine} est ouvert. Vous etes le beneficiaire. Bonne reception!");

    /// <summary>Versement confirmé → accusé de réception</summary>
    public static string VersementConfirme(decimal montant, string devise, string nomTontine)
        => Truncate($"TontinesApp: Paiement de {montant} {devise} recu pour {nomTontine}. Merci!");

    /// <summary>Rappel J-3 avant échéance</summary>
    public static string RappelJ3(string nomTontine, decimal montant, string devise)
        => Truncate($"TontinesApp: Rappel - votre cotisation de {montant} {devise} pour {nomTontine} est due dans 3 jours.");

    /// <summary>Rappel J-1 avant échéance</summary>
    public static string RappelJ1(string nomTontine, decimal montant, string devise)
        => Truncate($"TontinesApp: Rappel - cotisation de {montant} {devise} pour {nomTontine} due demain. Merci de payer.");

    /// <summary>Paiement en retard</summary>
    public static string PaiementEnRetard(string nomTontine, decimal montant, string devise)
        => Truncate($"TontinesApp: Cotisation de {montant} {devise} pour {nomTontine} en retard. Veuillez payer au plus vite.");

    /// <summary>Récapitulatif hebdomadaire</summary>
    public static string RecapHebdomadaire(string nomTontine, int membresAJour, int totalMembres)
        => Truncate($"TontinesApp: Recap {nomTontine} - {membresAJour}/{totalMembres} membres a jour cette semaine.");

    /// <summary>Bienvenue</summary>
    public static string Bienvenue(string nomTontine)
        => Truncate($"TontinesApp: Bienvenue dans la tontine {nomTontine}! Bonne epargne avec votre groupe.");

    /// <summary>Confirmation d'abonnement</summary>
    public static string ConfirmationAbonnement(string plan, decimal montant, string devise)
        => Truncate($"TontinesApp: Abonnement {plan} active! Montant: {montant} {devise}/mois. Merci de votre confiance.");

    /// <summary>Rappel de renouvellement J-3</summary>
    public static string RappelRenouvellement(string plan, decimal montant, string devise, DateTime dateFin)
        => Truncate($"TontinesApp: Votre abonnement {plan} expire le {dateFin:dd/MM}. Renouvellement: {montant} {devise}.");

    /// <summary>Renouvellement réussi</summary>
    public static string RenouvellementReussi(string plan, DateTime nouvelleDateFin)
        => Truncate($"TontinesApp: Abonnement {plan} renouvele jusqu'au {nouvelleDateFin:dd/MM/yyyy}. Merci!");

    /// <summary>Échec de renouvellement</summary>
    public static string RenouvellementEchoue(string plan)
        => Truncate($"TontinesApp: Echec du renouvellement {plan}. Verifiez votre solde. Grace de 3 jours accordee.");

    private static string Truncate(string message)
        => message.Length > ContenuMessage.MaxLength
            ? message[..ContenuMessage.MaxLength]
            : message;
}
