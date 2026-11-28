using Lafie.Core.Common;

namespace Lafie.Pharmacy.Application.Dispensing;

/// <summary>Position de prescription (ligne de liste) : en-tête + cumuls calculés.</summary>
public sealed class PrescriptionDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public Guid? PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string? PatientReference { get; init; }
    public string? PrescriberName { get; init; }
    public string? Reference { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateOnly? PrescribedOn { get; init; }
    public int LineCount { get; init; }
    public int TotalPrescribed { get; init; }
    public int TotalDispensed { get; init; }
    public string? Notes { get; init; }
}

/// <summary>Ligne de prescription (avec libellé médicament joint).</summary>
public sealed class PrescriptionLineDto
{
    public Guid Id { get; init; }
    public Guid PrescriptionId { get; init; }
    public Guid MedicationId { get; init; }
    public string MedicationCode { get; init; } = string.Empty;
    public string MedicationName { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string? Dosage { get; init; }
    public int QuantityPrescribed { get; init; }
    public int QuantityDispensed { get; init; }
}

/// <summary>Détail complet d'une prescription : en-tête + lignes.</summary>
public sealed record PrescriptionDetailDto(PrescriptionDto Header, IReadOnlyList<PrescriptionLineDto> Lines);

/// <summary>Une ligne à créer dans une prescription.</summary>
public sealed record PrescriptionLineInput(Guid MedicationId, string? Dosage, int QuantityPrescribed);

/// <summary>Critères de recherche des prescriptions d'un établissement.</summary>
public sealed record PrescriptionFilter(string? Search = null, string? Status = null, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des prescriptions et de leurs lignes (schéma `pharmacy`).</summary>
public interface IPrescriptionRepository
{
    Task AddAsync(Lafie.Pharmacy.Domain.Prescription prescription, IReadOnlyList<Lafie.Pharmacy.Domain.PrescriptionLine> lines, CancellationToken cancellationToken = default);
    Task UpdateHeaderAsync(Lafie.Pharmacy.Domain.Prescription prescription, CancellationToken cancellationToken = default);
    Task UpdateLineAsync(Lafie.Pharmacy.Domain.PrescriptionLine line, CancellationToken cancellationToken = default);

    Task<Lafie.Pharmacy.Domain.Prescription?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Lafie.Pharmacy.Domain.PrescriptionLine?> FindLineAsync(Guid lineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lafie.Pharmacy.Domain.PrescriptionLine>> GetLineAggregatesAsync(Guid prescriptionId, CancellationToken cancellationToken = default);

    Task<PrescriptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrescriptionLineDto>> GetLinesAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
    Task<PagedResult<PrescriptionDto>> SearchByFacilityAsync(Guid facilityId, PrescriptionFilter filter, CancellationToken cancellationToken = default);
}
