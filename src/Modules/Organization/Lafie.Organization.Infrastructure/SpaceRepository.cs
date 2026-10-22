using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des espaces (schéma `organization`).</summary>
public sealed class SpaceRepository(IDbConnectionFactory connectionFactory) : ISpaceRepository
{
    private const string Columns = """
        id as "Id", facility_id as "FacilityId", building_id as "BuildingId", floor_id as "FloorId",
        code as "Code", name as "Name", type as "Type", description as "Description",
        area as "Area", max_capacity as "MaxCapacity", accessible as "Accessible",
        status as "Status", created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc", archived_on_utc as "ArchivedOnUtc"
        """;

    public async Task AddAsync(Space s, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.spaces
                (id, facility_id, building_id, floor_id, code, name, type, description, area, max_capacity, accessible, status, created_on_utc, updated_on_utc, archived_on_utc)
            values
                (@Id, @FacilityId, @BuildingId, @FloorId, @Code, @Name, @Type, @Description, @Area, @MaxCapacity, @Accessible, @Status, @CreatedOnUtc, @UpdatedOnUtc, @ArchivedOnUtc)
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(s), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Space s, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.spaces set
                code = @Code, name = @Name, type = @Type, description = @Description,
                area = @Area, max_capacity = @MaxCapacity, accessible = @Accessible,
                status = @Status, updated_on_utc = @UpdatedOnUtc, archived_on_utc = @ArchivedOnUtc
            where id = @Id
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(s), cancellationToken: cancellationToken));
    }

    public async Task<Space?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return Space.Rehydrate(
            dto.Id, dto.FacilityId, dto.BuildingId, dto.FloorId, dto.Code, dto.Name,
            Enum.Parse<SpaceType>(dto.Type), dto.Description, dto.Area, dto.MaxCapacity, dto.Accessible,
            Enum.Parse<SpaceStatus>(dto.Status),
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null,
            dto.ArchivedOnUtc is { } a ? new DateTimeOffset(a, TimeSpan.Zero) : null);
    }

    public async Task<SpaceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.spaces where id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<SpaceDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<SpaceDto>> SearchByFloorAsync(Guid floorId, SpaceFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where floor_id = @floorId");
        if (!filter.IncludeArchived) where.Append(" and archived_on_utc is null");
        var p = new DynamicParameters();
        p.Add("floorId", floorId);

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
            $"select count(*) from organization.spaces {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<SpaceDto>(new CommandDefinition(
            $"select {Columns} from organization.spaces {where} order by code limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<SpaceDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from organization.spaces where id = @id", new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.spaces where facility_id = @facilityId and lower(code) = lower(@code) and (@excludeId is null or id <> @excludeId))";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { facilityId, code, excludeId }, cancellationToken: cancellationToken));
    }

    private static object ToParams(Space s) => new
    {
        s.Id,
        s.FacilityId,
        s.BuildingId,
        s.FloorId,
        s.Code,
        s.Name,
        Type = s.Type.ToString(),
        s.Description,
        s.Area,
        s.MaxCapacity,
        s.Accessible,
        Status = s.Status.ToString(),
        s.CreatedOnUtc,
        s.UpdatedOnUtc,
        s.ArchivedOnUtc,
    };
}
