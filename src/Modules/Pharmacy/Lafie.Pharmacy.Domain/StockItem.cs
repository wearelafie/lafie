using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Fiche de stock d'un médicament dans un établissement : porte le seuil de réapprovisionnement.
/// La quantité en stock n'est pas stockée ici — elle est calculée par somme des lots actifs.
/// </summary>
public sealed class StockItem : AggregateRoot<Guid>
{
    public const int NotesMaxLength = 1000;

    public Guid FacilityId { get; private set; }
    public Guid MedicationId { get; private set; }
    public int ReorderThreshold { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private StockItem(
        Guid id, Guid facilityId, Guid medicationId, int reorderThreshold, string? notes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        MedicationId = medicationId;
        ReorderThreshold = reorderThreshold;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<StockItem> Create(Guid facilityId, Guid medicationId, int reorderThreshold, string? notes)
    {
        if (facilityId == Guid.Empty) return Result.Failure<StockItem>("Établissement obligatoire.");
        if (medicationId == Guid.Empty) return Result.Failure<StockItem>("Médicament obligatoire.");
        if (reorderThreshold < 0) return Result.Failure<StockItem>("Le seuil de réapprovisionnement ne peut être négatif.");

        return Result.Success(new StockItem(
            Guid.NewGuid(), facilityId, medicationId, reorderThreshold, Clamp(notes, NotesMaxLength),
            DateTimeOffset.UtcNow, null));
    }

    public Result SetReorderThreshold(int threshold, string? notes)
    {
        if (threshold < 0) return Result.Failure("Le seuil de réapprovisionnement ne peut être négatif.");
        ReorderThreshold = threshold;
        Notes = Clamp(notes, NotesMaxLength);
        UpdatedOnUtc = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public static StockItem Rehydrate(
        Guid id, Guid facilityId, Guid medicationId, int reorderThreshold, string? notes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, medicationId, reorderThreshold, notes, createdOnUtc, updatedOnUtc);

    private static string? Clamp(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
