namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Projection lecture d'un bordereau d'élimination (Dapper, par nom de colonne).</summary>
public sealed class WasteSlipDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Stream { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Carrier { get; init; }
    public string? Destination { get; init; }
    public string? Treatment { get; init; }
    public decimal TotalWeightKg { get; init; }
    public DateTime? ShippedOnUtc { get; init; }
    public DateTime? TreatedOnUtc { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public int CollectionCount { get; init; }
    /// <summary>Poids cumulé en direct des collectes rattachées (utile tant que le bordereau est au brouillon).</summary>
    public decimal LiveWeightKg { get; init; }
}
