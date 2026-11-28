using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Ligne d'un bon de commande : un médicament, une quantité commandée et le cumul déjà reçu.
/// La réception se fait par incréments (<see cref="Receive"/>), bornée à la quantité commandée.
/// </summary>
public sealed class PurchaseOrderLine : AggregateRoot<Guid>
{
    public Guid PurchaseOrderId { get; private set; }
    public Guid MedicationId { get; private set; }
    public int QuantityOrdered { get; private set; }
    public int QuantityReceived { get; private set; }
    public decimal? UnitPrice { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    public int QuantityOutstanding => Math.Max(0, QuantityOrdered - QuantityReceived);
    public bool IsFullyReceived => QuantityReceived >= QuantityOrdered;

    private PurchaseOrderLine(
        Guid id, Guid purchaseOrderId, Guid medicationId, int quantityOrdered, int quantityReceived,
        decimal? unitPrice, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        PurchaseOrderId = purchaseOrderId;
        MedicationId = medicationId;
        QuantityOrdered = quantityOrdered;
        QuantityReceived = quantityReceived;
        UnitPrice = unitPrice;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<PurchaseOrderLine> Create(Guid purchaseOrderId, Guid medicationId, int quantityOrdered, decimal? unitPrice)
    {
        if (medicationId == Guid.Empty) return Result.Failure<PurchaseOrderLine>("Médicament obligatoire.");
        if (quantityOrdered <= 0) return Result.Failure<PurchaseOrderLine>("La quantité commandée doit être strictement positive.");
        if (unitPrice is < 0) return Result.Failure<PurchaseOrderLine>("Le prix unitaire ne peut être négatif.");

        return Result.Success(new PurchaseOrderLine(
            Guid.NewGuid(), purchaseOrderId, medicationId, quantityOrdered, quantityReceived: 0, unitPrice,
            DateTimeOffset.UtcNow, null));
    }

    /// <summary>Enregistre une réception partielle ou totale. Retourne la quantité effectivement reçue.</summary>
    public Result Receive(int quantity)
    {
        if (quantity <= 0) return Result.Failure("La quantité reçue doit être strictement positive.");
        if (quantity > QuantityOutstanding)
            return Result.Failure($"Quantité reçue ({quantity}) supérieure au reste à livrer ({QuantityOutstanding}).");
        QuantityReceived += quantity;
        Touch();
        return Result.Success();
    }

    public static PurchaseOrderLine Rehydrate(
        Guid id, Guid purchaseOrderId, Guid medicationId, int quantityOrdered, int quantityReceived,
        decimal? unitPrice, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, purchaseOrderId, medicationId, quantityOrdered, quantityReceived, unitPrice, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;
}
