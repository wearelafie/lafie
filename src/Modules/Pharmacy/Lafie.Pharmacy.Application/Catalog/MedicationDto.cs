namespace Lafie.Pharmacy.Application.Catalog;

/// <summary>Projection lecture d'une fiche médicament (Dapper, par nom de colonne).</summary>
public sealed class MedicationDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Inn { get; init; }
    public string Form { get; init; } = string.Empty;
    public string? Strength { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string? AtcCode { get; init; }
    public string? TherapeuticClass { get; init; }
    public bool Controlled { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}
