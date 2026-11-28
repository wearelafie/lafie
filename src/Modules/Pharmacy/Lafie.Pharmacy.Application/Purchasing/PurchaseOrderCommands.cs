using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Pharmacy.Application.Catalog;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Purchasing;

/// <summary>Crée un bon de commande en brouillon avec ses lignes.</summary>
public sealed record CreatePurchaseOrderCommand(
    Guid FacilityId, Guid SupplierId, string Reference, DateOnly? OrderDate, DateOnly? ExpectedOn,
    string? Notes, IReadOnlyList<PurchaseOrderLineInput> Lines) : IRequest<Result<Guid>>;

public sealed class CreatePurchaseOrderHandler(
    IPurchaseOrderRepository orders, ISupplierRepository suppliers, IMedicationRepository medications)
    : IRequestHandler<CreatePurchaseOrderCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var supplier = await suppliers.FindAsync(command.SupplierId, cancellationToken);
        if (supplier is null || supplier.FacilityId != command.FacilityId)
            return Result.Failure<Guid>("Fournisseur introuvable dans cet établissement.");

        if (await orders.ReferenceExistsAsync(command.FacilityId, command.Reference.Trim(), cancellationToken))
            return Result.Failure<Guid>($"Un bon de commande porte déjà la référence « {command.Reference} ».");

        var header = PurchaseOrder.Create(command.FacilityId, command.SupplierId, command.Reference,
            command.OrderDate, command.ExpectedOn, command.Notes);
        if (header.IsFailure) return Result.Failure<Guid>(header.Error!);

        var linesResult = await BuildLinesAsync(header.Value.Id, command.FacilityId, command.Lines, medications, cancellationToken);
        if (linesResult.IsFailure) return Result.Failure<Guid>(linesResult.Error!);

        await orders.AddAsync(header.Value, linesResult.Value, cancellationToken);
        return Result.Success(header.Value.Id);
    }

    internal static async Task<Result<List<PurchaseOrderLine>>> BuildLinesAsync(
        Guid orderId, Guid facilityId, IReadOnlyList<PurchaseOrderLineInput> inputs,
        IMedicationRepository medications, CancellationToken ct)
    {
        var lines = new List<PurchaseOrderLine>();
        foreach (var input in inputs)
        {
            var med = await medications.GetByIdAsync(input.MedicationId, ct);
            if (med is null || med.FacilityId != facilityId)
                return Result.Failure<List<PurchaseOrderLine>>("Une ligne référence un médicament introuvable dans cet établissement.");

            var line = PurchaseOrderLine.Create(orderId, input.MedicationId, input.QuantityOrdered, input.UnitPrice);
            if (line.IsFailure) return Result.Failure<List<PurchaseOrderLine>>(line.Error!);
            lines.Add(line.Value);
        }
        return Result.Success(lines);
    }
}

/// <summary>Met à jour l'en-tête et remplace les lignes d'un bon de commande en brouillon.</summary>
public sealed record UpdatePurchaseOrderCommand(
    Guid Id, Guid SupplierId, DateOnly? OrderDate, DateOnly? ExpectedOn, string? Notes,
    IReadOnlyList<PurchaseOrderLineInput> Lines) : IRequest<Result>;

public sealed class UpdatePurchaseOrderHandler(
    IPurchaseOrderRepository orders, ISupplierRepository suppliers, IMedicationRepository medications)
    : IRequestHandler<UpdatePurchaseOrderCommand, Result>
{
    public async ValueTask<Result> Handle(UpdatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Bon de commande {command.Id} introuvable.");

        var supplier = await suppliers.FindAsync(command.SupplierId, cancellationToken);
        if (supplier is null || supplier.FacilityId != order.FacilityId)
            return Result.Failure("Fournisseur introuvable dans cet établissement.");

        var edit = order.EditHeader(command.SupplierId, command.OrderDate, command.ExpectedOn, command.Notes);
        if (edit.IsFailure) return edit;

        var linesResult = await CreatePurchaseOrderHandler.BuildLinesAsync(order.Id, order.FacilityId, command.Lines, medications, cancellationToken);
        if (linesResult.IsFailure) return Result.Failure(linesResult.Error!);

        await orders.UpdateHeaderAsync(order, cancellationToken);
        await orders.ReplaceLinesAsync(order.Id, linesResult.Value, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Passe la commande (brouillon → commandé).</summary>
public sealed record SubmitPurchaseOrderCommand(Guid Id) : IRequest<Result>;

public sealed class SubmitPurchaseOrderHandler(IPurchaseOrderRepository orders)
    : IRequestHandler<SubmitPurchaseOrderCommand, Result>
{
    public async ValueTask<Result> Handle(SubmitPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Bon de commande {command.Id} introuvable.");

        var lines = await orders.GetLineAggregatesAsync(order.Id, cancellationToken);
        if (lines.Count == 0) return Result.Failure("Impossible de passer une commande sans aucune ligne.");

        var result = order.Submit();
        if (result.IsFailure) return result;

        await orders.UpdateHeaderAsync(order, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Annule un bon de commande (tant qu'il n'est pas totalement reçu).</summary>
public sealed record CancelPurchaseOrderCommand(Guid Id) : IRequest<Result>;

public sealed class CancelPurchaseOrderHandler(IPurchaseOrderRepository orders)
    : IRequestHandler<CancelPurchaseOrderCommand, Result>
{
    public async ValueTask<Result> Handle(CancelPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Bon de commande {command.Id} introuvable.");

        var result = order.Cancel();
        if (result.IsFailure) return result;

        await orders.UpdateHeaderAsync(order, cancellationToken);
        return Result.Success();
    }
}
