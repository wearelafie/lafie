using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>Étape de la checklist d'un bio-nettoyage (validée une à une).</summary>
public sealed class CleaningTaskItem : BaseEntity<Guid>
{
    public const int LabelMaxLength = 300;

    public Guid TaskId { get; private set; }
    public string Label { get; private set; }
    public bool Done { get; private set; }
    public DateTimeOffset? DoneOnUtc { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }

    private CleaningTaskItem(Guid id, Guid taskId, string label, bool done, DateTimeOffset? doneOnUtc, int sortOrder, DateTimeOffset createdOnUtc)
    {
        Id = id;
        TaskId = taskId;
        Label = label;
        Done = done;
        DoneOnUtc = doneOnUtc;
        SortOrder = sortOrder;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<CleaningTaskItem> Create(Guid taskId, string label, int sortOrder)
    {
        if (taskId == Guid.Empty) return Result.Failure<CleaningTaskItem>("Tâche obligatoire.");
        if (string.IsNullOrWhiteSpace(label)) return Result.Failure<CleaningTaskItem>("Libellé de l'étape obligatoire.");
        label = label.Trim();
        if (label.Length > LabelMaxLength) return Result.Failure<CleaningTaskItem>($"Le libellé dépasse {LabelMaxLength} caractères.");

        return Result.Success(new CleaningTaskItem(Guid.NewGuid(), taskId, label, false, null, sortOrder, DateTimeOffset.UtcNow));
    }

    public void SetDone(bool done)
    {
        Done = done;
        DoneOnUtc = done ? DateTimeOffset.UtcNow : null;
    }

    public static CleaningTaskItem Rehydrate(Guid id, Guid taskId, string label, bool done, DateTimeOffset? doneOnUtc, int sortOrder, DateTimeOffset createdOnUtc)
        => new(id, taskId, label, done, doneOnUtc, sortOrder, createdOnUtc);
}
