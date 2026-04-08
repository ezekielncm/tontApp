namespace Application.CreditScoringManagement.EventHandlers;

using Domain.CreditScoringManagement;
using Domain.CreditScoringManagement.Ports;
using Domain.CreditScoringManagement.Repositories;
using Domain.Common;
using Domain.PaymentManagement.Events;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles VersementConfirmedEvent to update credit scoring.
/// Creates a new ProfilCredit if one doesn't exist for the member.
/// Recalculates the score asynchronously (not in the payment transaction).
/// Traces each recalculation in the audit log.
/// </summary>
public sealed class VersementConfirmeEventHandler : INotificationHandler<VersementConfirmedEvent>
{
    private readonly IProfilCreditRepository _profilCreditRepository;
    private readonly IScoringEngine _scoringEngine;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VersementConfirmeEventHandler> _logger;

    public VersementConfirmeEventHandler(
        IProfilCreditRepository profilCreditRepository,
        IScoringEngine scoringEngine,
        IUnitOfWork unitOfWork,
        ILogger<VersementConfirmeEventHandler> logger)
    {
        _profilCreditRepository = profilCreditRepository;
        _scoringEngine = scoringEngine;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(VersementConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var membreId = notification.PayeurId.Value;

        _logger.LogInformation(
            "Recalcul du score crédit pour le membre {MembreId} suite au versement {VersementId}",
            membreId,
            notification.VersementId.Value);

        var profil = await _profilCreditRepository.GetByMembreIdAsync(membreId, cancellationToken);

        if (profil is null)
        {
            profil = ProfilCredit.Create(membreId);
            profil.EnregistrerVersementConfirme(estPonctuel: true, _scoringEngine);
            await _profilCreditRepository.AddAsync(profil, cancellationToken);
        }
        else
        {
            profil.EnregistrerVersementConfirme(estPonctuel: true, _scoringEngine);
            await _profilCreditRepository.UpdateAsync(profil, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Score crédit recalculé pour le membre {MembreId}: score={Score}, niveau={Niveau}",
            membreId,
            profil.ScoreActuel.Valeur,
            profil.ScoreActuel.Niveau);
    }
}
