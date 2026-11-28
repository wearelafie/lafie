using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Pharmacy.Application.Dispensing;
using Lafie.Pharmacy.Domain;
using Lafie.Persistence;

namespace Lafie.Pharmacy.Infrastructure;

/// <summary>Persistance Dapper des prescriptions et de leurs lignes (schéma `pharmacy`).</summary>
public sealed class PrescriptionRepository(IDbConnectionFactory connectionFactory) : IPrescriptionRepository
{
    public async Task AddAsync(Prescription prescription, IReadOnlyList<PrescriptionLine> lines, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var tx = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition(InsertHeaderSql, HeaderParams(prescription), tx, cancellationToken: cancellationToken));
        foreach (var line in lines)
            await connection.ExecuteAsync(new CommandDefinition(InsertLineSql, LineParams(line), tx, cancellationToken: cancellationToken));
        tx.Commit();
    }

    public async Task UpdateHeaderAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        const string sql = "update pharmacy.prescriptions set status = @Status, updated_on_utc = @UpdatedOnUtc where id = @Id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, HeaderParams(prescription), cancellationToken: cancellationToken));
    }

    public async Task UpdateLineAsync(PrescriptionLine line, CancellationToken cancellationToken = default)
    {
        const string sql = "update pharmacy.prescription_lines set quantity_dispensed = @QuantityDispensed, updated_on_utc = @UpdatedOnUtc where id = @Id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, LineParams(line), cancellationToken: cancellationToken));
    }

    public async Task<Prescription?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", facility_id as "FacilityId", patient_id as "PatientId", patient_name as "PatientName",
                   patient_reference as "PatientReference", prescriber_name as "PrescriberName", reference as "Reference",
                   status as "Status", prescribed_on as "PrescribedOn", notes as "Notes",
                   created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
            from pharmacy.prescriptions where id = @id
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var r = await connection.QuerySingleOrDefaultAsync<HeaderRow>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
        return r is null ? null : Prescription.Rehydrate(
            r.Id, r.FacilityId, r.PatientId, r.PatientName, r.PatientReference, r.PrescriberName, r.Reference,
            Enum.Parse<PrescriptionStatus>(r.Status), r.PrescribedOn, r.Notes,
            new DateTimeOffset(r.CreatedOnUtc, TimeSpan.Zero),
            r.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
    }

    public async Task<PrescriptionLine?> FindLineAsync(Guid lineId, CancellationToken cancellationToken = default)
    {
        var r = await QueryLineRowsAsync("where id = @lineId", new { lineId }, cancellationToken);
        return r.Count == 0 ? null : ToAggregate(r[0]);
    }

    public async Task<IReadOnlyList<PrescriptionLine>> GetLineAggregatesAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryLineRowsAsync("where prescription_id = @prescriptionId", new { prescriptionId }, cancellationToken);
        return rows.Select(ToAggregate).ToList();
    }

    public async Task<PrescriptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"{HeaderProjectionSql} where p.id = @id group by p.id";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<PrescriptionDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<PrescriptionLineDto>> GetLinesAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select l.id as "Id", l.prescription_id as "PrescriptionId", l.medication_id as "MedicationId",
                   m.code as "MedicationCode", m.name as "MedicationName", m.unit as "Unit", l.dosage as "Dosage",
                   l.quantity_prescribed as "QuantityPrescribed", l.quantity_dispensed as "QuantityDispensed"
            from pharmacy.prescription_lines l
            join pharmacy.medications m on m.id = l.medication_id
            where l.prescription_id = @prescriptionId
            order by m.name
            """;
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<PrescriptionLineDto>(
            new CommandDefinition(sql, new { prescriptionId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<PagedResult<PrescriptionDto>> SearchByFacilityAsync(Guid facilityId, PrescriptionFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where p.facility_id = @facilityId");
        var dp = new DynamicParameters();
        dp.Add("facilityId", facilityId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (p.patient_name ilike @search or p.patient_reference ilike @search or p.reference ilike @search)");
            dp.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            where.Append(" and p.status = @status");
            dp.Add("status", filter.Status);
        }

        dp.Add("limit", filter.SafePageSize);
        dp.Add("offset", filter.Offset);

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from pharmacy.prescriptions p {where}", dp, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<PrescriptionDto>(new CommandDefinition(
            $"{HeaderProjectionSql} {where} group by p.id order by p.created_on_utc desc limit @limit offset @offset",
            dp, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<PrescriptionDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    // --- SQL partagé ---

    private const string InsertHeaderSql = """
        insert into pharmacy.prescriptions
            (id, facility_id, patient_id, patient_name, patient_reference, prescriber_name, reference, status,
             prescribed_on, notes, created_on_utc, updated_on_utc)
        values
            (@Id, @FacilityId, @PatientId, @PatientName, @PatientReference, @PrescriberName, @Reference, @Status,
             @PrescribedOn, @Notes, @CreatedOnUtc, @UpdatedOnUtc)
        """;

    private const string InsertLineSql = """
        insert into pharmacy.prescription_lines
            (id, prescription_id, medication_id, dosage, quantity_prescribed, quantity_dispensed, created_on_utc, updated_on_utc)
        values
            (@Id, @PrescriptionId, @MedicationId, @Dosage, @QuantityPrescribed, @QuantityDispensed, @CreatedOnUtc, @UpdatedOnUtc)
        """;

    // En-tête + agrégats des lignes (nb, total prescrit/dispensé). group by ajouté par l'appelant.
    private const string HeaderProjectionSql = """
        select p.id as "Id", p.facility_id as "FacilityId", p.patient_id as "PatientId", p.patient_name as "PatientName",
               p.patient_reference as "PatientReference", p.prescriber_name as "PrescriberName", p.reference as "Reference",
               p.status as "Status", p.prescribed_on as "PrescribedOn", p.notes as "Notes",
               count(l.id)::int as "LineCount",
               coalesce(sum(l.quantity_prescribed), 0)::int as "TotalPrescribed",
               coalesce(sum(l.quantity_dispensed), 0)::int as "TotalDispensed"
        from pharmacy.prescriptions p
        left join pharmacy.prescription_lines l on l.prescription_id = p.id
        """;

    private const string LineColumns = """
        id as "Id", prescription_id as "PrescriptionId", medication_id as "MedicationId", dosage as "Dosage",
        quantity_prescribed as "QuantityPrescribed", quantity_dispensed as "QuantityDispensed",
        created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
        """;

    private async Task<IReadOnlyList<LineRow>> QueryLineRowsAsync(string whereClause, object parameters, CancellationToken cancellationToken)
    {
        var sql = $"select {LineColumns} from pharmacy.prescription_lines {whereClause}";
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<LineRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();
    }

    private static PrescriptionLine ToAggregate(LineRow r) => PrescriptionLine.Rehydrate(
        r.Id, r.PrescriptionId, r.MedicationId, r.Dosage, r.QuantityPrescribed, r.QuantityDispensed,
        new DateTimeOffset(r.CreatedOnUtc, TimeSpan.Zero),
        r.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);

    private static object HeaderParams(Prescription p) => new
    {
        p.Id, p.FacilityId, p.PatientId, p.PatientName, p.PatientReference, p.PrescriberName, p.Reference,
        Status = p.Status.ToString(), p.PrescribedOn, p.Notes, p.CreatedOnUtc, p.UpdatedOnUtc,
    };

    private static object LineParams(PrescriptionLine l) => new
    {
        l.Id, l.PrescriptionId, l.MedicationId, l.Dosage, l.QuantityPrescribed, l.QuantityDispensed,
        l.CreatedOnUtc, l.UpdatedOnUtc,
    };

    private sealed record HeaderRow(
        Guid Id, Guid FacilityId, Guid? PatientId, string PatientName, string? PatientReference,
        string? PrescriberName, string? Reference, string Status, DateOnly? PrescribedOn, string? Notes,
        DateTime CreatedOnUtc, DateTime? UpdatedOnUtc);

    private sealed record LineRow(
        Guid Id, Guid PrescriptionId, Guid MedicationId, string? Dosage, int QuantityPrescribed,
        int QuantityDispensed, DateTime CreatedOnUtc, DateTime? UpdatedOnUtc);
}
