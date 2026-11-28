namespace Lafie.Pharmacy.Application.Stock;

/// <summary>Position de stock d'un médicament : quantité en main (somme des lots), seuil, péremption la plus proche.</summary>
public sealed class StockItemDto
{
    public Guid MedicationId { get; init; }
    public Guid FacilityId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string Form { get; init; } = string.Empty;
    public bool Controlled { get; init; }
    public int OnHand { get; init; }
    public int ReorderThreshold { get; init; }
    public int LotCount { get; init; }
    public DateOnly? EarliestExpiry { get; init; }
    public bool LowStock { get; init; }
}

/// <summary>Projection lecture d'un lot de stock.</summary>
public sealed class StockLotDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public Guid MedicationId { get; init; }
    public string? LotNumber { get; init; }
    public DateOnly? ExpiryOn { get; init; }
    public int QuantityInitial { get; init; }
    public int QuantityOnHand { get; init; }
    public decimal? UnitCost { get; init; }
    public string? SupplierName { get; init; }
    public DateOnly ReceivedOn { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

/// <summary>Projection lecture d'un mouvement de stock (grand livre).</summary>
public sealed class StockMovementDto
{
    public Guid Id { get; init; }
    public Guid MedicationId { get; init; }
    public Guid? LotId { get; init; }
    public string? LotNumber { get; init; }
    public string Type { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string? Reason { get; init; }
    public string? PerformedBy { get; init; }
    public DateTime OccurredOnUtc { get; init; }
    public string? Notes { get; init; }
}
