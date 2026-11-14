using Dapper;
using Lafie.Organization.Application.Equipment.Dashboard;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Lecture Dapper du tableau de bord des équipements (synthèse + alertes).</summary>
public sealed class EquipmentDashboardRepository(IDbConnectionFactory connectionFactory) : IEquipmentDashboardRepository
{
    public async Task<EquipmentDashboardDto> GetAsync(Guid facilityId, EquipmentDashboardOptions options, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var p = new
        {
            facilityId,
            warrantyWindow = options.SafeWarrantyWindow,
            maintenanceWindow = options.SafeMaintenanceWindow,
        };

        // Synthèse par statut (équipements non archivés).
        const string statusSql = """
            select
                count(*)::int as total,
                count(*) filter (where status = 'Operational')::int as operational,
                count(*) filter (where status = 'UnderMaintenance')::int as undermaintenance,
                count(*) filter (where status = 'OutOfService')::int as outofservice,
                count(*) filter (where status = 'Decommissioned')::int as decommissioned
            from organization.equipment
            where facility_id = @facilityId and archived_on_utc is null
            """;
        var counts = await connection.QuerySingleAsync<StatusCounts>(
            new CommandDefinition(statusSql, p, cancellationToken: cancellationToken));

        const string categorySql = """
            select category as "Category", count(*)::int as "Count"
            from organization.equipment
            where facility_id = @facilityId and archived_on_utc is null
            group by category
            order by count(*) desc, category
            """;
        var byCategory = (await connection.QueryAsync<EquipmentCategoryCount>(
            new CommandDefinition(categorySql, p, cancellationToken: cancellationToken))).ToList();

        // Alertes garantie (hors équipements réformés) : jours restants = warranty_end - aujourd'hui.
        const string warrantyExpiredSql = """
            select e.id as "EquipmentId", e.inventory_number as "InventoryNumber", e.name as "Name",
                   e.category as "Category", e.status as "Status", e.warranty_end as "WarrantyEnd",
                   (e.warranty_end - current_date) as "DaysRemaining"
            from organization.equipment e
            where e.facility_id = @facilityId and e.archived_on_utc is null
              and e.status <> 'Decommissioned'
              and e.warranty_end is not null and e.warranty_end < current_date
            order by e.warranty_end
            """;
        var warrantyExpired = (await connection.QueryAsync<WarrantyAlertDto>(
            new CommandDefinition(warrantyExpiredSql, p, cancellationToken: cancellationToken))).ToList();

        const string warrantyExpiringSql = """
            select e.id as "EquipmentId", e.inventory_number as "InventoryNumber", e.name as "Name",
                   e.category as "Category", e.status as "Status", e.warranty_end as "WarrantyEnd",
                   (e.warranty_end - current_date) as "DaysRemaining"
            from organization.equipment e
            where e.facility_id = @facilityId and e.archived_on_utc is null
              and e.status <> 'Decommissioned'
              and e.warranty_end is not null
              and e.warranty_end >= current_date
              and e.warranty_end <= current_date + @warrantyWindow
            order by e.warranty_end
            """;
        var warrantyExpiring = (await connection.QueryAsync<WarrantyAlertDto>(
            new CommandDefinition(warrantyExpiringSql, p, cancellationToken: cancellationToken))).ToList();

        // Alertes maintenance : interventions planifiées/en cours dont la date prévue est dépassée,
        // et interventions planifiées dans la fenêtre à venir (équipements non archivés).
        const string maintenanceOverdueSql = """
            select m.id as "MaintenanceId", m.equipment_id as "EquipmentId",
                   e.inventory_number as "InventoryNumber", e.name as "EquipmentName",
                   m.type as "Type", m.status as "Status", m.scheduled_on as "ScheduledOn",
                   (m.scheduled_on - current_date) as "DaysRemaining", m.description as "Description"
            from organization.equipment_maintenances m
            join organization.equipment e on e.id = m.equipment_id
            where e.facility_id = @facilityId and e.archived_on_utc is null
              and m.status in ('Planned', 'InProgress')
              and m.scheduled_on is not null and m.scheduled_on < current_date
            order by m.scheduled_on
            """;
        var maintenanceOverdue = (await connection.QueryAsync<MaintenanceAlertDto>(
            new CommandDefinition(maintenanceOverdueSql, p, cancellationToken: cancellationToken))).ToList();

        const string maintenanceUpcomingSql = """
            select m.id as "MaintenanceId", m.equipment_id as "EquipmentId",
                   e.inventory_number as "InventoryNumber", e.name as "EquipmentName",
                   m.type as "Type", m.status as "Status", m.scheduled_on as "ScheduledOn",
                   (m.scheduled_on - current_date) as "DaysRemaining", m.description as "Description"
            from organization.equipment_maintenances m
            join organization.equipment e on e.id = m.equipment_id
            where e.facility_id = @facilityId and e.archived_on_utc is null
              and m.status = 'Planned'
              and m.scheduled_on is not null
              and m.scheduled_on >= current_date
              and m.scheduled_on <= current_date + @maintenanceWindow
            order by m.scheduled_on
            """;
        var maintenanceUpcoming = (await connection.QueryAsync<MaintenanceAlertDto>(
            new CommandDefinition(maintenanceUpcomingSql, p, cancellationToken: cancellationToken))).ToList();

        return new EquipmentDashboardDto
        {
            Total = counts.Total,
            Operational = counts.Operational,
            UnderMaintenance = counts.UnderMaintenance,
            OutOfService = counts.OutOfService,
            Decommissioned = counts.Decommissioned,
            ByCategory = byCategory,
            WarrantyExpired = warrantyExpired,
            WarrantyExpiring = warrantyExpiring,
            MaintenanceOverdue = maintenanceOverdue,
            MaintenanceUpcoming = maintenanceUpcoming,
        };
    }

    private sealed record StatusCounts(int Total, int Operational, int UnderMaintenance, int OutOfService, int Decommissioned);
}
