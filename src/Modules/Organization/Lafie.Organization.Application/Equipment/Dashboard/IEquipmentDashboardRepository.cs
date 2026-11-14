namespace Lafie.Organization.Application.Equipment.Dashboard;

/// <summary>Fenêtres d'alerte (en jours) pour le tableau de bord des équipements.</summary>
public sealed record EquipmentDashboardOptions(int WarrantyWindowDays = 60, int MaintenanceWindowDays = 30)
{
    public const int MaxWindowDays = 365;
    public int SafeWarrantyWindow => Math.Clamp(WarrantyWindowDays, 1, MaxWindowDays);
    public int SafeMaintenanceWindow => Math.Clamp(MaintenanceWindowDays, 1, MaxWindowDays);
}

/// <summary>Port de lecture du tableau de bord des équipements (synthèse + alertes).</summary>
public interface IEquipmentDashboardRepository
{
    Task<EquipmentDashboardDto> GetAsync(Guid facilityId, EquipmentDashboardOptions options, CancellationToken cancellationToken = default);
}
