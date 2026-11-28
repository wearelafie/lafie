using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Pharmacy.Application.Stock;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Dispensing;

/// <summary>
/// Dispense (tout ou partie de) une ligne de prescription : décrémente le stock par sortie FEFO
/// (réutilise le sous-domaine Stock), incrémente le dispensé de la ligne et recalcule le statut de
/// la prescription (Completed quand toutes les lignes sont servies). Échoue sans rien modifier si le
/// stock est insuffisant.
/// </summary>
public sealed record DispensePrescriptionLineCommand(
    Guid PrescriptionId, Guid LineId, int Quantity, string? PerformedBy) : IRequest<Result>;

public sealed class DispensePrescriptionLineHandler(IPrescriptionRepository prescriptions, IStockRepository stock)
    : IRequestHandler<DispensePrescriptionLineCommand, Result>
{
    public async ValueTask<Result> Handle(DispensePrescriptionLineCommand command, CancellationToken cancellationToken)
    {
        var prescription = await prescriptions.FindAsync(command.PrescriptionId, cancellationToken)
            ?? throw new NotFoundException($"Prescription {command.PrescriptionId} introuvable.");
        if (prescription.Status != PrescriptionStatus.Active)
            return Result.Failure("Seule une prescription active peut être dispensée.");

        var line = await prescriptions.FindLineAsync(command.LineId, cancellationToken);
        if (line is null || line.PrescriptionId != prescription.Id)
            return Result.Failure("Ligne de prescription introuvable pour cette prescription.");

        // Vérifie le reste à servir avant de toucher au stock.
        var dispensed = line.Dispense(command.Quantity);
        if (dispensed.IsFailure) return dispensed;

        // Sortie de stock FEFO (transactionnelle) ; échoue si stock insuffisant → ligne non persistée.
        var meta = new MovementMeta($"Dispensation {prescription.Reference ?? prescription.PatientName}", command.PerformedBy, null);
        var issue = await stock.IssueFefoAsync(prescription.FacilityId, line.MedicationId, command.Quantity,
            StockMovementType.Issue, meta, cancellationToken);
        if (issue.IsFailure) return issue;

        await prescriptions.UpdateLineAsync(line, cancellationToken);

        var lines = await prescriptions.GetLineAggregatesAsync(prescription.Id, cancellationToken);
        var allDispensed = lines.Count > 0 && lines.All(l => l.IsFullyDispensed);
        var mark = prescription.MarkDispensing(allDispensed);
        if (mark.IsSuccess) await prescriptions.UpdateHeaderAsync(prescription, cancellationToken);

        return Result.Success();
    }
}
