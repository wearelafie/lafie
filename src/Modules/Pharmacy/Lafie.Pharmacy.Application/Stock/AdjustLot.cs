using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Stock;

/// <summary>Corrige la quantité restante d'un lot (ajustement d'inventaire) et journalise le delta.</summary>
public sealed record AdjustLotCommand(Guid LotId, int NewQuantityOnHand, string? Reason, string? PerformedBy)
    : IRequest<Result>;

public sealed class AdjustLotHandler(IStockRepository stock)
    : IRequestHandler<AdjustLotCommand, Result>
{
    public async ValueTask<Result> Handle(AdjustLotCommand command, CancellationToken cancellationToken)
    {
        var lot = await stock.FindLotAsync(command.LotId, cancellationToken)
            ?? throw new NotFoundException($"Lot {command.LotId} introuvable.");

        var adjusted = lot.AdjustTo(command.NewQuantityOnHand);
        if (adjusted.IsFailure) return Result.Failure(adjusted.Error!);
        if (adjusted.Value == 0) return Result.Success(); // aucun changement

        await stock.UpdateLotAsync(lot, cancellationToken);
        await stock.AddMovementAsync(StockMovement.Create(
            lot.FacilityId, lot.MedicationId, lot.Id, StockMovementType.Adjustment, adjusted.Value,
            command.Reason, command.PerformedBy, notes: null), cancellationToken);

        return Result.Success();
    }
}
