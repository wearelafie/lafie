using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Equipment.Reports;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Lectures transverses Dapper des garanties et de la maintenance (vues globales établissement).</summary>
public sealed class EquipmentReportsRepository(IDbConnectionFactory connectionFactory) : IEquipmentReportsRepository
{
    public async Task<PagedResult<EquipmentWarrantyDto>> SearchWarrantiesAsync(Guid facilityId, EquipmentWarrantyFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where e.facility_id = @facilityId and e.archived_on_utc is null");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);
        p.Add("window", filter.SafeWarrantyWindow);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (e.name ilike @search or e.inventory_number ilike @search or e.brand ilike @search or e.model ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }

        switch (filter.State)
        {
            case "Expired":
                where.Append(" and e.warranty_end is not null and e.warranty_end < current_date");
                break;
            case "Expiring":
                where.Append(" and e.warranty_end is not null and e.warranty_end >= current_date and e.warranty_end <= current_date + @window");
                break;
            case "Valid":
                where.Append(" and e.warranty_end is not null and e.warranty_end > current_date + @window");
                break;
            case "None":
                where.Append(" and e.warranty_end is null");
                break;
        }

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        const string selectColumns = """
            e.id as "EquipmentId", e.inventory_number as "InventoryNumber", e.name as "Name",
            e.category as "Category", e.status as "Status",
            sp.name as "SpaceName", svc.name as "ServiceName",
            e.warranty_end as "WarrantyEnd",
            case when e.warranty_end is null then null else (e.warranty_end - current_date) end as "DaysRemaining",
            case
                when e.warranty_end is null then 'None'
                when e.warranty_end < current_date then 'Expired'
                when e.warranty_end <= current_date + @window then 'Expiring'
                else 'Valid'
            end as "WarrantyState"
            """;
        const string fromJoins = """
            from organization.equipment e
            left join organization.spaces sp on sp.id = e.space_id
            left join organization.services svc on svc.id = e.service_id
            """;

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from organization.equipment e {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<EquipmentWarrantyDto>(new CommandDefinition(
            $"select {selectColumns} {fromJoins} {where} order by e.warranty_end asc nulls last, e.name limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<EquipmentWarrantyDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task<PagedResult<FacilityMaintenanceDto>> SearchMaintenanceAsync(Guid facilityId, FacilityMaintenanceFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where e.facility_id = @facilityId and e.archived_on_utc is null");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (e.name ilike @search or e.inventory_number ilike @search or m.description ilike @search or m.provider ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            where.Append(" and m.type = @type");
            p.Add("type", filter.Type.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and m.status = @status");
            p.Add("status", filter.Status.Trim());
        }

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        const string selectColumns = """
            m.id as "Id", m.equipment_id as "EquipmentId",
            e.inventory_number as "InventoryNumber", e.name as "EquipmentName",
            m.type as "Type", m.status as "Status", m.scheduled_on as "ScheduledOn",
            m.performed_on as "PerformedOn", m.provider as "Provider", m.cost as "Cost",
            m.description as "Description", m.created_on_utc as "CreatedOnUtc"
            """;
        const string fromJoins = """
            from organization.equipment_maintenances m
            join organization.equipment e on e.id = m.equipment_id
            """;

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) {fromJoins} {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<FacilityMaintenanceDto>(new CommandDefinition(
            $"select {selectColumns} {fromJoins} {where} order by coalesce(m.scheduled_on, m.performed_on, m.created_on_utc::date) desc, m.created_on_utc desc limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<FacilityMaintenanceDto>(items, filter.SafePage, filter.SafePageSize, total);
    }
}
