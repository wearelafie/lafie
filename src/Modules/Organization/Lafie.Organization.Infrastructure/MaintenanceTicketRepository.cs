using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Maintenance.Tickets;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des tickets de maintenance (GMAO, schéma `organization`).</summary>
public sealed class MaintenanceTicketRepository(IDbConnectionFactory connectionFactory) : IMaintenanceTicketRepository
{
    // Projection lecture : la cible (équipement ou espace) est enrichie par jointures conditionnelles.
    private const string SelectColumns = """
        t.id as "Id", t.facility_id as "FacilityId", t.target_type as "TargetType", t.target_id as "TargetId",
        coalesce(eq.name, sp.name) as "TargetName",
        coalesce(eq.inventory_number, sp.code) as "TargetCode",
        t.title as "Title", t.description as "Description", t.criticality as "Criticality", t.blocking as "Blocking",
        t.status as "Status", t.assignee_type as "AssigneeType", t.assignee_ref as "AssigneeRef",
        t.reported_by as "ReportedBy", t.resolution as "Resolution",
        t.downtime_start as "DowntimeStart", t.downtime_end as "DowntimeEnd",
        t.created_on_utc as "CreatedOnUtc", t.updated_on_utc as "UpdatedOnUtc"
        """;

    private const string FromJoins = """
        from organization.maintenance_tickets t
        left join organization.equipment eq on t.target_type = 'Equipment' and eq.id = t.target_id
        left join organization.spaces sp on t.target_type = 'Space' and sp.id = t.target_id
        """;

    public async Task AddAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.maintenance_tickets
                (id, facility_id, target_type, target_id, title, description, criticality, blocking, status,
                 assignee_type, assignee_ref, reported_by, resolution, downtime_start, downtime_end, created_on_utc, updated_on_utc)
            values
                (@Id, @FacilityId, @TargetType, @TargetId, @Title, @Description, @Criticality, @Blocking, @Status,
                 @AssigneeType, @AssigneeRef, @ReportedBy, @Resolution, @DowntimeStart, @DowntimeEnd, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(ticket), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.maintenance_tickets set
                title = @Title, description = @Description, criticality = @Criticality, blocking = @Blocking,
                status = @Status, assignee_type = @AssigneeType, assignee_ref = @AssigneeRef,
                resolution = @Resolution, downtime_start = @DowntimeStart, downtime_end = @DowntimeEnd,
                updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(ticket), cancellationToken: cancellationToken));
    }

    public async Task<MaintenanceTicket?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", facility_id as "FacilityId", target_type as "TargetType", target_id as "TargetId",
                   title as "Title", description as "Description", criticality as "Criticality", blocking as "Blocking",
                   status as "Status", assignee_type as "AssigneeType", assignee_ref as "AssigneeRef",
                   reported_by as "ReportedBy", resolution as "Resolution",
                   downtime_start as "DowntimeStart", downtime_end as "DowntimeEnd",
                   created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
            from organization.maintenance_tickets where id = @id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var r = await connection.QuerySingleOrDefaultAsync<TicketRow>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
        if (r is null) return null;

        return MaintenanceTicket.Rehydrate(
            r.Id, r.FacilityId, Enum.Parse<TicketTargetType>(r.TargetType), r.TargetId, r.Title, r.Description,
            Enum.Parse<TicketCriticality>(r.Criticality), r.Blocking, Enum.Parse<TicketStatus>(r.Status),
            string.IsNullOrWhiteSpace(r.AssigneeType) ? null : Enum.Parse<TicketAssigneeType>(r.AssigneeType), r.AssigneeRef,
            r.ReportedBy, r.Resolution,
            r.DowntimeStart is { } ds ? new DateTimeOffset(ds, TimeSpan.Zero) : null,
            r.DowntimeEnd is { } de ? new DateTimeOffset(de, TimeSpan.Zero) : null,
            new DateTimeOffset(r.CreatedOnUtc, TimeSpan.Zero),
            r.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<MaintenanceTicketDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {SelectColumns} {FromJoins} where t.id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<MaintenanceTicketDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<MaintenanceTicketDto>> SearchByFacilityAsync(Guid facilityId, MaintenanceTicketFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where t.facility_id = @facilityId");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (t.title ilike @search or t.description ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and t.status = @status");
            p.Add("status", filter.Status.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.Criticality))
        {
            where.Append(" and t.criticality = @criticality");
            p.Add("criticality", filter.Criticality.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.TargetType))
        {
            where.Append(" and t.target_type = @targetType");
            p.Add("targetType", filter.TargetType.Trim());
        }
        if (filter.TargetId is { } tid)
        {
            where.Append(" and t.target_id = @targetId");
            p.Add("targetId", tid);
        }
        if (filter.OpenOnly)
            where.Append(" and t.status not in ('Resolved', 'Closed')");

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from organization.maintenance_tickets t {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<MaintenanceTicketDto>(new CommandDefinition(
            $"""
             select {SelectColumns} {FromJoins} {where}
             order by
                 case t.criticality when 'Critical' then 0 when 'High' then 1 when 'Medium' then 2 else 3 end,
                 t.created_on_utc desc
             limit @limit offset @offset
             """,
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<MaintenanceTicketDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task<int> CountActiveBlockingForSpaceAsync(Guid spaceId, Guid? excludeTicketId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select count(*)::int from organization.maintenance_tickets
            where target_type = 'Space' and target_id = @spaceId and blocking = true
              and status not in ('Resolved', 'Closed')
              and (@excludeTicketId is null or id <> @excludeTicketId)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql, new { spaceId, excludeTicketId }, cancellationToken: cancellationToken));
    }

    public async Task AddPartAsync(MaintenanceTicketPart part, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.maintenance_ticket_parts
                (id, ticket_id, label, reference, quantity, unit_cost, created_on_utc)
            values
                (@Id, @TicketId, @Label, @Reference, @Quantity, @UnitCost, @CreatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            part.Id,
            part.TicketId,
            part.Label,
            part.Reference,
            part.Quantity,
            part.UnitCost,
            part.CreatedOnUtc,
        }, cancellationToken: cancellationToken));
    }

    public async Task DeletePartAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from organization.maintenance_ticket_parts where id = @partId",
            new { partId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<MaintenanceTicketPartDto>> GetPartsAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", ticket_id as "TicketId", label as "Label", reference as "Reference",
                   quantity as "Quantity", unit_cost as "UnitCost", created_on_utc as "CreatedOnUtc"
            from organization.maintenance_ticket_parts
            where ticket_id = @ticketId
            order by created_on_utc
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<MaintenanceTicketPartDto>(
            new CommandDefinition(sql, new { ticketId }, cancellationToken: cancellationToken))).ToList();
    }

    private static object ToParams(MaintenanceTicket t) => new
    {
        t.Id,
        t.FacilityId,
        TargetType = t.TargetType.ToString(),
        t.TargetId,
        t.Title,
        t.Description,
        Criticality = t.Criticality.ToString(),
        t.Blocking,
        Status = t.Status.ToString(),
        AssigneeType = t.AssigneeType?.ToString(),
        t.AssigneeRef,
        t.ReportedBy,
        t.Resolution,
        t.DowntimeStart,
        t.DowntimeEnd,
        t.CreatedOnUtc,
        t.UpdatedOnUtc,
    };

    /// <summary>Ligne brute pour réhydrater l'agrégat (colonnes sans jointure).</summary>
    private sealed class TicketRow
    {
        public Guid Id { get; init; }
        public Guid FacilityId { get; init; }
        public string TargetType { get; init; } = string.Empty;
        public Guid TargetId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string Criticality { get; init; } = string.Empty;
        public bool Blocking { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? AssigneeType { get; init; }
        public string? AssigneeRef { get; init; }
        public string? ReportedBy { get; init; }
        public string? Resolution { get; init; }
        public DateTime? DowntimeStart { get; init; }
        public DateTime? DowntimeEnd { get; init; }
        public DateTime CreatedOnUtc { get; init; }
        public DateTime? UpdatedOnUtc { get; init; }
    }
}
