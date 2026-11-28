using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Lot (batch) d'un médicament reçu en stock : numéro de lot, péremption et quantité restante.
/// La sortie FEFO (First Expired First Out) consomme en priorité les lots dont la péremption est
/// la plus proche. La consommation transactionnelle est orchestrée côté repository.
/// </summary>
public sealed class StockLot : AggregateRoot<Guid>
{
    public const int LotMaxLength = 100;
    public const int NameMaxLength = 200;
    public const int NotesMaxLength = 1000;

    public Guid FacilityId { get; private set; }
    public Guid MedicationId { get; private set; }
    public string? LotNumber { get; private set; }
    public DateOnly? ExpiryOn { get; private set; }
    public int QuantityInitial { get; private set; }
    public int QuantityOnHand { get; private set; }
    public decimal? UnitCost { get; private set; }
    public string? SupplierName { get; private set; }
    public DateOnly ReceivedOn { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private StockLot(
        Guid id, Guid facilityId, Guid medicationId, string? lotNumber, DateOnly? expiryOn,
        int quantityInitial, int quantityOnHand, decimal? unitCost, string? supplierName,
        DateOnly receivedOn, string? notes, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        MedicationId = medicationId;
        LotNumber = lotNumber;
        ExpiryOn = expiryOn;
        QuantityInitial = quantityInitial;
        QuantityOnHand = quantityOnHand;
        UnitCost = unitCost;
        SupplierName = supplierName;
        ReceivedOn = receivedOn;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<StockLot> Create(
        Guid facilityId, Guid medicationId, string? lotNumber, DateOnly? expiryOn, int quantity,
        decimal? unitCost, string? supplierName, DateOnly? receivedOn, string? notes)
    {
        if (facilityId == Guid.Empty) return Result.Failure<StockLot>("Établissement obligatoire.");
        if (medicationId == Guid.Empty) return Result.Failure<StockLot>("Médicament obligatoire.");
        if (quantity <= 0) return Result.Failure<StockLot>("La quantité reçue doit être strictement positive.");
        if (unitCost is < 0) return Result.Failure<StockLot>("Le coût unitaire ne peut être négatif.");

        return Result.Success(new StockLot(
            Guid.NewGuid(), facilityId, medicationId, Clamp(lotNumber, LotMaxLength), expiryOn,
            quantity, quantity, unitCost, Clamp(supplierName, NameMaxLength),
            receivedOn ?? DateOnly.FromDateTime(DateTime.UtcNow), Clamp(notes, NotesMaxLength),
            DateTimeOffset.UtcNow, null));
    }

    /// <summary>Corrige la quantité restante (ajustement d'inventaire). Retourne le delta appliqué.</summary>
    public Result<int> AdjustTo(int newQuantityOnHand)
    {
        if (newQuantityOnHand < 0) return Result.Failure<int>("La quantité ne peut être négative.");
        var delta = newQuantityOnHand - QuantityOnHand;
        QuantityOnHand = newQuantityOnHand;
        Touch();
        return Result.Success(delta);
    }

    public bool IsExpired(DateOnly today) => ExpiryOn is { } e && e < today;

    public static StockLot Rehydrate(
        Guid id, Guid facilityId, Guid medicationId, string? lotNumber, DateOnly? expiryOn,
        int quantityInitial, int quantityOnHand, decimal? unitCost, string? supplierName,
        DateOnly receivedOn, string? notes, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, medicationId, lotNumber, expiryOn, quantityInitial, quantityOnHand,
            unitCost, supplierName, receivedOn, notes, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Clamp(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
