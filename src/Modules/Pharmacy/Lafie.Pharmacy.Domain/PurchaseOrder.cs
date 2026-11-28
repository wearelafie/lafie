using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Bon de commande fournisseur (en-tête). Les lignes commandées sont gérées séparément
/// (<see cref="PurchaseOrderLine"/>). La réception d'une ligne alimente le stock (lot + mouvement).
/// </summary>
public sealed class PurchaseOrder : AggregateRoot<Guid>
{
    public const int ReferenceMaxLength = 50;
    public const int NotesMaxLength = 1000;

    public Guid FacilityId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public PurchaseOrderStatus Status { get; private set; }
    public DateOnly? OrderDate { get; private set; }
    public DateOnly? ExpectedOn { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private PurchaseOrder(
        Guid id, Guid facilityId, Guid supplierId, string reference, PurchaseOrderStatus status,
        DateOnly? orderDate, DateOnly? expectedOn, string? notes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        SupplierId = supplierId;
        Reference = reference;
        Status = status;
        OrderDate = orderDate;
        ExpectedOn = expectedOn;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<PurchaseOrder> Create(
        Guid facilityId, Guid supplierId, string reference, DateOnly? orderDate, DateOnly? expectedOn, string? notes)
    {
        if (facilityId == Guid.Empty) return Result.Failure<PurchaseOrder>("Établissement obligatoire.");
        if (supplierId == Guid.Empty) return Result.Failure<PurchaseOrder>("Fournisseur obligatoire.");
        if (string.IsNullOrWhiteSpace(reference)) return Result.Failure<PurchaseOrder>("La référence du bon de commande est obligatoire.");

        return Result.Success(new PurchaseOrder(
            Guid.NewGuid(), facilityId, supplierId, reference.Trim()[..Math.Min(reference.Trim().Length, ReferenceMaxLength)],
            PurchaseOrderStatus.Draft, orderDate, expectedOn, Clamp(notes, NotesMaxLength),
            DateTimeOffset.UtcNow, null));
    }

    /// <summary>Modifie l'en-tête. Autorisé uniquement en brouillon.</summary>
    public Result EditHeader(Guid supplierId, DateOnly? orderDate, DateOnly? expectedOn, string? notes)
    {
        if (Status != PurchaseOrderStatus.Draft) return Result.Failure("Seul un bon de commande en brouillon peut être modifié.");
        if (supplierId == Guid.Empty) return Result.Failure("Fournisseur obligatoire.");
        SupplierId = supplierId;
        OrderDate = orderDate;
        ExpectedOn = expectedOn;
        Notes = Clamp(notes, NotesMaxLength);
        Touch();
        return Result.Success();
    }

    /// <summary>Passe la commande (brouillon → commandé). Le handler vérifie la présence de lignes.</summary>
    public Result Submit()
    {
        if (Status != PurchaseOrderStatus.Draft) return Result.Failure("Ce bon de commande n'est plus en brouillon.");
        Status = PurchaseOrderStatus.Ordered;
        Touch();
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
            return Result.Failure("Ce bon de commande ne peut plus être annulé.");
        Status = PurchaseOrderStatus.Cancelled;
        Touch();
        return Result.Success();
    }

    /// <summary>
    /// Met à jour le statut après réception d'une ou plusieurs lignes.
    /// <paramref name="allReceived"/> : toutes les lignes sont intégralement reçues.
    /// </summary>
    public Result MarkReception(bool allReceived)
    {
        if (Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
            return Result.Failure("Seul un bon de commande commandé peut être réceptionné.");
        Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
        Touch();
        return Result.Success();
    }

    public static PurchaseOrder Rehydrate(
        Guid id, Guid facilityId, Guid supplierId, string reference, PurchaseOrderStatus status,
        DateOnly? orderDate, DateOnly? expectedOn, string? notes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, supplierId, reference, status, orderDate, expectedOn, notes, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Clamp(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
