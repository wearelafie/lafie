using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Pharmacy.Application.Purchasing;
using Lafie.Pharmacy.Domain;
using Lafie.Persistence;

namespace Lafie.Pharmacy.Infrastructure;

/// <summary>Persistance Dapper des bons de commande et de leurs lignes (schéma `pharmacy`).</summary>
public sealed class PurchaseOrderRepository(IDbConnectionFactory connectionFactory) : IPurchaseOrderRepository
{
    public async Task AddAsync(PurchaseOrder order, IReadOnlyList<PurchaseOrderLine> lines, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var tx = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition(InsertHeaderSql, HeaderParams(order), tx, cancellationToken: cancellationToken));
        foreach (var line in lines)
            await connection.ExecuteAsync(new CommandDefinition(InsertLineSql, LineParams(line), tx, cancellationToken: cancellationToken));
        tx.Commit();
    }

    public async Task UpdateHeaderAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update pharmacy.purchase_orders set
                supplier_id = @SupplierId, status = @Status, order_date = @OrderDate, expected_on = @ExpectedOn,
                notes = @Notes, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, HeaderParams(order), cancellationToken: cancellationToken));
    }

    public async Task ReplaceLinesAsync(Guid orderId, IReadOnlyList<PurchaseOrderLine> lines, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var tx = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition(
            "delete from pharmacy.purchase_order_lines where purchase_order_id = @orderId", new { orderId }, tx, cancellationToken: cancellationToken));
        foreach (var line in lines)
            await connection.ExecuteAsync(new CommandDefinition(InsertLineSql, LineParams(line), tx, cancellationToken: cancellationToken));
        tx.Commit();
    }

    public async Task UpdateLineAsync(PurchaseOrderLine line, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update pharmacy.purchase_order_lines set
                quantity_ordered = @QuantityOrdered, quantity_received = @QuantityReceived,
                unit_price = @UnitPrice, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, LineParams(line), cancellationToken: cancellationToken));
    }

    public async Task<PurchaseOrder?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", facility_id as "FacilityId", supplier_id as "SupplierId", reference as "Reference",
                   status as "Status", order_date as "OrderDate", expected_on as "ExpectedOn", notes as "Notes",
                   created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
            from pharmacy.purchase_orders where id = @id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var r = await connection.QuerySingleOrDefaultAsync<HeaderRow>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
        return r is null ? null : PurchaseOrder.Rehydrate(
            r.Id, r.FacilityId, r.SupplierId, r.Reference, Enum.Parse<PurchaseOrderStatus>(r.Status),
            r.OrderDate, r.ExpectedOn, r.Notes, new DateTimeOffset(r.CreatedOnUtc, TimeSpan.Zero),
            r.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<PurchaseOrderLine?> FindLineAsync(Guid lineId, CancellationToken cancellationToken = default)
    {
        var r = await QueryLineRowsAsync("where id = @lineId", new { lineId }, cancellationToken);
        return r.Count == 0 ? null : ToAggregate(r[0]);
    }

    public async Task<IReadOnlyList<PurchaseOrderLine>> GetLineAggregatesAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryLineRowsAsync("where purchase_order_id = @orderId", new { orderId }, cancellationToken);
        return rows.Select(ToAggregate).ToList();
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"{HeaderProjectionSql} where po.id = @id group by po.id, s.name";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<PurchaseOrderDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<PurchaseOrderLineDto>> GetLinesAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select l.id as "Id", l.purchase_order_id as "PurchaseOrderId", l.medication_id as "MedicationId",
                   m.code as "MedicationCode", m.name as "MedicationName", m.unit as "Unit",
                   l.quantity_ordered as "QuantityOrdered", l.quantity_received as "QuantityReceived", l.unit_price as "UnitPrice"
            from pharmacy.purchase_order_lines l
            join pharmacy.medications m on m.id = l.medication_id
            where l.purchase_order_id = @orderId
            order by m.name
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<PurchaseOrderLineDto>(
            new CommandDefinition(sql, new { orderId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<bool> ReferenceExistsAsync(Guid facilityId, string reference, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from pharmacy.purchase_orders where facility_id = @facilityId and lower(reference) = lower(@reference))";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { facilityId, reference }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<PurchaseOrderDto>> SearchByFacilityAsync(Guid facilityId, PurchaseOrderFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where po.facility_id = @facilityId");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (po.reference ilike @search or s.name ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and po.status = @status");
            p.Add("status", filter.Status);
        }
        if (filter.SupplierId is { } supplierId)
        {
            where.Append(" and po.supplier_id = @supplierId");
            p.Add("supplierId", supplierId);
        }

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from pharmacy.purchase_orders po join pharmacy.suppliers s on s.id = po.supplier_id {where}",
            p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<PurchaseOrderDto>(new CommandDefinition(
            $"{HeaderProjectionSql} {where} group by po.id, s.name order by po.created_on_utc desc limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<PurchaseOrderDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    // --- SQL partagé ---

    private const string InsertHeaderSql = """
        insert into pharmacy.purchase_orders
            (id, facility_id, supplier_id, reference, status, order_date, expected_on, notes, created_on_utc, updated_on_utc)
        values
            (@Id, @FacilityId, @SupplierId, @Reference, @Status, @OrderDate, @ExpectedOn, @Notes, @CreatedOnUtc, @UpdatedOnUtc)
        """;

    private const string InsertLineSql = """
        insert into pharmacy.purchase_order_lines
            (id, purchase_order_id, medication_id, quantity_ordered, quantity_received, unit_price, created_on_utc, updated_on_utc)
        values
            (@Id, @PurchaseOrderId, @MedicationId, @QuantityOrdered, @QuantityReceived, @UnitPrice, @CreatedOnUtc, @UpdatedOnUtc)
        """;

    // En-tête + agrégats des lignes (nb, total commandé/reçu, montant). group by ajouté par l'appelant.
    private const string HeaderProjectionSql = """
        select po.id as "Id", po.facility_id as "FacilityId", po.supplier_id as "SupplierId", s.name as "SupplierName",
               po.reference as "Reference", po.status as "Status", po.order_date as "OrderDate", po.expected_on as "ExpectedOn",
               po.notes as "Notes",
               count(l.id)::int as "LineCount",
               coalesce(sum(l.quantity_ordered), 0)::int as "TotalOrdered",
               coalesce(sum(l.quantity_received), 0)::int as "TotalReceived",
               coalesce(sum(l.quantity_ordered * coalesce(l.unit_price, 0)), 0) as "TotalAmount"
        from pharmacy.purchase_orders po
        join pharmacy.suppliers s on s.id = po.supplier_id
        left join pharmacy.purchase_order_lines l on l.purchase_order_id = po.id
        """;

    private const string LineColumns = """
        id as "Id", purchase_order_id as "PurchaseOrderId", medication_id as "MedicationId",
        quantity_ordered as "QuantityOrdered", quantity_received as "QuantityReceived", unit_price as "UnitPrice",
        created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
        """;

    private async Task<IReadOnlyList<LineRow>> QueryLineRowsAsync(string whereClause, object parameters, CancellationToken cancellationToken)
    {
        var sql = $"select {LineColumns} from pharmacy.purchase_order_lines {whereClause}";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<LineRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();
    }

    private static PurchaseOrderLine ToAggregate(LineRow r) => PurchaseOrderLine.Rehydrate(
        r.Id, r.PurchaseOrderId, r.MedicationId, r.QuantityOrdered, r.QuantityReceived, r.UnitPrice,
        new DateTimeOffset(r.CreatedOnUtc, TimeSpan.Zero),
        r.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);

    private static object HeaderParams(PurchaseOrder o) => new
    {
        o.Id, o.FacilityId, o.SupplierId, o.Reference, Status = o.Status.ToString(),
        o.OrderDate, o.ExpectedOn, o.Notes, o.CreatedOnUtc, o.UpdatedOnUtc,
    };

    private static object LineParams(PurchaseOrderLine l) => new
    {
        l.Id, l.PurchaseOrderId, l.MedicationId, l.QuantityOrdered, l.QuantityReceived, l.UnitPrice,
        l.CreatedOnUtc, l.UpdatedOnUtc,
    };

    private sealed record HeaderRow(
        Guid Id, Guid FacilityId, Guid SupplierId, string Reference, string Status,
        DateOnly? OrderDate, DateOnly? ExpectedOn, string? Notes, DateTime CreatedOnUtc, DateTime? UpdatedOnUtc);

    private sealed record LineRow(
        Guid Id, Guid PurchaseOrderId, Guid MedicationId, int QuantityOrdered, int QuantityReceived,
        decimal? UnitPrice, DateTime CreatedOnUtc, DateTime? UpdatedOnUtc);
}
