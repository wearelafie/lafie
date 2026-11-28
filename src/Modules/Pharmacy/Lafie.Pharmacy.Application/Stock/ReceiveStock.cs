using Lafie.Core.Common;
using Lafie.Pharmacy.Application.Catalog;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Stock;

/// <summary>Réceptionne un lot d'un médicament en stock (entrée) et journalise le mouvement.</summary>
public sealed record ReceiveStockCommand(
    Guid FacilityId, Guid MedicationId, string? LotNumber, DateOnly? ExpiryOn, int Quantity,
    decimal? UnitCost, string? SupplierName, DateOnly? ReceivedOn, string? Notes, string? PerformedBy)
    : IRequest<Result<Guid>>;

public sealed class ReceiveStockHandler(IStockRepository stock, IMedicationRepository medications)
    : IRequestHandler<ReceiveStockCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(ReceiveStockCommand command, CancellationToken cancellationToken)
    {
        var medication = await medications.GetByIdAsync(command.MedicationId, cancellationToken);
        if (medication is null || medication.FacilityId != command.FacilityId)
            return Result.Failure<Guid>("Médicament introuvable dans cet établissement.");

        var created = StockLot.Create(
            command.FacilityId, command.MedicationId, command.LotNumber, command.ExpiryOn, command.Quantity,
            command.UnitCost, command.SupplierName, command.ReceivedOn, command.Notes);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        var lot = created.Value;
        await stock.AddLotAsync(lot, cancellationToken);
        await stock.AddMovementAsync(StockMovement.Create(
            command.FacilityId, command.MedicationId, lot.Id, StockMovementType.Receipt, lot.QuantityInitial,
            reason: null, command.PerformedBy, command.Notes), cancellationToken);

        return Result.Success(lot.Id);
    }
}
