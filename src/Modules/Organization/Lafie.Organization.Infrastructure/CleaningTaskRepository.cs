using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Hygiene.Cleaning;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des tâches de bio-nettoyage (hygiène, schéma `organization`).</summary>
public sealed class CleaningTaskRepository(IDbConnectionFactory connectionFactory) : ICleaningTaskRepository
{
    private const string SelectColumns = """
        t.id as "Id", t.facility_id as "FacilityId", t.space_id as "SpaceId",
        sp.name as "SpaceName", sp.code as "SpaceCode",
        t.protocol as "Protocol", t."trigger" as "Trigger", t.status as "Status",
        t.requested_by as "RequestedBy", t.assigned_to as "AssignedTo", t.notes as "Notes",
        t.started_on_utc as "StartedOnUtc", t.completed_on_utc as "CompletedOnUtc",
        t.created_on_utc as "CreatedOnUtc", t.updated_on_utc as "UpdatedOnUtc",
        (select count(*) from organization.cleaning_task_items i where i.task_id = t.id)::int as "ItemsTotal",
        (select count(*) from organization.cleaning_task_items i where i.task_id = t.id and i.done)::int as "ItemsDone"
        """;

    private const string FromJoins = """
        from organization.cleaning_tasks t
        left join organization.spaces sp on sp.id = t.space_id
        """;

    public async Task AddAsync(CleaningTask task, IReadOnlyList<CleaningTaskItem> items, CancellationToken cancellationToken = default)
    {
        const string taskSql = """
            insert into organization.cleaning_tasks
                (id, facility_id, space_id, protocol, "trigger", status, requested_by, assigned_to, notes,
                 started_on_utc, completed_on_utc, created_on_utc, updated_on_utc)
            values
                (@Id, @FacilityId, @SpaceId, @Protocol, @Trigger, @Status, @RequestedBy, @AssignedTo, @Notes,
                 @StartedOnUtc, @CompletedOnUtc, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        const string itemSql = """
            insert into organization.cleaning_task_items
                (id, task_id, label, done, done_on_utc, sort_order, created_on_utc)
            values
                (@Id, @TaskId, @Label, @Done, @DoneOnUtc, @SortOrder, @CreatedOnUtc)
            """;

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition(taskSql, ToParams(task), transaction, cancellationToken: cancellationToken));
        foreach (var item in items)
            await connection.ExecuteAsync(new CommandDefinition(itemSql, ItemParams(item), transaction, cancellationToken: cancellationToken));
        transaction.Commit();
    }

    public async Task UpdateAsync(CleaningTask task, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.cleaning_tasks set
                protocol = @Protocol, "trigger" = @Trigger, status = @Status,
                requested_by = @RequestedBy, assigned_to = @AssignedTo, notes = @Notes,
                started_on_utc = @StartedOnUtc, completed_on_utc = @CompletedOnUtc, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(task), cancellationToken: cancellationToken));
    }

    public async Task<CleaningTask?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", facility_id as "FacilityId", space_id as "SpaceId", protocol as "Protocol",
                   "trigger" as "Trigger", status as "Status", requested_by as "RequestedBy", assigned_to as "AssignedTo",
                   notes as "Notes", started_on_utc as "StartedOnUtc", completed_on_utc as "CompletedOnUtc",
                   created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
            from organization.cleaning_tasks where id = @id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var r = await connection.QuerySingleOrDefaultAsync<TaskRow>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
        if (r is null) return null;

        return CleaningTask.Rehydrate(
            r.Id, r.FacilityId, r.SpaceId, Enum.Parse<CleaningProtocol>(r.Protocol), Enum.Parse<CleaningTrigger>(r.Trigger),
            Enum.Parse<CleaningStatus>(r.Status), r.RequestedBy, r.AssignedTo, r.Notes,
            r.StartedOnUtc is { } s ? new DateTimeOffset(s, TimeSpan.Zero) : null,
            r.CompletedOnUtc is { } c ? new DateTimeOffset(c, TimeSpan.Zero) : null,
            new DateTimeOffset(r.CreatedOnUtc, TimeSpan.Zero),
            r.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<CleaningTaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {SelectColumns} {FromJoins} where t.id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CleaningTaskDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<CleaningTaskDto>> SearchByFacilityAsync(Guid facilityId, CleaningTaskFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where t.facility_id = @facilityId");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (sp.name ilike @search or sp.code ilike @search or t.assigned_to ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and t.status = @status");
            p.Add("status", filter.Status.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.Protocol))
        {
            where.Append(" and t.protocol = @protocol");
            p.Add("protocol", filter.Protocol.Trim());
        }
        if (filter.SpaceId is { } sid)
        {
            where.Append(" and t.space_id = @spaceId");
            p.Add("spaceId", sid);
        }
        if (filter.ActiveOnly)
            where.Append(" and t.status in ('Pending', 'InProgress')");

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from organization.cleaning_tasks t left join organization.spaces sp on sp.id = t.space_id {where}",
            p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<CleaningTaskDto>(new CommandDefinition(
            $"""
             select {SelectColumns} {FromJoins} {where}
             order by
                 case t.status when 'InProgress' then 0 when 'Pending' then 1 when 'Completed' then 2 else 3 end,
                 t.created_on_utc desc
             limit @limit offset @offset
             """,
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<CleaningTaskDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task<int> CountActiveForSpaceAsync(Guid spaceId, Guid? excludeTaskId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select count(*)::int from organization.cleaning_tasks
            where space_id = @spaceId and status in ('Pending', 'InProgress')
              and (@excludeTaskId is null or id <> @excludeTaskId)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql, new { spaceId, excludeTaskId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<CleaningTaskItemDto>> GetItemsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", task_id as "TaskId", label as "Label", done as "Done",
                   done_on_utc as "DoneOnUtc", sort_order as "SortOrder"
            from organization.cleaning_task_items
            where task_id = @taskId
            order by sort_order, created_on_utc
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<CleaningTaskItemDto>(
            new CommandDefinition(sql, new { taskId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<CleaningTaskItem?> FindItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", task_id as "TaskId", label as "Label", done as "Done",
                   done_on_utc as "DoneOnUtc", sort_order as "SortOrder", created_on_utc as "CreatedOnUtc"
            from organization.cleaning_task_items where id = @itemId
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var r = await connection.QuerySingleOrDefaultAsync<ItemRow>(
            new CommandDefinition(sql, new { itemId }, cancellationToken: cancellationToken));
        if (r is null) return null;
        return CleaningTaskItem.Rehydrate(
            r.Id, r.TaskId, r.Label, r.Done,
            r.DoneOnUtc is { } d ? new DateTimeOffset(d, TimeSpan.Zero) : null,
            r.SortOrder, new DateTimeOffset(r.CreatedOnUtc, TimeSpan.Zero));
    }

    public async Task AddItemAsync(CleaningTaskItem item, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.cleaning_task_items
                (id, task_id, label, done, done_on_utc, sort_order, created_on_utc)
            values
                (@Id, @TaskId, @Label, @Done, @DoneOnUtc, @SortOrder, @CreatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ItemParams(item), cancellationToken: cancellationToken));
    }

    public async Task UpdateItemAsync(CleaningTaskItem item, CancellationToken cancellationToken = default)
    {
        const string sql = "update organization.cleaning_task_items set done = @Done, done_on_utc = @DoneOnUtc where id = @Id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ItemParams(item), cancellationToken: cancellationToken));
    }

    public async Task DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from organization.cleaning_task_items where id = @itemId", new { itemId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> AllItemsDoneAsync(Guid taskId, Guid? ignoreItemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select not exists(
                select 1 from organization.cleaning_task_items
                where task_id = @taskId and done = false and (@ignoreItemId is null or id <> @ignoreItemId))
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { taskId, ignoreItemId }, cancellationToken: cancellationToken));
    }

    public async Task<int> NextSortOrderAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        const string sql = "select coalesce(max(sort_order), -1) + 1 from organization.cleaning_task_items where task_id = @taskId";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { taskId }, cancellationToken: cancellationToken));
    }

    private static object ToParams(CleaningTask t) => new
    {
        t.Id,
        t.FacilityId,
        t.SpaceId,
        Protocol = t.Protocol.ToString(),
        Trigger = t.Trigger.ToString(),
        Status = t.Status.ToString(),
        t.RequestedBy,
        t.AssignedTo,
        t.Notes,
        t.StartedOnUtc,
        t.CompletedOnUtc,
        t.CreatedOnUtc,
        t.UpdatedOnUtc,
    };

    private static object ItemParams(CleaningTaskItem i) => new
    {
        i.Id,
        i.TaskId,
        i.Label,
        i.Done,
        i.DoneOnUtc,
        i.SortOrder,
        i.CreatedOnUtc,
    };

    private sealed class TaskRow
    {
        public Guid Id { get; init; }
        public Guid FacilityId { get; init; }
        public Guid SpaceId { get; init; }
        public string Protocol { get; init; } = string.Empty;
        public string Trigger { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string? RequestedBy { get; init; }
        public string? AssignedTo { get; init; }
        public string? Notes { get; init; }
        public DateTime? StartedOnUtc { get; init; }
        public DateTime? CompletedOnUtc { get; init; }
        public DateTime CreatedOnUtc { get; init; }
        public DateTime? UpdatedOnUtc { get; init; }
    }

    private sealed class ItemRow
    {
        public Guid Id { get; init; }
        public Guid TaskId { get; init; }
        public string Label { get; init; } = string.Empty;
        public bool Done { get; init; }
        public DateTime? DoneOnUtc { get; init; }
        public int SortOrder { get; init; }
        public DateTime CreatedOnUtc { get; init; }
    }
}
