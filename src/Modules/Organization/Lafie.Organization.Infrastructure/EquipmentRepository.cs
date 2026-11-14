using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Equipment;
using Lafie.Organization.Domain;
using Lafie.Persistence;
using DomainEquipment = Lafie.Organization.Domain.Equipment;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des équipements (schéma `organization`).</summary>
public sealed class EquipmentRepository(IDbConnectionFactory connectionFactory) : IEquipmentRepository
{
    // Projection lecture (avec jointures pour les libellés de localisation/affectation).
    private const string SelectColumns = """
        e.id as "Id", e.facility_id as "FacilityId", e.inventory_number as "InventoryNumber",
        e.serial_number as "SerialNumber", e.name as "Name", e.category as "Category",
        e.brand as "Brand", e.model as "Model", e.acquisition_date as "AcquisitionDate",
        e.supplier as "Supplier", e.warranty_end as "WarrantyEnd",
        e.estimated_lifespan_months as "EstimatedLifespanMonths",
        e.space_id as "SpaceId", sp.name as "SpaceName",
        e.service_id as "ServiceId", svc.name as "ServiceName",
        e.care_unit_id as "CareUnitId", cu.name as "CareUnitName",
        e.status as "Status", e.notes as "Notes", e.display_order as "DisplayOrder",
        e.created_on_utc as "CreatedOnUtc", e.updated_on_utc as "UpdatedOnUtc", e.archived_on_utc as "ArchivedOnUtc"
        """;

    private const string FromJoins = """
        from organization.equipment e
        left join organization.spaces sp on sp.id = e.space_id
        left join organization.services svc on svc.id = e.service_id
        left join organization.care_units cu on cu.id = e.care_unit_id
        """;

    public async Task AddAsync(DomainEquipment e, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.equipment
                (id, facility_id, inventory_number, serial_number, name, category, brand, model,
                 acquisition_date, supplier, warranty_end, estimated_lifespan_months,
                 space_id, service_id, care_unit_id, status, notes, display_order,
                 created_on_utc, updated_on_utc, archived_on_utc)
            values
                (@Id, @FacilityId, @InventoryNumber, @SerialNumber, @Name, @Category, @Brand, @Model,
                 @AcquisitionDate, @Supplier, @WarrantyEnd, @EstimatedLifespanMonths,
                 @SpaceId, @ServiceId, @CareUnitId, @Status, @Notes, @DisplayOrder,
                 @CreatedOnUtc, @UpdatedOnUtc, @ArchivedOnUtc)
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(e), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(DomainEquipment e, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.equipment set
                inventory_number = @InventoryNumber, serial_number = @SerialNumber, name = @Name,
                category = @Category, brand = @Brand, model = @Model, acquisition_date = @AcquisitionDate,
                supplier = @Supplier, warranty_end = @WarrantyEnd, estimated_lifespan_months = @EstimatedLifespanMonths,
                space_id = @SpaceId, service_id = @ServiceId, care_unit_id = @CareUnitId,
                status = @Status, notes = @Notes, display_order = @DisplayOrder,
                updated_on_utc = @UpdatedOnUtc, archived_on_utc = @ArchivedOnUtc
            where id = @Id
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(e), cancellationToken: cancellationToken));
    }

    public async Task<DomainEquipment?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return DomainEquipment.Rehydrate(
            dto.Id, dto.FacilityId, dto.InventoryNumber, dto.SerialNumber, dto.Name, dto.Category,
            dto.Brand, dto.Model, dto.AcquisitionDate, dto.Supplier, dto.WarrantyEnd, dto.EstimatedLifespanMonths,
            dto.SpaceId, dto.ServiceId, dto.CareUnitId, Enum.Parse<EquipmentStatus>(dto.Status), dto.Notes, dto.DisplayOrder,
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null,
            dto.ArchivedOnUtc is { } a ? new DateTimeOffset(a, TimeSpan.Zero) : null);
    }

    public async Task<EquipmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {SelectColumns} {FromJoins} where e.id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<EquipmentDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<EquipmentDto>> SearchAsync(Guid facilityId, EquipmentFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where e.facility_id = @facilityId");
        if (!filter.IncludeArchived) where.Append(" and e.archived_on_utc is null");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (e.name ilike @search or e.inventory_number ilike @search or e.serial_number ilike @search or e.brand ilike @search or e.model ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            where.Append(" and e.category = @category");
            p.Add("category", filter.Category.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and e.status = @status");
            p.Add("status", filter.Status.Trim());
        }
        if (filter.SpaceId is { } spaceId && spaceId != Guid.Empty)
        {
            where.Append(" and e.space_id = @spaceId");
            p.Add("spaceId", spaceId);
        }
        if (filter.ServiceId is { } serviceId && serviceId != Guid.Empty)
        {
            where.Append(" and e.service_id = @serviceId");
            p.Add("serviceId", serviceId);
        }
        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from organization.equipment e {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<EquipmentDto>(new CommandDefinition(
            $"select {SelectColumns} {FromJoins} {where} order by e.display_order, e.name limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<EquipmentDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task<bool> InventoryNumberExistsAsync(Guid facilityId, string inventoryNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.equipment where facility_id = @facilityId and lower(inventory_number) = lower(@inventoryNumber) and (@excludeId is null or id <> @excludeId))";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { facilityId, inventoryNumber, excludeId }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from organization.equipment where id = @id", new { id }, cancellationToken: cancellationToken));
    }

    private static object ToParams(DomainEquipment e) => new
    {
        e.Id,
        e.FacilityId,
        e.InventoryNumber,
        e.SerialNumber,
        e.Name,
        e.Category,
        e.Brand,
        e.Model,
        e.AcquisitionDate,
        e.Supplier,
        e.WarrantyEnd,
        e.EstimatedLifespanMonths,
        e.SpaceId,
        e.ServiceId,
        e.CareUnitId,
        Status = e.Status.ToString(),
        e.Notes,
        e.DisplayOrder,
        e.CreatedOnUtc,
        e.UpdatedOnUtc,
        e.ArchivedOnUtc,
    };
}
