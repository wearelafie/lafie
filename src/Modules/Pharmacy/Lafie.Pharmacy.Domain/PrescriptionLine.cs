using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Ligne de prescription : un médicament, une posologie, une quantité prescrite et le cumul dispensé.
/// La dispensation se fait par incréments (<see cref="Dispense"/>), bornée à la quantité prescrite.
/// </summary>
public sealed class PrescriptionLine : AggregateRoot<Guid>
{
    public const int DosageMaxLength = 300;

    public Guid PrescriptionId { get; private set; }
    public Guid MedicationId { get; private set; }
    public string? Dosage { get; private set; }
    public int QuantityPrescribed { get; private set; }
    public int QuantityDispensed { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    public int QuantityOutstanding => Math.Max(0, QuantityPrescribed - QuantityDispensed);
    public bool IsFullyDispensed => QuantityDispensed >= QuantityPrescribed;

    private PrescriptionLine(
        Guid id, Guid prescriptionId, Guid medicationId, string? dosage, int quantityPrescribed,
        int quantityDispensed, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        PrescriptionId = prescriptionId;
        MedicationId = medicationId;
        Dosage = dosage;
        QuantityPrescribed = quantityPrescribed;
        QuantityDispensed = quantityDispensed;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<PrescriptionLine> Create(Guid prescriptionId, Guid medicationId, string? dosage, int quantityPrescribed)
    {
        if (medicationId == Guid.Empty) return Result.Failure<PrescriptionLine>("Médicament obligatoire.");
        if (quantityPrescribed <= 0) return Result.Failure<PrescriptionLine>("La quantité prescrite doit être strictement positive.");

        return Result.Success(new PrescriptionLine(
            Guid.NewGuid(), prescriptionId, medicationId, Clamp(dosage, DosageMaxLength), quantityPrescribed,
            quantityDispensed: 0, DateTimeOffset.UtcNow, null));
    }

    /// <summary>Enregistre une dispensation partielle ou totale (le stock est décrémenté par l'appelant).</summary>
    public Result Dispense(int quantity)
    {
        if (quantity <= 0) return Result.Failure("La quantité à dispenser doit être strictement positive.");
        if (quantity > QuantityOutstanding)
            return Result.Failure($"Quantité à dispenser ({quantity}) supérieure au reste à servir ({QuantityOutstanding}).");
        QuantityDispensed += quantity;
        Touch();
        return Result.Success();
    }

    public static PrescriptionLine Rehydrate(
        Guid id, Guid prescriptionId, Guid medicationId, string? dosage, int quantityPrescribed,
        int quantityDispensed, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, prescriptionId, medicationId, dosage, quantityPrescribed, quantityDispensed, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Clamp(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
