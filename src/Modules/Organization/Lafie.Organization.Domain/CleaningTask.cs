using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>
/// Tâche de bio-nettoyage d'un espace (hygiène hospitalière). Une tâche active bascule l'espace
/// en <see cref="SpaceOperationalState.Cleaning"/> (orchestré par le handler), sa complétion le libère.
/// </summary>
public sealed class CleaningTask : AggregateRoot<Guid>
{
    public const int TextMaxLength = 2000;
    public const int NameMaxLength = 200;

    public Guid FacilityId { get; private set; }
    public Guid SpaceId { get; private set; }
    public CleaningProtocol Protocol { get; private set; }
    public CleaningTrigger Trigger { get; private set; }
    public CleaningStatus Status { get; private set; }
    public string? RequestedBy { get; private set; }
    public string? AssignedTo { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? StartedOnUtc { get; private set; }
    public DateTimeOffset? CompletedOnUtc { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private CleaningTask(
        Guid id, Guid facilityId, Guid spaceId, CleaningProtocol protocol, CleaningTrigger trigger, CleaningStatus status,
        string? requestedBy, string? assignedTo, string? notes, DateTimeOffset? startedOnUtc, DateTimeOffset? completedOnUtc,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        SpaceId = spaceId;
        Protocol = protocol;
        Trigger = trigger;
        Status = status;
        RequestedBy = requestedBy;
        AssignedTo = assignedTo;
        Notes = notes;
        StartedOnUtc = startedOnUtc;
        CompletedOnUtc = completedOnUtc;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<CleaningTask> Create(
        Guid facilityId, Guid spaceId, CleaningProtocol protocol, CleaningTrigger trigger, string? requestedBy, string? notes)
    {
        if (facilityId == Guid.Empty) return Result.Failure<CleaningTask>("Établissement obligatoire.");
        if (spaceId == Guid.Empty) return Result.Failure<CleaningTask>("Espace cible obligatoire.");

        var task = new CleaningTask(
            Guid.NewGuid(), facilityId, spaceId, protocol, trigger, CleaningStatus.Pending,
            Trim(requestedBy, NameMaxLength), null, Trim(notes, TextMaxLength), null, null, DateTimeOffset.UtcNow, null);
        return Result.Success(task);
    }

    public Result Assign(string assignedTo)
    {
        if (Status is CleaningStatus.Completed or CleaningStatus.Cancelled) return Result.Failure("Tâche déjà clôturée.");
        if (string.IsNullOrWhiteSpace(assignedTo)) return Result.Failure("Intervenant obligatoire.");
        AssignedTo = assignedTo.Trim();
        Touch();
        return Result.Success();
    }

    public Result Start()
    {
        if (Status is CleaningStatus.Completed or CleaningStatus.Cancelled) return Result.Failure("Tâche déjà clôturée.");
        Status = CleaningStatus.InProgress;
        StartedOnUtc ??= DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    /// <summary>Valide la tâche. <paramref name="allItemsDone"/> impose que toute la checklist soit cochée.</summary>
    public Result Complete(bool allItemsDone)
    {
        if (Status is CleaningStatus.Cancelled) return Result.Failure("Tâche annulée.");
        if (!allItemsDone) return Result.Failure("Toutes les étapes de la checklist doivent être validées avant de terminer.");
        Status = CleaningStatus.Completed;
        CompletedOnUtc ??= DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status is CleaningStatus.Completed) return Result.Failure("Tâche déjà terminée.");
        Status = CleaningStatus.Cancelled;
        Touch();
        return Result.Success();
    }

    /// <summary>Vrai tant que la tâche occupe effectivement l'espace (non terminée/annulée).</summary>
    public bool IsActive => Status is CleaningStatus.Pending or CleaningStatus.InProgress;

    public static CleaningTask Rehydrate(
        Guid id, Guid facilityId, Guid spaceId, CleaningProtocol protocol, CleaningTrigger trigger, CleaningStatus status,
        string? requestedBy, string? assignedTo, string? notes, DateTimeOffset? startedOnUtc, DateTimeOffset? completedOnUtc,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, spaceId, protocol, trigger, status, requestedBy, assignedTo, notes, startedOnUtc, completedOnUtc, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Trim(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
