using Dapper;
using Lafie.Organization.Application.References;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Accès Dapper aux données de référence (schéma `organization`).</summary>
public sealed class ReferenceDataRepository(IDbConnectionFactory connectionFactory) : IReferenceDataRepository
{
    public async Task<IReadOnlyList<ReferenceValueDto>> GetReferenceValuesAsync(string category, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select category as "Category", code as "Code", label_fr as "LabelFr", label_en as "LabelEn", display_order as "DisplayOrder"
            from organization.reference_values
            where category = @category and active
            order by display_order, label_fr
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<ReferenceValueDto>(
            new CommandDefinition(sql, new { category }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<bool> ReferenceValueExistsAsync(string category, string code, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.reference_values where category = @category and code = @code and active)";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { category, code }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<GeoDivisionDto>> GetGeoDivisionsAsync(string level, string? parentCode, CancellationToken cancellationToken = default)
    {
        var sql = """
            select d.id as "Id", d.level as "Level", d.code as "Code", d.name as "Name",
                   p.code as "ParentCode", d.display_order as "DisplayOrder"
            from organization.geo_divisions d
            left join organization.geo_divisions p on p.id = d.parent_id
            where d.level = @level
            """ + (parentCode is null ? "" : " and p.code = @parentCode") + " order by d.display_order, d.name";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<GeoDivisionDto>(
            new CommandDefinition(sql, new { level, parentCode }, cancellationToken: cancellationToken))).ToList();
    }
}
