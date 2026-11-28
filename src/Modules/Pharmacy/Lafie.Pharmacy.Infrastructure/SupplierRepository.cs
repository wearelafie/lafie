using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Pharmacy.Application.Purchasing;
using Lafie.Pharmacy.Domain;
using Lafie.Persistence;

namespace Lafie.Pharmacy.Infrastructure;

/// <summary>Persistance Dapper des fournisseurs (schéma `pharmacy`).</summary>
public sealed class SupplierRepository(IDbConnectionFactory connectionFactory) : ISupplierRepository
{
    private const string Columns = """
        id as "Id", facility_id as "FacilityId", code as "Code", name as "Name", contact_name as "ContactName",
        email as "Email", phone as "Phone", address as "Address", active as "Active", notes as "Notes",
        created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
        """;

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into pharmacy.suppliers
                (id, facility_id, code, name, contact_name, email, phone, address, active, notes, created_on_utc, updated_on_utc)
            values
                (@Id, @FacilityId, @Code, @Name, @ContactName, @Email, @Phone, @Address, @Active, @Notes, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(supplier), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update pharmacy.suppliers set
                name = @Name, contact_name = @ContactName, email = @Email, phone = @Phone, address = @Address,
                active = @Active, notes = @Notes, updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(supplier), cancellationToken: cancellationToken));
    }

    public async Task<Supplier?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        return dto is null ? null : Supplier.Rehydrate(
            dto.Id, dto.FacilityId, dto.Code, dto.Name, dto.ContactName, dto.Email, dto.Phone, dto.Address,
            dto.Active, dto.Notes, new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from pharmacy.suppliers where id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<SupplierDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(Guid facilityId, string code, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from pharmacy.suppliers where facility_id = @facilityId and lower(code) = lower(@code))";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { facilityId, code }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<SupplierDto>> SearchByFacilityAsync(Guid facilityId, SupplierFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where facility_id = @facilityId");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (name ilike @search or code ilike @search or contact_name ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (filter.Active is { } active)
        {
            where.Append(" and active = @active");
            p.Add("active", active);
        }

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from pharmacy.suppliers {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<SupplierDto>(new CommandDefinition(
            $"select {Columns} from pharmacy.suppliers {where} order by name limit @limit offset @offset", p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<SupplierDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    private static object ToParams(Supplier s) => new
    {
        s.Id, s.FacilityId, s.Code, s.Name, s.ContactName, s.Email, s.Phone, s.Address, s.Active, s.Notes,
        s.CreatedOnUtc, s.UpdatedOnUtc,
    };
}
