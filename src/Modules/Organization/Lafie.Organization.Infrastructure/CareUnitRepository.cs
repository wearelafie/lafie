using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.CareUnits;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des unités de soins + de leur association aux espaces (schéma `organization`).</summary>
public sealed class CareUnitRepository(IDbConnectionFactory connectionFactory) : ICareUnitRepository
{
    private const string Columns = """
        id as "Id", facility_id as "FacilityId", service_id as "ServiceId", code as "Code", name as "Name",
        type as "Type", bed_capacity as "BedCapacity", description as "Description", display_order as "DisplayOrder",
        status as "Status", created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc", archived_on_utc as "ArchivedOnUtc"
        """;

    private const string SpaceColumns = """
        sp.id as "Id", sp.facility_id as "FacilityId", sp.building_id as "BuildingId", sp.floor_id as "FloorId",
        sp.code as "Code", sp.name as "Name", sp.type as "Type", sp.description as "Description",
        sp.area as "Area", sp.max_capacity as "MaxCapacity", sp.accessible as "Accessible", sp.status as "Status",
        sp.created_on_utc as "CreatedOnUtc", sp.updated_on_utc as "UpdatedOnUtc", sp.archived_on_utc as "ArchivedOnUtc"
        """;

    public async Task AddAsync(CareUnit u, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.care_units
                (id, facility_id, service_id, code, name, type, bed_capacity, description, display_order, status, created_on_utc, updated_on_utc, archived_on_utc)
            values
                (@Id, @FacilityId, @ServiceId, @Code, @Name, @Type, @BedCapacity, @Description, @DisplayOrder, @Status, @CreatedOnUtc, @UpdatedOnUtc, @ArchivedOnUtc)
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(u), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(CareUnit u, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.care_units set
                code = @Code, name = @Name, type = @Type, bed_capacity = @BedCapacity, description = @Description,
                display_order = @DisplayOrder, status = @Status, updated_on_utc = @UpdatedOnUtc, archived_on_utc = @ArchivedOnUtc
            where id = @Id
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(u), cancellationToken: cancellationToken));
    }

    public async Task<CareUnit?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return CareUnit.Rehydrate(
            dto.Id, dto.FacilityId, dto.ServiceId, dto.Code, dto.Name,
            Enum.Parse<CareUnitType>(dto.Type), dto.BedCapacity, dto.Description, dto.DisplayOrder,
            Enum.Parse<CareUnitStatus>(dto.Status),
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } up ? new DateTimeOffset(up, TimeSpan.Zero) : null,
            dto.ArchivedOnUtc is { } a ? new DateTimeOffset(a, TimeSpan.Zero) : null);
    }

    public async Task<CareUnitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.care_units where id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CareUnitDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<CareUnitDto>> SearchByServiceAsync(Guid serviceId, CareUnitFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where service_id = @serviceId");
        if (!filter.IncludeArchived) where.Append(" and archived_on_utc is null");
        var p = new DynamicParameters();
        p.Add("serviceId", serviceId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (name ilike @search or code ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            where.Append(" and type = @type");
            p.Add("type", filter.Type.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and status = @status");
            p.Add("status", filter.Status.Trim());
        }
        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from organization.care_units {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<CareUnitDto>(new CommandDefinition(
            $"select {Columns} from organization.care_units {where} order by display_order, name limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<CareUnitDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.care_units where facility_id = @facilityId and lower(code) = lower(@code) and (@excludeId is null or id <> @excludeId))";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { facilityId, code, excludeId }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from organization.care_units where id = @id", new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> SpaceLinkExistsAsync(Guid careUnitId, Guid spaceId, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.care_unit_spaces where care_unit_id = @careUnitId and space_id = @spaceId)";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { careUnitId, spaceId }, cancellationToken: cancellationToken));
    }

    public async Task AddSpaceAsync(Guid careUnitId, Guid spaceId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.care_unit_spaces (care_unit_id, space_id, created_on_utc)
            values (@careUnitId, @spaceId, @now)
            on conflict (care_unit_id, space_id) do nothing
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { careUnitId, spaceId, now = DateTimeOffset.UtcNow }, cancellationToken: cancellationToken));
    }

    public async Task RemoveSpaceAsync(Guid careUnitId, Guid spaceId, CancellationToken cancellationToken = default)
    {
        const string sql = "delete from organization.care_unit_spaces where care_unit_id = @careUnitId and space_id = @spaceId";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { careUnitId, spaceId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<SpaceDto>> GetSpacesAsync(Guid careUnitId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            select {SpaceColumns}
            from organization.care_unit_spaces cus
            join organization.spaces sp on sp.id = cus.space_id
            where cus.care_unit_id = @careUnitId
            order by sp.code
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<SpaceDto>(
            new CommandDefinition(sql, new { careUnitId }, cancellationToken: cancellationToken))).ToList();
    }

    private static object ToParams(CareUnit u) => new
    {
        u.Id,
        u.FacilityId,
        u.ServiceId,
        u.Code,
        u.Name,
        Type = u.Type.ToString(),
        u.BedCapacity,
        u.Description,
        u.DisplayOrder,
        Status = u.Status.ToString(),
        u.CreatedOnUtc,
        u.UpdatedOnUtc,
        u.ArchivedOnUtc,
    };
}
