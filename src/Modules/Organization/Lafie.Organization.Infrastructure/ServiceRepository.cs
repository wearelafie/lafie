using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Services;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des services (schéma `organization`).</summary>
public sealed class ServiceRepository(IDbConnectionFactory connectionFactory) : IServiceRepository
{
    private const string Columns = """
        id as "Id", facility_id as "FacilityId", parent_service_id as "ParentServiceId",
        code as "Code", name as "Name", category as "Category", description as "Description",
        head as "Head", display_order as "DisplayOrder", status as "Status",
        created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc", archived_on_utc as "ArchivedOnUtc"
        """;

    public async Task AddAsync(Service s, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.services
                (id, facility_id, parent_service_id, code, name, category, description, head, display_order, status, created_on_utc, updated_on_utc, archived_on_utc)
            values
                (@Id, @FacilityId, @ParentServiceId, @Code, @Name, @Category, @Description, @Head, @DisplayOrder, @Status, @CreatedOnUtc, @UpdatedOnUtc, @ArchivedOnUtc)
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(s), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Service s, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.services set
                parent_service_id = @ParentServiceId, code = @Code, name = @Name, category = @Category,
                description = @Description, head = @Head, display_order = @DisplayOrder, status = @Status,
                updated_on_utc = @UpdatedOnUtc, archived_on_utc = @ArchivedOnUtc
            where id = @Id
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(s), cancellationToken: cancellationToken));
    }

    public async Task<Service?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return Service.Rehydrate(
            dto.Id, dto.FacilityId, dto.ParentServiceId, dto.Code, dto.Name,
            Enum.Parse<ServiceCategory>(dto.Category), dto.Description, dto.Head, dto.DisplayOrder,
            Enum.Parse<ServiceStatus>(dto.Status),
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null,
            dto.ArchivedOnUtc is { } a ? new DateTimeOffset(a, TimeSpan.Zero) : null);
    }

    public async Task<ServiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.services where id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ServiceDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<ServiceDto>> SearchAsync(Guid facilityId, ServiceFilter filter, CancellationToken cancellationToken = default)
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
        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            where.Append(" and category = @category");
            p.Add("category", filter.Category.Trim());
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
            $"select count(*) from organization.services {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<ServiceDto>(new CommandDefinition(
            $"select {Columns} from organization.services {where} order by display_order, name limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<ServiceDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.services where facility_id = @facilityId and lower(code) = lower(@code) and (@excludeId is null or id <> @excludeId))";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { facilityId, code, excludeId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> HasChildrenAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.services where parent_service_id = @serviceId)";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { serviceId }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from organization.services where id = @id", new { id }, cancellationToken: cancellationToken));
    }

    private static object ToParams(Service s) => new
    {
        s.Id,
        s.FacilityId,
        s.ParentServiceId,
        s.Code,
        s.Name,
        Category = s.Category.ToString(),
        s.Description,
        s.Head,
        s.DisplayOrder,
        Status = s.Status.ToString(),
        s.CreatedOnUtc,
        s.UpdatedOnUtc,
        s.ArchivedOnUtc,
    };
}
