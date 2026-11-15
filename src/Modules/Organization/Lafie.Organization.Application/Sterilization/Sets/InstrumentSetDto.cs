namespace Lafie.Organization.Application.Sterilization.Sets;

/// <summary>Projection lecture d'une boîte d'instruments (Dapper, par nom de colonne).</summary>
public sealed class InstrumentSetDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public int ItemCount { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? LastCycleId { get; init; }
    public DateTime? SterilizedOnUtc { get; init; }
    public DateOnly? ExpiryOn { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}

/// <summary>Affectation d'une boîte à une intervention.</summary>
public sealed class InstrumentSetUsageDto
{
    public Guid Id { get; init; }
    public Guid SetId { get; init; }
    public string InterventionLabel { get; init; } = string.Empty;
    public string? PatientRef { get; init; }
    public DateOnly? ScheduledOn { get; init; }
    public DateTime? PerformedOnUtc { get; init; }
    public string? PerformedBy { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
