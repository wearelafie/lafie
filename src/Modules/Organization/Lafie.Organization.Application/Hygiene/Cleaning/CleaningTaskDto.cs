namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>Projection lecture d'une tâche de bio-nettoyage (Dapper, par nom de colonne). Espace enrichi par jointure.</summary>
public sealed class CleaningTaskDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public Guid SpaceId { get; init; }
    public string? SpaceName { get; init; }
    public string? SpaceCode { get; init; }
    public string Protocol { get; init; } = string.Empty;
    public string Trigger { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? RequestedBy { get; init; }
    public string? AssignedTo { get; init; }
    public string? Notes { get; init; }
    public DateTime? StartedOnUtc { get; init; }
    public DateTime? CompletedOnUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public int ItemsTotal { get; init; }
    public int ItemsDone { get; init; }
}

/// <summary>Étape de checklist d'un bio-nettoyage.</summary>
public sealed class CleaningTaskItemDto
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public string Label { get; init; } = string.Empty;
    public bool Done { get; init; }
    public DateTime? DoneOnUtc { get; init; }
    public int SortOrder { get; init; }
}
