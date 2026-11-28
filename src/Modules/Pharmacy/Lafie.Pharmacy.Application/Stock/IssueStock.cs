using Lafie.Core.Common;
using Lafie.Pharmacy.Application.Catalog;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Stock;

/// <summary>Sortie de stock d'un médicament (consommation FEFO), hors dispensation patient.</summary>
public sealed record IssueStockCommand(
    Guid FacilityId, Guid MedicationId, int Quantity, string? Reason, string? PerformedBy, string? Notes)
    : IRequest<Result>;

public sealed class IssueStockHandler(IStockRepository stock, IMedicationRepository medications)
    : IRequestHandler<IssueStockCommand, Result>
{
    public async ValueTask<Result> Handle(IssueStockCommand command, CancellationToken cancellationToken)
    {
        if (command.Quantity <= 0) return Result.Failure("La quantité à sortir doit être strictement positive.");

        var medication = await medications.GetByIdAsync(command.MedicationId, cancellationToken);
        if (medication is null || medication.FacilityId != command.FacilityId)
            return Result.Failure("Médicament introuvable dans cet établissement.");

        return await stock.IssueFefoAsync(
            command.FacilityId, command.MedicationId, command.Quantity, StockMovementType.Issue,
            new MovementMeta(command.Reason, command.PerformedBy, command.Notes), cancellationToken);
    }
}
