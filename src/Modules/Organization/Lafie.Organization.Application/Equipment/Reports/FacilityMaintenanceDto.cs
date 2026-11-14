namespace Lafie.Organization.Application.Equipment.Reports;

/// <summary>Projection lecture d'une intervention de maintenance (vue globale établissement, avec l'équipement).</summary>
public sealed class FacilityMaintenanceDto
{
    public Guid Id { get; init; }
    public Guid EquipmentId { get; init; }
    public string InventoryNumber { get; init; } = string.Empty;
    public string EquipmentName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateOnly? ScheduledOn { get; init; }
    public DateOnly? PerformedOn { get; init; }
    public string? Provider { get; init; }
    public decimal? Cost { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedOnUtc { get; init; }
}

/// <summary>Critères de la vue globale de maintenance (au sein d'un établissement).</summary>
public sealed record FacilityMaintenanceFilter(string? Search = null, string? Type = null, string? Status = null, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}
