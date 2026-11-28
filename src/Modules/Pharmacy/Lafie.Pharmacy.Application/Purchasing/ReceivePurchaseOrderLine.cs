using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Pharmacy.Application.Stock;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Purchasing;

/// <summary>
/// Réceptionne (tout ou partie de) une ligne de commande : crée un lot de stock et son mouvement
/// d'entrée (réutilise le sous-domaine Stock), incrémente le reçu de la ligne et recalcule le statut
/// du bon de commande (PartiallyReceived / Received).
/// </summary>
public sealed record ReceivePurchaseOrderLineCommand(
    Guid OrderId, Guid LineId, int Quantity, string? LotNumber, DateOnly? ExpiryOn, DateOnly? ReceivedOn, string? PerformedBy)
    : IRequest<Result>;

public sealed class ReceivePurchaseOrderLineHandler(
    IPurchaseOrderRepository orders, ISupplierRepository suppliers, IStockRepository stock)
    : IRequestHandler<ReceivePurchaseOrderLineCommand, Result>
{
    public async ValueTask<Result> Handle(ReceivePurchaseOrderLineCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException($"Bon de commande {command.OrderId} introuvable.");
        if (order.Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
            return Result.Failure("Seul un bon de commande commandé peut être réceptionné.");

        var line = await orders.FindLineAsync(command.LineId, cancellationToken);
        if (line is null || line.PurchaseOrderId != order.Id)
            return Result.Failure("Ligne de commande introuvable pour ce bon de commande.");

        // Incrémente le reçu (borné au reste à livrer).
        var received = line.Receive(command.Quantity);
        if (received.IsFailure) return received;

        var supplier = await suppliers.FindAsync(order.SupplierId, cancellationToken);

        // Entrée en stock : un lot + un mouvement de réception.
        var lot = StockLot.Create(order.FacilityId, line.MedicationId, command.LotNumber, command.ExpiryOn,
            command.Quantity, line.UnitPrice, supplier?.Name, command.ReceivedOn, notes: null);
        if (lot.IsFailure) return Result.Failure(lot.Error!);

        await stock.AddLotAsync(lot.Value, cancellationToken);
        await stock.AddMovementAsync(StockMovement.Create(
            order.FacilityId, line.MedicationId, lot.Value.Id, StockMovementType.Receipt, lot.Value.QuantityInitial,
            reason: $"Réception BC {order.Reference}", command.PerformedBy, notes: null), cancellationToken);

        await orders.UpdateLineAsync(line, cancellationToken);

        // Recalcule le statut du bon de commande d'après l'état de toutes ses lignes.
        var lines = await orders.GetLineAggregatesAsync(order.Id, cancellationToken);
        var allReceived = lines.Count > 0 && lines.All(l => l.IsFullyReceived);
        var mark = order.MarkReception(allReceived);
        if (mark.IsSuccess) await orders.UpdateHeaderAsync(order, cancellationToken);

        return Result.Success();
    }
}
