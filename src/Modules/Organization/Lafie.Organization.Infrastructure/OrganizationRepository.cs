using Dapper;
using Lafie.Organization.Application;
using Lafie.Organization.Domain;
using Lafie.Persistence;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper du module Organization (SQL explicite, schéma `organization`).</summary>
public sealed class OrganizationRepository(IDbConnectionFactory connectionFactory) : IOrganizationRepository
{
    private const string SelectColumns = """
        id as "Id", name as "Name", type as "Type", code as "Code",
        active as "Active", parent_id as "ParentId", created_on_utc as "CreatedOnUtc"
        """;

    public async Task AddAsync(DomainOrganization organization, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.organizations
                (id, name, type, code, active, parent_id, created_on_utc)
            values
                (@Id, @Name, @Type, @Code, @Active, @ParentId, @CreatedOnUtc)
            """;

        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            organization.Id,
            organization.Name,
            Type = organization.Type.ToString(),
            organization.Code,
            organization.Active,
            organization.ParentId,
            organization.CreatedOnUtc,
        }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<OrganizationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"select {SelectColumns} from organization.organizations order by name";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<OrganizationDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {SelectColumns} from organization.organizations where id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<OrganizationDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }
}
