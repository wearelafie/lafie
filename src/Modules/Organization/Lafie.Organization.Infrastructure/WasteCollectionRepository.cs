using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Waste.Collections;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des collectes de déchets (DASRI &amp; assimilés, schéma `organization`).</summary>
public sealed class WasteCollectionRepository(IDbConnectionFactory connectionFactory) : IWasteCollectionRepository
{
    private const string SelectColumns = """
        c.id as "Id", c.facility_id as "FacilityId", c.stream as "Stream", c.weight_kg as "WeightKg",
        c.container_count as "ContainerCount", c.status as "Status", c.space_id as "SpaceId",
        sp.name as "SpaceName", sp.code as "SpaceCode", c.slip_id as "SlipId", sl.reference as "SlipReference",
        c.collected_on as "CollectedOn", c.collected_by as "CollectedBy", c.notes as "Notes",
        c.created_on_utc as "CreatedOnUtc", c.updated_on_utc as "UpdatedOnUtc"
        """;

    private const string FromJoins = """
        from organization.waste_collections c
        left join organization.spaces sp on sp.id = c.space_id
        left join organization.waste_elimination_slips sl on sl.id = c.slip_id
        """;

    public async Task AddAsync(WasteCollection c, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.waste_collections
                (id, facility_id, stream, weight_kg, container_count, status, space_id, slip_id, collected_on, collected_by, notes, created_on_utc, updated_on_utc)
            values
                (@Id, @FacilityId, @Stream, @WeightKg, @ContainerCount, @Status, @SpaceId, @SlipId, @CollectedOn, @CollectedBy, @Notes, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(c), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(WasteCollection c, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.waste_collections set
                stream = @Stream, weight_kg = @WeightKg, container_count = @ContainerCount, status = @Status,
                space_id = @SpaceId, slip_id = @SlipId, collected_on = @CollectedOn, collected_by = @CollectedBy,
                notes = @Notes, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(c), cancellationToken: cancellationToken));
    }

    public async Task<WasteCollection?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return WasteCollection.Rehydrate(
            dto.Id, dto.FacilityId, Enum.Parse<WasteStream>(dto.Stream), dto.WeightKg, dto.ContainerCount,
            Enum.Parse<WasteCollectionStatus>(dto.Status), dto.SpaceId, dto.SlipId, dto.CollectedOn, dto.CollectedBy, dto.Notes,
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<WasteCollectionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {SelectColumns} {FromJoins} where c.id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WasteCollectionDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<WasteCollectionDto>> SearchByFacilityAsync(Guid facilityId, WasteCollectionFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where c.facility_id = @facilityId");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (c.collected_by ilike @search or c.notes ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Stream))
        {
            where.Append(" and c.stream = @stream");
            p.Add("stream", filter.Stream.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and c.status = @status");
            p.Add("status", filter.Status.Trim());
        }
        if (filter.UnassignedOnly)
            where.Append(" and c.slip_id is null and c.status = 'Collected'");

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from organization.waste_collections c {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<WasteCollectionDto>(new CommandDefinition(
            $"select {SelectColumns} {FromJoins} {where} order by c.collected_on desc nulls last, c.created_on_utc desc limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<WasteCollectionDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task<IReadOnlyList<WasteCollectionDto>> GetBySlipAsync(Guid slipId, CancellationToken cancellationToken = default)
    {
        var sql = $"select {SelectColumns} {FromJoins} where c.slip_id = @slipId order by c.created_on_utc";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<WasteCollectionDto>(
            new CommandDefinition(sql, new { slipId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetSlipCollectionIdsAsync(Guid slipId, CancellationToken cancellationToken = default)
    {
        const string sql = "select id from organization.waste_collections where slip_id = @slipId";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<Guid>(new CommandDefinition(sql, new { slipId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<decimal> SumWeightForSlipAsync(Guid slipId, CancellationToken cancellationToken = default)
    {
        const string sql = "select coalesce(sum(weight_kg), 0) from organization.waste_collections where slip_id = @slipId and status <> 'Cancelled'";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<decimal>(new CommandDefinition(sql, new { slipId }, cancellationToken: cancellationToken));
    }

    private static object ToParams(WasteCollection c) => new
    {
        c.Id,
        c.FacilityId,
        Stream = c.Stream.ToString(),
        c.WeightKg,
        c.ContainerCount,
        Status = c.Status.ToString(),
        c.SpaceId,
        c.SlipId,
        c.CollectedOn,
        c.CollectedBy,
        c.Notes,
        c.CreatedOnUtc,
        c.UpdatedOnUtc,
    };
}
