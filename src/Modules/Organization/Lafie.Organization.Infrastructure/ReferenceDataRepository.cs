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
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<ReferenceValueDto>(
            new CommandDefinition(sql, new { category }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<bool> ReferenceValueExistsAsync(string category, string code, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.reference_values where category = @category and code = @code and active)";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
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
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<GeoDivisionDto>(
            new CommandDefinition(sql, new { level, parentCode }, cancellationToken: cancellationToken))).ToList();
    }

    // ---------------------------------------------------------------- Administration : valeurs de référence

    public async Task<IReadOnlyList<ReferenceCategoryDto>> GetReferenceCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select category as "Category", count(*)::int as "Count"
            from organization.reference_values
            group by category
            order by category
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<ReferenceCategoryDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<IReadOnlyList<ReferenceValueAdminDto>> GetReferenceValuesAdminAsync(string category, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var sql = """
            select category as "Category", code as "Code", label_fr as "LabelFr", label_en as "LabelEn",
                   display_order as "DisplayOrder", active as "Active"
            from organization.reference_values
            where category = @category
            """
            + (includeInactive ? "" : " and active")
            + (string.IsNullOrWhiteSpace(search) ? "" : " and (code ilike @like or label_fr ilike @like or label_en ilike @like)")
            + " order by display_order, label_fr";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<ReferenceValueAdminDto>(
            new CommandDefinition(sql, new { category, like = $"%{search}%" }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<bool> ReferenceValueCodeTakenAsync(string category, string code, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.reference_values where category = @category and code = @code)";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { category, code }, cancellationToken: cancellationToken));
    }

    public async Task CreateReferenceValueAsync(string category, string code, string labelFr, string labelEn, int displayOrder, bool active, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.reference_values (category, code, label_fr, label_en, display_order, active)
            values (@category, @code, @labelFr, @labelEn, @displayOrder, @active)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { category, code, labelFr, labelEn, displayOrder, active }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateReferenceValueAsync(string category, string code, string labelFr, string labelEn, int displayOrder, bool active, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.reference_values
            set label_fr = @labelFr, label_en = @labelEn, display_order = @displayOrder, active = @active
            where category = @category and code = @code
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { category, code, labelFr, labelEn, displayOrder, active }, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> SetReferenceValueActiveAsync(string category, string code, bool active, CancellationToken cancellationToken = default)
    {
        const string sql = "update organization.reference_values set active = @active where category = @category and code = @code";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { category, code, active }, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> DeleteReferenceValueAsync(string category, string code, CancellationToken cancellationToken = default)
    {
        const string sql = "delete from organization.reference_values where category = @category and code = @code";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { category, code }, cancellationToken: cancellationToken)) > 0;
    }

    // ---------------------------------------------------------------- Administration : divisions géographiques

    public async Task<bool> GeoDivisionCodeTakenAsync(string code, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.geo_divisions where code = @code)";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { code }, cancellationToken: cancellationToken));
    }

    public async Task<Guid?> GetGeoDivisionIdByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        const string sql = "select id from organization.geo_divisions where code = @code limit 1";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql, new { code }, cancellationToken: cancellationToken));
    }

    public async Task<Guid> CreateGeoDivisionAsync(string level, string code, string name, Guid? parentId, int displayOrder, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.geo_divisions (id, level, code, name, parent_id, display_order)
            values (@id, @level, @code, @name, @parentId, @displayOrder)
            """;
        var id = Guid.NewGuid();
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { id, level, code, name, parentId, displayOrder }, cancellationToken: cancellationToken));
        return id;
    }

    public async Task<bool> UpdateGeoDivisionAsync(Guid id, string name, Guid? parentId, int displayOrder, CancellationToken cancellationToken = default)
    {
        const string sql = "update organization.geo_divisions set name = @name, parent_id = @parentId, display_order = @displayOrder where id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { id, name, parentId, displayOrder }, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<int> CountGeoChildrenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "select count(*)::int from organization.geo_divisions where parent_id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteGeoDivisionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "delete from organization.geo_divisions where id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken)) > 0;
    }
}
