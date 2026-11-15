using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Sterilization.Cycles;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des cycles d'autoclave et de leur charge (CSSD, schéma `organization`).</summary>
public sealed class SterilizationCycleRepository(IDbConnectionFactory connectionFactory) : ISterilizationCycleRepository
{
    private const string Columns = """
        id as "Id", facility_id as "FacilityId", autoclave as "Autoclave", cycle_number as "CycleNumber",
        program as "Program", status as "Status", operator_name as "Operator", temperature_c as "TemperatureC",
        pressure_bar as "PressureBar", duration_min as "DurationMin", started_on_utc as "StartedOnUtc",
        ended_on_utc as "EndedOnUtc", result_notes as "ResultNotes", created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc",
        (select count(*) from organization.sterilization_cycle_sets cs where cs.cycle_id = c.id)::int as "LoadCount"
        """;

    public async Task AddAsync(SterilizationCycle cycle, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.sterilization_cycles
                (id, facility_id, autoclave, cycle_number, program, status, operator_name, temperature_c, pressure_bar, duration_min, started_on_utc, ended_on_utc, result_notes, created_on_utc, updated_on_utc)
            values
                (@Id, @FacilityId, @Autoclave, @CycleNumber, @Program, @Status, @Operator, @TemperatureC, @PressureBar, @DurationMin, @StartedOnUtc, @EndedOnUtc, @ResultNotes, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(cycle), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(SterilizationCycle cycle, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.sterilization_cycles set
                autoclave = @Autoclave, cycle_number = @CycleNumber, program = @Program, status = @Status, operator_name = @Operator,
                temperature_c = @TemperatureC, pressure_bar = @PressureBar, duration_min = @DurationMin,
                started_on_utc = @StartedOnUtc, ended_on_utc = @EndedOnUtc, result_notes = @ResultNotes, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(cycle), cancellationToken: cancellationToken));
    }

    public async Task<SterilizationCycle?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return SterilizationCycle.Rehydrate(
            dto.Id, dto.FacilityId, dto.Autoclave, dto.CycleNumber, dto.Program,
            Enum.Parse<SterilizationCycleStatus>(dto.Status), dto.Operator,
            dto.TemperatureC, dto.PressureBar, dto.DurationMin,
            dto.StartedOnUtc is { } s ? new DateTimeOffset(s, TimeSpan.Zero) : null,
            dto.EndedOnUtc is { } e ? new DateTimeOffset(e, TimeSpan.Zero) : null,
            dto.ResultNotes,
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<SterilizationCycleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.sterilization_cycles c where id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<SterilizationCycleDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<SterilizationCycleDto>> SearchByFacilityAsync(Guid facilityId, SterilizationCycleFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where c.facility_id = @facilityId");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (c.autoclave ilike @search or c.cycle_number ilike @search or c.program ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and c.status = @status");
            p.Add("status", filter.Status.Trim());
        }
        if (filter.OpenOnly)
            where.Append(" and c.status in ('Planned', 'Running')");

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from organization.sterilization_cycles c {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<SterilizationCycleDto>(new CommandDefinition(
            $"select {Columns} from organization.sterilization_cycles c {where} order by c.created_on_utc desc limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<SterilizationCycleDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    // --- Charge ---

    public async Task AddToLoadAsync(Guid cycleId, Guid setId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.sterilization_cycle_sets (cycle_id, set_id)
            values (@cycleId, @setId)
            on conflict (cycle_id, set_id) do nothing
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { cycleId, setId }, cancellationToken: cancellationToken));
    }

    public async Task RemoveFromLoadAsync(Guid cycleId, Guid setId, CancellationToken cancellationToken = default)
    {
        const string sql = "delete from organization.sterilization_cycle_sets where cycle_id = @cycleId and set_id = @setId";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { cycleId, setId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> IsInLoadAsync(Guid cycleId, Guid setId, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.sterilization_cycle_sets where cycle_id = @cycleId and set_id = @setId)";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { cycleId, setId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Guid>> GetLoadSetIdsAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        const string sql = "select set_id from organization.sterilization_cycle_sets where cycle_id = @cycleId";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<Guid>(new CommandDefinition(sql, new { cycleId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<IReadOnlyList<CycleSetDto>> GetLoadAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select s.id as "SetId", s.code as "Code", s.name as "Name", s.status as "Status"
            from organization.sterilization_cycle_sets cs
            join organization.instrument_sets s on s.id = cs.set_id
            where cs.cycle_id = @cycleId
            order by s.code
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<CycleSetDto>(new CommandDefinition(sql, new { cycleId }, cancellationToken: cancellationToken))).ToList();
    }

    private static object ToParams(SterilizationCycle c) => new
    {
        c.Id,
        c.FacilityId,
        c.Autoclave,
        c.CycleNumber,
        c.Program,
        Status = c.Status.ToString(),
        c.Operator,
        c.TemperatureC,
        c.PressureBar,
        c.DurationMin,
        c.StartedOnUtc,
        c.EndedOnUtc,
        c.ResultNotes,
        c.CreatedOnUtc,
        c.UpdatedOnUtc,
    };
}
