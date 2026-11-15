using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Sterilization.Sets;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des boîtes d'instruments et de leurs affectations (CSSD, schéma `organization`).</summary>
public sealed class InstrumentSetRepository(IDbConnectionFactory connectionFactory) : IInstrumentSetRepository
{
    private const string Columns = """
        id as "Id", facility_id as "FacilityId", code as "Code", name as "Name", description as "Description",
        category as "Category", item_count as "ItemCount", status as "Status", last_cycle_id as "LastCycleId",
        sterilized_on_utc as "SterilizedOnUtc", expiry_on as "ExpiryOn", created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
        """;

    public async Task AddAsync(InstrumentSet s, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.instrument_sets
                (id, facility_id, code, name, description, category, item_count, status, last_cycle_id, sterilized_on_utc, expiry_on, created_on_utc, updated_on_utc)
            values
                (@Id, @FacilityId, @Code, @Name, @Description, @Category, @ItemCount, @Status, @LastCycleId, @SterilizedOnUtc, @ExpiryOn, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(s), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(InstrumentSet s, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.instrument_sets set
                name = @Name, description = @Description, category = @Category, item_count = @ItemCount,
                status = @Status, last_cycle_id = @LastCycleId, sterilized_on_utc = @SterilizedOnUtc,
                expiry_on = @ExpiryOn, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(s), cancellationToken: cancellationToken));
    }

    public async Task<InstrumentSet?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return InstrumentSet.Rehydrate(
            dto.Id, dto.FacilityId, dto.Code, dto.Name, dto.Description, dto.Category, dto.ItemCount,
            Enum.Parse<InstrumentSetStatus>(dto.Status), dto.LastCycleId,
            dto.SterilizedOnUtc is { } s ? new DateTimeOffset(s, TimeSpan.Zero) : null, dto.ExpiryOn,
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<InstrumentSetDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.instrument_sets where id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<InstrumentSetDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<InstrumentSetDto>> SearchByFacilityAsync(Guid facilityId, InstrumentSetFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where facility_id = @facilityId");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

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
        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            where.Append(" and category = @category");
            p.Add("category", filter.Category.Trim());
        }
        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from organization.instrument_sets {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<InstrumentSetDto>(new CommandDefinition(
            $"select {Columns} from organization.instrument_sets {where} order by code limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<InstrumentSetDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.instrument_sets where facility_id = @facilityId and lower(code) = lower(@code) and (@excludeId is null or id <> @excludeId))";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { facilityId, code, excludeId }, cancellationToken: cancellationToken));
    }

    // --- Affectations ---

    public async Task AddUsageAsync(InstrumentSetUsage u, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.instrument_set_usages
                (id, set_id, intervention_label, patient_ref, scheduled_on, performed_on_utc, performed_by, status, notes, created_on_utc, updated_on_utc)
            values
                (@Id, @SetId, @InterventionLabel, @PatientRef, @ScheduledOn, @PerformedOnUtc, @PerformedBy, @Status, @Notes, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, UsageParams(u), cancellationToken: cancellationToken));
    }

    public async Task UpdateUsageAsync(InstrumentSetUsage u, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.instrument_set_usages set
                intervention_label = @InterventionLabel, patient_ref = @PatientRef, scheduled_on = @ScheduledOn,
                performed_on_utc = @PerformedOnUtc, performed_by = @PerformedBy, status = @Status, notes = @Notes, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, UsageParams(u), cancellationToken: cancellationToken));
    }

    public async Task<InstrumentSetUsage?> FindUsageAsync(Guid usageId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", set_id as "SetId", intervention_label as "InterventionLabel", patient_ref as "PatientRef",
                   scheduled_on as "ScheduledOn", performed_on_utc as "PerformedOnUtc", performed_by as "PerformedBy",
                   status as "Status", notes as "Notes", created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
            from organization.instrument_set_usages where id = @usageId
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var r = await connection.QuerySingleOrDefaultAsync<UsageRow>(
            new CommandDefinition(sql, new { usageId }, cancellationToken: cancellationToken));
        if (r is null) return null;
        return InstrumentSetUsage.Rehydrate(
            r.Id, r.SetId, r.InterventionLabel, r.PatientRef, r.ScheduledOn,
            r.PerformedOnUtc is { } pf ? new DateTimeOffset(pf, TimeSpan.Zero) : null, r.PerformedBy,
            Enum.Parse<SetUsageStatus>(r.Status), r.Notes,
            new DateTimeOffset(r.CreatedOnUtc, TimeSpan.Zero),
            r.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<IReadOnlyList<InstrumentSetUsageDto>> GetUsagesAsync(Guid setId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", set_id as "SetId", intervention_label as "InterventionLabel", patient_ref as "PatientRef",
                   scheduled_on as "ScheduledOn", performed_on_utc as "PerformedOnUtc", performed_by as "PerformedBy",
                   status as "Status", notes as "Notes", created_on_utc as "CreatedOnUtc"
            from organization.instrument_set_usages
            where set_id = @setId
            order by created_on_utc desc
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<InstrumentSetUsageDto>(
            new CommandDefinition(sql, new { setId }, cancellationToken: cancellationToken))).ToList();
    }

    private static object ToParams(InstrumentSet s) => new
    {
        s.Id,
        s.FacilityId,
        s.Code,
        s.Name,
        s.Description,
        s.Category,
        s.ItemCount,
        Status = s.Status.ToString(),
        s.LastCycleId,
        s.SterilizedOnUtc,
        s.ExpiryOn,
        s.CreatedOnUtc,
        s.UpdatedOnUtc,
    };

    private static object UsageParams(InstrumentSetUsage u) => new
    {
        u.Id,
        u.SetId,
        u.InterventionLabel,
        u.PatientRef,
        u.ScheduledOn,
        u.PerformedOnUtc,
        u.PerformedBy,
        Status = u.Status.ToString(),
        u.Notes,
        u.CreatedOnUtc,
        u.UpdatedOnUtc,
    };

    private sealed class UsageRow
    {
        public Guid Id { get; init; }
        public Guid SetId { get; init; }
        public string InterventionLabel { get; init; } = string.Empty;
        public string? PatientRef { get; init; }
        public DateOnly? ScheduledOn { get; init; }
        public DateTime? PerformedOnUtc { get; init; }
        public string? PerformedBy { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? Notes { get; init; }
        public DateTime CreatedOnUtc { get; init; }
        public DateTime? UpdatedOnUtc { get; init; }
    }
}
