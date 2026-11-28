using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Pharmacy.Application.Stock;
using Lafie.Pharmacy.Domain;
using Lafie.Persistence;

namespace Lafie.Pharmacy.Infrastructure;

/// <summary>Persistance Dapper du stock pharmaceutique (fiches, lots, mouvements ; schéma `pharmacy`).</summary>
public sealed class StockRepository(IDbConnectionFactory connectionFactory) : IStockRepository
{
    // --- Fiches de stock (seuil) ---

    public async Task<StockItem?> FindItemAsync(Guid facilityId, Guid medicationId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", facility_id as "FacilityId", medication_id as "MedicationId",
                   reorder_threshold as "ReorderThreshold", notes as "Notes",
                   created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
            from pharmacy.stock_items where facility_id = @facilityId and medication_id = @medicationId
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var r = await connection.QuerySingleOrDefaultAsync<StockItemRow>(
            new CommandDefinition(sql, new { facilityId, medicationId }, cancellationToken: cancellationToken));
        return r is null ? null : StockItem.Rehydrate(
            r.Id, r.FacilityId, r.MedicationId, r.ReorderThreshold, r.Notes,
            new DateTimeOffset(r.CreatedOnUtc, TimeSpan.Zero),
            r.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task AddItemAsync(StockItem item, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into pharmacy.stock_items (id, facility_id, medication_id, reorder_threshold, notes, created_on_utc, updated_on_utc)
            values (@Id, @FacilityId, @MedicationId, @ReorderThreshold, @Notes, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            item.Id, item.FacilityId, item.MedicationId, item.ReorderThreshold, item.Notes, item.CreatedOnUtc, item.UpdatedOnUtc,
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateItemAsync(StockItem item, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update pharmacy.stock_items set reorder_threshold = @ReorderThreshold, notes = @Notes, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            item.Id, item.ReorderThreshold, item.Notes, item.UpdatedOnUtc,
        }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<StockItemDto>> SearchStockAsync(Guid facilityId, StockFilter filter, CancellationToken cancellationToken = default)
    {
        // Positions de stock = médicaments ayant au moins un lot OU une fiche de seuil.
        var where = new StringBuilder("where m.facility_id = @facilityId and (si.id is not null or agg.medication_id is not null)");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (m.name ilike @search or m.code ilike @search or m.inn ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (filter.LowOnly)
            where.Append(" and coalesce(si.reorder_threshold, 0) > 0 and coalesce(agg.on_hand, 0) <= coalesce(si.reorder_threshold, 0)");

        const string from = """
            from pharmacy.medications m
            left join pharmacy.stock_items si on si.facility_id = m.facility_id and si.medication_id = m.id
            left join (
                select facility_id, medication_id, sum(quantity_on_hand)::int as on_hand, count(*)::int as lot_count,
                       min(expiry_on) filter (where quantity_on_hand > 0) as earliest_expiry
                from pharmacy.stock_lots group by facility_id, medication_id
            ) agg on agg.facility_id = m.facility_id and agg.medication_id = m.id
            """;

        const string select = """
            select m.id as "MedicationId", m.facility_id as "FacilityId", m.code as "Code", m.name as "Name",
                   m.unit as "Unit", m.form as "Form", m.controlled as "Controlled",
                   coalesce(agg.on_hand, 0) as "OnHand", coalesce(si.reorder_threshold, 0) as "ReorderThreshold",
                   coalesce(agg.lot_count, 0) as "LotCount", agg.earliest_expiry as "EarliestExpiry",
                   (coalesce(si.reorder_threshold, 0) > 0 and coalesce(agg.on_hand, 0) <= coalesce(si.reorder_threshold, 0)) as "LowStock"
            """;

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) {from} {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<StockItemDto>(new CommandDefinition(
            $"{select} {from} {where} order by m.name limit @limit offset @offset", p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<StockItemDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    // --- Lots ---

    private const string LotColumns = """
        id as "Id", facility_id as "FacilityId", medication_id as "MedicationId", lot_number as "LotNumber",
        expiry_on as "ExpiryOn", quantity_initial as "QuantityInitial", quantity_on_hand as "QuantityOnHand",
        unit_cost as "UnitCost", supplier_name as "SupplierName", received_on as "ReceivedOn", notes as "Notes",
        created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
        """;

    public async Task AddLotAsync(StockLot lot, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into pharmacy.stock_lots
                (id, facility_id, medication_id, lot_number, expiry_on, quantity_initial, quantity_on_hand, unit_cost, supplier_name, received_on, notes, created_on_utc, updated_on_utc)
            values
                (@Id, @FacilityId, @MedicationId, @LotNumber, @ExpiryOn, @QuantityInitial, @QuantityOnHand, @UnitCost, @SupplierName, @ReceivedOn, @Notes, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, LotParams(lot), cancellationToken: cancellationToken));
    }

    public async Task UpdateLotAsync(StockLot lot, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update pharmacy.stock_lots set
                lot_number = @LotNumber, expiry_on = @ExpiryOn, quantity_on_hand = @QuantityOnHand,
                unit_cost = @UnitCost, supplier_name = @SupplierName, notes = @Notes, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, LotParams(lot), cancellationToken: cancellationToken));
    }

    public async Task<StockLot?> FindLotAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetLotByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return StockLot.Rehydrate(
            dto.Id, dto.FacilityId, dto.MedicationId, dto.LotNumber, dto.ExpiryOn,
            dto.QuantityInitial, dto.QuantityOnHand, dto.UnitCost, dto.SupplierName,
            dto.ReceivedOn, dto.Notes,
            new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero), null);
    }

    public async Task<StockLotDto?> GetLotByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {LotColumns} from pharmacy.stock_lots where id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<StockLotDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<StockLotDto>> GetLotsAsync(Guid facilityId, Guid medicationId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            select {LotColumns} from pharmacy.stock_lots
            where facility_id = @facilityId and medication_id = @medicationId
            order by expiry_on asc nulls last, received_on asc
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<StockLotDto>(
            new CommandDefinition(sql, new { facilityId, medicationId }, cancellationToken: cancellationToken))).ToList();
    }

    // --- Mouvements (grand livre) ---

    public async Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into pharmacy.stock_movements
                (id, facility_id, medication_id, lot_id, type, quantity, reason, performed_by, occurred_on_utc, notes)
            values
                (@Id, @FacilityId, @MedicationId, @LotId, @Type, @Quantity, @Reason, @PerformedBy, @OccurredOnUtc, @Notes)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, MovementParams(movement), cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(Guid facilityId, Guid medicationId, int limit, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select mv.id as "Id", mv.medication_id as "MedicationId", mv.lot_id as "LotId", l.lot_number as "LotNumber",
                   mv.type as "Type", mv.quantity as "Quantity", mv.reason as "Reason", mv.performed_by as "PerformedBy",
                   mv.occurred_on_utc as "OccurredOnUtc", mv.notes as "Notes"
            from pharmacy.stock_movements mv
            left join pharmacy.stock_lots l on l.id = mv.lot_id
            where mv.facility_id = @facilityId and mv.medication_id = @medicationId
            order by mv.occurred_on_utc desc
            limit @limit
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<StockMovementDto>(
            new CommandDefinition(sql, new { facilityId, medicationId, limit }, cancellationToken: cancellationToken))).ToList();
    }

    // --- Sortie FEFO (transactionnelle) ---

    public async Task<Result> IssueFefoAsync(
        Guid facilityId, Guid medicationId, int quantity, StockMovementType type, MovementMeta meta,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) return Result.Failure("La quantité à sortir doit être strictement positive.");

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var tx = connection.BeginTransaction();

        // Verrouille les lots actifs par péremption croissante (FEFO).
        const string selectSql = """
            select id as "Id", quantity_on_hand as "QuantityOnHand" from pharmacy.stock_lots
            where facility_id = @facilityId and medication_id = @medicationId and quantity_on_hand > 0
            order by expiry_on asc nulls last, received_on asc, created_on_utc asc
            for update
            """;
        var lots = (await connection.QueryAsync<(Guid Id, int QuantityOnHand)>(
            new CommandDefinition(selectSql, new { facilityId, medicationId }, tx, cancellationToken: cancellationToken))).ToList();

        var available = lots.Sum(l => l.QuantityOnHand);
        if (available < quantity)
        {
            tx.Rollback();
            return Result.Failure($"Stock insuffisant : {available} disponible(s), {quantity} demandé(s).");
        }

        var remaining = quantity;
        var now = DateTimeOffset.UtcNow;
        foreach (var lot in lots)
        {
            if (remaining <= 0) break;
            var take = Math.Min(remaining, lot.QuantityOnHand);

            await connection.ExecuteAsync(new CommandDefinition(
                "update pharmacy.stock_lots set quantity_on_hand = quantity_on_hand - @take, updated_on_utc = @now where id = @id",
                new { take, now, id = lot.Id }, tx, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("""
                insert into pharmacy.stock_movements (id, facility_id, medication_id, lot_id, type, quantity, reason, performed_by, occurred_on_utc, notes)
                values (@id, @facilityId, @medicationId, @lotId, @type, @quantity, @reason, @performedBy, @now, @notes)
                """,
                new
                {
                    id = Guid.NewGuid(), facilityId, medicationId, lotId = lot.Id, type = type.ToString(),
                    quantity = -take, meta.Reason, performedBy = meta.PerformedBy, now, meta.Notes,
                }, tx, cancellationToken: cancellationToken));

            remaining -= take;
        }

        tx.Commit();
        return Result.Success();
    }

    // --- Mapping ---

    private static object LotParams(StockLot l) => new
    {
        l.Id, l.FacilityId, l.MedicationId, l.LotNumber, l.ExpiryOn, l.QuantityInitial, l.QuantityOnHand,
        l.UnitCost, l.SupplierName, l.ReceivedOn, l.Notes, l.CreatedOnUtc, l.UpdatedOnUtc,
    };

    private static object MovementParams(StockMovement m) => new
    {
        m.Id, m.FacilityId, m.MedicationId, m.LotId, Type = m.Type.ToString(), m.Quantity,
        m.Reason, m.PerformedBy, m.OccurredOnUtc, m.Notes,
    };

    private sealed record StockItemRow(
        Guid Id, Guid FacilityId, Guid MedicationId, int ReorderThreshold, string? Notes,
        DateTime CreatedOnUtc, DateTime? UpdatedOnUtc);
}
