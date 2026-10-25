using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Floors;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des étages (schéma `organization`).</summary>
public sealed class FloorRepository(IDbConnectionFactory connectionFactory) : IFloorRepository
{
    private const string Columns = """
        id as "Id", building_id as "BuildingId", code as "Code", name as "Name",
        level as "Level", display_order as "DisplayOrder", description as "Description",
        status as "Status", created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc", archived_on_utc as "ArchivedOnUtc"
        """;

    public async Task AddAsync(Floor f, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.floors
                (id, building_id, code, name, level, display_order, description, status, created_on_utc, updated_on_utc, archived_on_utc)
            values
                (@Id, @BuildingId, @Code, @Name, @Level, @DisplayOrder, @Description, @Status, @CreatedOnUtc, @UpdatedOnUtc, @ArchivedOnUtc)
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(f), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Floor f, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.floors set
                code = @Code, name = @Name, level = @Level, display_order = @DisplayOrder,
                description = @Description, status = @Status, updated_on_utc = @UpdatedOnUtc, archived_on_utc = @ArchivedOnUtc
            where id = @Id
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(f), cancellationToken: cancellationToken));
    }

    public async Task<Floor?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return Floor.Rehydrate(
            dto.Id, dto.BuildingId, dto.Code, dto.Name, dto.Level, dto.DisplayOrder, dto.Description,
            Enum.Parse<FloorStatus>(dto.Status),
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null,
            dto.ArchivedOnUtc is { } a ? new DateTimeOffset(a, TimeSpan.Zero) : null);
    }

    public async Task<FloorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.floors where id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<FloorDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<FloorDto>> SearchAsync(Guid buildingId, FloorFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where building_id = @buildingId");
        if (!filter.IncludeArchived) where.Append(" and archived_on_utc is null");
        var p = new DynamicParameters();
        p.Add("buildingId", buildingId);

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
            $"select count(*) from organization.floors {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<FloorDto>(new CommandDefinition(
            $"select {Columns} from organization.floors {where} order by display_order, level nulls last, code limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<FloorDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from organization.floors where id = @id", new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(Guid buildingId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.floors where building_id = @buildingId and lower(code) = lower(@code) and (@excludeId is null or id <> @excludeId))";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { buildingId, code, excludeId }, cancellationToken: cancellationToken));
    }

    private static object ToParams(Floor f) => new
    {
        f.Id,
        f.BuildingId,
        f.Code,
        f.Name,
        f.Level,
        f.DisplayOrder,
        f.Description,
        Status = f.Status.ToString(),
        f.CreatedOnUtc,
        f.UpdatedOnUtc,
        f.ArchivedOnUtc,
    };
}
