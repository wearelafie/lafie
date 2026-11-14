using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Equipment.Maintenance;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des interventions de maintenance (schéma `organization`).</summary>
public sealed class EquipmentMaintenanceRepository(IDbConnectionFactory connectionFactory) : IEquipmentMaintenanceRepository
{
    private const string Columns = """
        id as "Id", equipment_id as "EquipmentId", type as "Type", status as "Status",
        scheduled_on as "ScheduledOn", performed_on as "PerformedOn", provider as "Provider",
        cost as "Cost", description as "Description", notes as "Notes",
        created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
        """;

    public async Task AddAsync(EquipmentMaintenance m, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.equipment_maintenances
                (id, equipment_id, type, status, scheduled_on, performed_on, provider, cost, description, notes, created_on_utc, updated_on_utc)
            values
                (@Id, @EquipmentId, @Type, @Status, @ScheduledOn, @PerformedOn, @Provider, @Cost, @Description, @Notes, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(m), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(EquipmentMaintenance m, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.equipment_maintenances set
                type = @Type, status = @Status, scheduled_on = @ScheduledOn, performed_on = @PerformedOn,
                provider = @Provider, cost = @Cost, description = @Description, notes = @Notes,
                updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(m), cancellationToken: cancellationToken));
    }

    public async Task<EquipmentMaintenance?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return EquipmentMaintenance.Rehydrate(
            dto.Id, dto.EquipmentId, Enum.Parse<MaintenanceType>(dto.Type), Enum.Parse<MaintenanceStatus>(dto.Status),
            dto.ScheduledOn, dto.PerformedOn, dto.Provider, dto.Cost, dto.Description, dto.Notes,
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<EquipmentMaintenanceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.equipment_maintenances where id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<EquipmentMaintenanceDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<EquipmentMaintenanceDto>> SearchByEquipmentAsync(Guid equipmentId, EquipmentMaintenanceFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where equipment_id = @equipmentId");
        var p = new DynamicParameters();
        p.Add("equipmentId", equipmentId);

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
            $"select count(*) from organization.equipment_maintenances {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<EquipmentMaintenanceDto>(new CommandDefinition(
            $"select {Columns} from organization.equipment_maintenances {where} order by coalesce(scheduled_on, performed_on, created_on_utc::date) desc, created_on_utc desc limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<EquipmentMaintenanceDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from organization.equipment_maintenances where id = @id", new { id }, cancellationToken: cancellationToken));
    }

    private static object ToParams(EquipmentMaintenance m) => new
    {
        m.Id,
        m.EquipmentId,
        Type = m.Type.ToString(),
        Status = m.Status.ToString(),
        m.ScheduledOn,
        m.PerformedOn,
        m.Provider,
        m.Cost,
        m.Description,
        m.Notes,
        m.CreatedOnUtc,
        m.UpdatedOnUtc,
    };
}
