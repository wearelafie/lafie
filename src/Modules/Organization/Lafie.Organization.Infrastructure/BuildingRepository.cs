using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Buildings;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des bâtiments (schéma `organization`).</summary>
public sealed class BuildingRepository(IDbConnectionFactory connectionFactory) : IBuildingRepository
{
    private const string Columns = """
        id as "Id", facility_id as "FacilityId", code as "Code", name as "Name",
        description as "Description", location as "Location", status as "Status",
        created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc", archived_on_utc as "ArchivedOnUtc"
        """;

    public async Task AddAsync(Building b, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.buildings
                (id, facility_id, code, name, description, location, status, created_on_utc, updated_on_utc, archived_on_utc)
            values
                (@Id, @FacilityId, @Code, @Name, @Description, @Location, @Status, @CreatedOnUtc, @UpdatedOnUtc, @ArchivedOnUtc)
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(b), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Building b, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.buildings set
                code = @Code, name = @Name, description = @Description, location = @Location,
                status = @Status, updated_on_utc = @UpdatedOnUtc, archived_on_utc = @ArchivedOnUtc
            where id = @Id
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(b), cancellationToken: cancellationToken));
    }

    public async Task<Building?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return Building.Rehydrate(
            dto.Id, dto.FacilityId, dto.Code, dto.Name, dto.Description, dto.Location,
            Enum.Parse<BuildingStatus>(dto.Status),
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null,
            dto.ArchivedOnUtc is { } a ? new DateTimeOffset(a, TimeSpan.Zero) : null);
    }

    public async Task<BuildingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.buildings where id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<BuildingDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<BuildingDto>> SearchAsync(Guid facilityId, BuildingFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where facility_id = @facilityId");
        if (!filter.IncludeArchived) where.Append(" and archived_on_utc is null");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (name ilike @search or code ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
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
            $"select count(*) from organization.buildings {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<BuildingDto>(new CommandDefinition(
            $"select {Columns} from organization.buildings {where} order by code limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<BuildingDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from organization.buildings where id = @id", new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = "select exists(select 1 from organization.buildings where facility_id = @facilityId and lower(code) = lower(@code) and (@excludeId is null or id <> @excludeId))";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { facilityId, code, excludeId }, cancellationToken: cancellationToken));
    }

    private static object ToParams(Building b) => new
    {
        b.Id,
        b.FacilityId,
        b.Code,
        b.Name,
        b.Description,
        b.Location,
        Status = b.Status.ToString(),
        b.CreatedOnUtc,
        b.UpdatedOnUtc,
        b.ArchivedOnUtc,
    };
}
