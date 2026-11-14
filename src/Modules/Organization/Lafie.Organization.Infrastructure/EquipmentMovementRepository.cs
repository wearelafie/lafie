using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Equipment.Movements;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des déplacements d'équipement (schéma `organization`).</summary>
public sealed class EquipmentMovementRepository(IDbConnectionFactory connectionFactory) : IEquipmentMovementRepository
{
    private const string SelectColumns = """
        m.id as "Id", m.equipment_id as "EquipmentId", m.moved_on as "MovedOn",
        m.from_space_id as "FromSpaceId", fs.name as "FromSpaceName",
        m.to_space_id as "ToSpaceId", ts.name as "ToSpaceName",
        m.reason as "Reason", m.performed_by as "PerformedBy", m.notes as "Notes",
        m.created_on_utc as "CreatedOnUtc"
        """;

    private const string FromJoins = """
        from organization.equipment_movements m
        left join organization.spaces fs on fs.id = m.from_space_id
        left join organization.spaces ts on ts.id = m.to_space_id
        """;

    public async Task AddAsync(EquipmentMovement m, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.equipment_movements
                (id, equipment_id, moved_on, from_space_id, to_space_id, reason, performed_by, notes, created_on_utc)
            values
                (@Id, @EquipmentId, @MovedOn, @FromSpaceId, @ToSpaceId, @Reason, @PerformedBy, @Notes, @CreatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            m.Id, m.EquipmentId, m.MovedOn, m.FromSpaceId, m.ToSpaceId, m.Reason, m.PerformedBy, m.Notes, m.CreatedOnUtc,
        }, cancellationToken: cancellationToken));
    }

    public async Task<EquipmentMovementDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {SelectColumns} {FromJoins} where m.id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<EquipmentMovementDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<EquipmentMovementDto>> SearchByEquipmentAsync(Guid equipmentId, EquipmentMovementFilter filter, CancellationToken cancellationToken = default)
    {
        var p = new DynamicParameters();
        p.Add("equipmentId", equipmentId);
        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "select count(*) from organization.equipment_movements where equipment_id = @equipmentId", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<EquipmentMovementDto>(new CommandDefinition(
            $"select {SelectColumns} {FromJoins} where m.equipment_id = @equipmentId order by m.moved_on desc, m.created_on_utc desc limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<EquipmentMovementDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from organization.equipment_movements where id = @id", new { id }, cancellationToken: cancellationToken));
    }
}
