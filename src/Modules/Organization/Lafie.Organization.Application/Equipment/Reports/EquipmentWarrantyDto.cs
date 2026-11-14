namespace Lafie.Organization.Application.Equipment.Reports;

/// <summary>Projection lecture d'une ligne « garantie » (vue globale établissement).</summary>
public sealed class EquipmentWarrantyDto
{
    public Guid EquipmentId { get; init; }
    public string InventoryNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? SpaceName { get; init; }
    public string? ServiceName { get; init; }
    public DateOnly? WarrantyEnd { get; init; }
    public int? DaysRemaining { get; init; }
    /// <summary>Expired | Expiring | Valid | None.</summary>
    public string WarrantyState { get; init; } = "None";
}

/// <summary>Critères de la vue globale des garanties (au sein d'un établissement).</summary>
public sealed record EquipmentWarrantyFilter(string? Search = null, string? State = null, int WarrantyWindowDays = 60, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int SafeWarrantyWindow => Math.Clamp(WarrantyWindowDays, 1, 365);
    public int Offset => (SafePage - 1) * SafePageSize;
}
