using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Pharmacy.Application.Catalog;
using Lafie.Pharmacy.Domain;
using Lafie.Persistence;

namespace Lafie.Pharmacy.Infrastructure;

/// <summary>Persistance Dapper des fiches médicament (catalogue, schéma `pharmacy`).</summary>
public sealed class MedicationRepository(IDbConnectionFactory connectionFactory) : IMedicationRepository
{
    private const string Columns = """
        id as "Id", facility_id as "FacilityId", code as "Code", name as "Name", inn as "Inn",
        form as "Form", strength as "Strength", unit as "Unit", atc_code as "AtcCode",
        therapeutic_class as "TherapeuticClass", controlled as "Controlled", status as "Status",
        notes as "Notes", created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
        """;

    public async Task AddAsync(Medication medication, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into pharmacy.medications
                (id, facility_id, code, name, inn, form, strength, unit, atc_code, therapeutic_class, controlled, status, notes, created_on_utc, updated_on_utc)
            values
                (@Id, @FacilityId, @Code, @Name, @Inn, @Form, @Strength, @Unit, @AtcCode, @TherapeuticClass, @Controlled, @Status, @Notes, @CreatedOnUtc, @UpdatedOnUtc)
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(medication), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Medication medication, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update pharmacy.medications set
                name = @Name, inn = @Inn, form = @Form, strength = @Strength, unit = @Unit, atc_code = @AtcCode,
                therapeutic_class = @TherapeuticClass, controlled = @Controlled, status = @Status, notes = @Notes,
                updated_on_utc = @UpdatedOnUtc
            where id = @Id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(medication), cancellationToken: cancellationToken));
    }

    public async Task<Medication?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return Medication.Rehydrate(
            dto.Id, dto.FacilityId, dto.Code, dto.Name, dto.Inn, Enum.Parse<MedicationForm>(dto.Form), dto.Strength,
            dto.Unit, dto.AtcCode, dto.TherapeuticClass, dto.Controlled, Enum.Parse<MedicationStatus>(dto.Status),
            dto.Notes, new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
            dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<MedicationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from pharmacy.medications where id = @id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<MedicationDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select exists(
                select 1 from pharmacy.medications
                where facility_id = @facilityId and lower(code) = lower(@code) and (@excludeId is null or id <> @excludeId))
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { facilityId, code, excludeId }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<MedicationDto>> SearchByFacilityAsync(Guid facilityId, MedicationFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where m.facility_id = @facilityId");
        var p = new DynamicParameters();
        p.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (m.name ilike @search or m.code ilike @search or m.inn ilike @search or m.atc_code ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Form))
        {
            where.Append(" and m.form = @form");
            p.Add("form", filter.Form.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and m.status = @status");
            p.Add("status", filter.Status.Trim());
        }
        if (filter.Controlled is { } controlled)
        {
            where.Append(" and m.controlled = @controlled");
            p.Add("controlled", controlled);
        }

        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from pharmacy.medications m {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<MedicationDto>(new CommandDefinition(
            $"select {Columns} from pharmacy.medications m {where} order by m.name limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<MedicationDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    private static object ToParams(Medication m) => new
    {
        m.Id,
        m.FacilityId,
        m.Code,
        m.Name,
        m.Inn,
        Form = m.Form.ToString(),
        m.Strength,
        m.Unit,
        m.AtcCode,
        m.TherapeuticClass,
        m.Controlled,
        Status = m.Status.ToString(),
        m.Notes,
        m.CreatedOnUtc,
        m.UpdatedOnUtc,
    };
}
