using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application.Waste.Slips;
using Lafie.Organization.Domain;
using Lafie.Persistence;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des bordereaux d'élimination (DASRI &amp; assimilés, schéma `organization`).</summary>
public sealed class WasteSlipRepository(IDbConnectionFactory connectionFactory) : IWasteSlipRepository
{
    private const string Columns = """
        id as "Id", facility_id as "FacilityId", reference as "Reference", stream as "Stream", status as "Status",
        carrier as "Carrier", destination as "Destination", treatment as "Treatment", total_weight_kg as "TotalWeightKg",
        shipped_on_utc as "ShippedOnUtc", treated_on_utc as "TreatedOnUtc", notes as "Notes",
        created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc",
        (select count(*) from organization.waste_collections wc where wc.slip_id = s.id)::int as "CollectionCount",
        (select coalesce(sum(wc.weight_kg), 0) from organization.waste_collections wc where wc.slip_id = s.id and wc.status <> 'Cancelled') as "LiveWeightKg"
        """;

    public async Task AddAsync(WasteEliminationSlip slip, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into organization.waste_elimination_slips
                (id, facility_id, reference, stream, status, carrier, destination, treatment, total_weight_kg, shipped_on_utc, treated_on_utc, notes, created_on_utc, updated_on_utc)
            values
                (@Id, @FacilityId, @Reference, @Stream, @Status, @Carrier, @Destination, @Treatment, @TotalWeightKg, @ShippedOnUtc, @TreatedOnUtc, @Notes, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(slip), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(WasteEliminationSlip slip, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update organization.waste_elimination_slips set
                stream = @Stream, status = @Status, carrier = @Carrier, destination = @Destination, treatment = @Treatment,
                total_weight_kg = @TotalWeightKg, shipped_on_utc = @ShippedOnUtc, treated_on_utc = @TreatedOnUtc,
                notes = @Notes, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(slip), cancellationToken: cancellationToken));
    }

    public async Task<WasteEliminationSlip?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return WasteEliminationSlip.Rehydrate(
            dto.Id, dto.FacilityId, dto.Reference, Enum.Parse<WasteStream>(dto.Stream),
            Enum.Parse<WasteSlipStatus>(dto.Status), dto.Carrier, dto.Destination,
            dto.Treatment is { } tr ? Enum.Parse<WasteTreatment>(tr) : null, dto.TotalWeightKg,
            dto.ShippedOnUtc is { } sh ? new DateTimeOffset(sh, TimeSpan.Zero) : null,
            dto.TreatedOnUtc is { } tt ? new DateTimeOffset(tt, TimeSpan.Zero) : null, dto.Notes,
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<WasteSlipDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.waste_elimination_slips s where id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WasteSlipDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<WasteSlipDto>> SearchByFacilityAsync(Guid facilityId, WasteSlipFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where s.facility_id = @facilityId");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (s.reference ilike @search or s.carrier ilike @search or s.destination ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Stream))
        {
            where.Append(" and s.stream = @stream");
            p.Add("stream", filter.Stream.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and s.status = @status");
            p.Add("status", filter.Status.Trim());
        }
        if (filter.OpenOnly)
            where.Append(" and s.status in ('Draft', 'Shipped')");

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from organization.waste_elimination_slips s {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<WasteSlipDto>(new CommandDefinition(
            $"select {Columns} from organization.waste_elimination_slips s {where} order by s.created_on_utc desc limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<WasteSlipDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task<bool> ReferenceExistsAsync(Guid facilityId, string reference, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.waste_elimination_slips where facility_id = @facilityId and lower(reference) = lower(@reference) and (@excludeId is null or id <> @excludeId))";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { facilityId, reference, excludeId }, cancellationToken: cancellationToken));
    }

    private static object ToParams(WasteEliminationSlip s) => new
    {
        s.Id,
        s.FacilityId,
        s.Reference,
        Stream = s.Stream.ToString(),
        Status = s.Status.ToString(),
        s.Carrier,
        s.Destination,
        Treatment = s.Treatment?.ToString(),
        s.TotalWeightKg,
        s.ShippedOnUtc,
        s.TreatedOnUtc,
        s.Notes,
        s.CreatedOnUtc,
        s.UpdatedOnUtc,
    };
}
