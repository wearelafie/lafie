using Lafie.Core.Domain;

namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Écriture immuable du grand livre de stock : une entrée, une sortie ou un ajustement d'un lot.
/// La quantité est signée (positive en entrée, négative en sortie) — le solde d'un médicament est
/// la somme de ses mouvements.
/// </summary>
public sealed class StockMovement : AggregateRoot<Guid>
{
    public const int TextMaxLength = 1000;

    public Guid FacilityId { get; private set; }
    public Guid MedicationId { get; private set; }
    public Guid? LotId { get; private set; }
    public StockMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string? Reason { get; private set; }
    public string? PerformedBy { get; private set; }
    public DateTimeOffset OccurredOnUtc { get; private set; }
    public string? Notes { get; private set; }

    private StockMovement(
        Guid id, Guid facilityId, Guid medicationId, Guid? lotId, StockMovementType type, int quantity,
        string? reason, string? performedBy, DateTimeOffset occurredOnUtc, string? notes)
    {
        Id = id;
        FacilityId = facilityId;
        MedicationId = medicationId;
        LotId = lotId;
        Type = type;
        Quantity = quantity;
        Reason = reason;
        PerformedBy = performedBy;
        OccurredOnUtc = occurredOnUtc;
        Notes = notes;
    }

    /// <summary><paramref name="signedQuantity"/> : positif en entrée, négatif en sortie.</summary>
    public static StockMovement Create(
        Guid facilityId, Guid medicationId, Guid? lotId, StockMovementType type, int signedQuantity,
        string? reason, string? performedBy, string? notes)
        => new(Guid.NewGuid(), facilityId, medicationId, lotId, type, signedQuantity,
            Clamp(reason, TextMaxLength), Clamp(performedBy, 200), DateTimeOffset.UtcNow, Clamp(notes, TextMaxLength));

    public static StockMovement Rehydrate(
        Guid id, Guid facilityId, Guid medicationId, Guid? lotId, StockMovementType type, int quantity,
        string? reason, string? performedBy, DateTimeOffset occurredOnUtc, string? notes)
        => new(id, facilityId, medicationId, lotId, type, quantity, reason, performedBy, occurredOnUtc, notes);

    private static string? Clamp(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
