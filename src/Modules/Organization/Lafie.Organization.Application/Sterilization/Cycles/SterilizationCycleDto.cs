namespace Lafie.Organization.Application.Sterilization.Cycles;

/// <summary>Projection lecture d'un cycle d'autoclave (Dapper, par nom de colonne).</summary>
public sealed class SterilizationCycleDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public string Autoclave { get; init; } = string.Empty;
    public string? CycleNumber { get; init; }
    public string Program { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Operator { get; init; }
    public decimal? TemperatureC { get; init; }
    public decimal? PressureBar { get; init; }
    public int? DurationMin { get; init; }
    public DateTime? StartedOnUtc { get; init; }
    public DateTime? EndedOnUtc { get; init; }
    public string? ResultNotes { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public int LoadCount { get; init; }
}

/// <summary>Boîte présente dans la charge d'un cycle.</summary>
public sealed class CycleSetDto
{
    public Guid SetId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
