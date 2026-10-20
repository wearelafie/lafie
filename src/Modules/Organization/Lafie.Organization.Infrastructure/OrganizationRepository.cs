using System.Data;
using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application;
using Lafie.Organization.Domain;
using Lafie.Persistence;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper du module Organization (SQL explicite, schéma `organization`).</summary>
public sealed class OrganizationRepository(IDbConnectionFactory connectionFactory) : IOrganizationRepository
{
    private const string Columns = """
        id as "Id", name as "Name", type as "Type", code as "Code", status as "Status",
        short_name as "ShortName", description as "Description", phone as "Phone", email as "Email",
        timezone as "Timezone", city as "City", country as "Country", parent_id as "ParentId",
        created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
        """;

    public async Task AddAsync(DomainOrganization organization, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.organizations
                (id, name, type, code, status, short_name, description, phone, email, timezone, city, country, parent_id, created_on_utc, updated_on_utc)
            values
                (@Id, @Name, @Type, @Code, @Status, @ShortName, @Description, @Phone, @Email, @Timezone, @City, @Country, @ParentId, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(organization), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(DomainOrganization organization, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.organizations set
                name = @Name, type = @Type, code = @Code, status = @Status,
                short_name = @ShortName, description = @Description, phone = @Phone, email = @Email,
                timezone = @Timezone, city = @City, country = @Country, parent_id = @ParentId,
                updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(organization), cancellationToken: cancellationToken));
    }

    public async Task<DomainOrganization?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;

        var profile = new OrganizationProfile(
            dto.ShortName, dto.Description, dto.Phone, dto.Email, dto.Timezone, dto.City, dto.Country);

        return DomainOrganization.Rehydrate(
            dto.Id,
            dto.Name,
            Enum.Parse<OrganizationType>(dto.Type),
            dto.Code,
            Enum.Parse<OrganizationStatus>(dto.Status),
            profile,
            dto.ParentId,
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.organizations where id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<OrganizationDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<OrganizationDto>> SearchAsync(OrganizationFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where 1 = 1");
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (name ilike @search or code ilike @search or short_name ilike @search)");
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
            $"select count(*) from organization.organizations {where}", p, cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<OrganizationDto>(new CommandDefinition(
            $"select {Columns} from organization.organizations {where} order by name limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<OrganizationDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    private static object ToParams(DomainOrganization o) => new
    {
        o.Id,
        o.Name,
        Type = o.Type.ToString(),
        o.Code,
        Status = o.Status.ToString(),
        o.ShortName,
        o.Description,
        o.Phone,
        o.Email,
        o.Timezone,
        o.City,
        o.Country,
        o.ParentId,
        o.CreatedOnUtc,
        o.UpdatedOnUtc,
    };
}
