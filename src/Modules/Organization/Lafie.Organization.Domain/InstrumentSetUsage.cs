using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>
/// Affectation d'une boîte stérile à une intervention (lien « chirurgie »). Faute de module Chirurgie,
/// l'intervention est désignée en texte libre + référence patient optionnelle. Assure la traçabilité boîte ↔ acte.
/// </summary>
public sealed class InstrumentSetUsage : AggregateRoot<Guid>
{
    public const int LabelMaxLength = 300;
    public const int NameMaxLength = 200;
    public const int TextMaxLength = 1000;

    public Guid SetId { get; private set; }
    public string InterventionLabel { get; private set; }
    public string? PatientRef { get; private set; }
    public DateOnly? ScheduledOn { get; private set; }
    public DateTimeOffset? PerformedOnUtc { get; private set; }
    public string? PerformedBy { get; private set; }
    public SetUsageStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private InstrumentSetUsage(
        Guid id, Guid setId, string interventionLabel, string? patientRef, DateOnly? scheduledOn, DateTimeOffset? performedOnUtc,
        string? performedBy, SetUsageStatus status, string? notes, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        SetId = setId;
        InterventionLabel = interventionLabel;
        PatientRef = patientRef;
        ScheduledOn = scheduledOn;
        PerformedOnUtc = performedOnUtc;
        PerformedBy = performedBy;
        Status = status;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<InstrumentSetUsage> Create(
        Guid setId, string interventionLabel, string? patientRef, DateOnly? scheduledOn, string? performedBy, string? notes)
    {
        if (setId == Guid.Empty) return Result.Failure<InstrumentSetUsage>("Boîte obligatoire.");
        if (string.IsNullOrWhiteSpace(interventionLabel)) return Result.Failure<InstrumentSetUsage>("L'intervention est obligatoire.");
        interventionLabel = interventionLabel.Trim();
        if (interventionLabel.Length > LabelMaxLength) return Result.Failure<InstrumentSetUsage>($"L'intitulé dépasse {LabelMaxLength} caractères.");

        var usage = new InstrumentSetUsage(
            Guid.NewGuid(), setId, interventionLabel, Trim(patientRef, NameMaxLength), scheduledOn, null,
            Trim(performedBy, NameMaxLength), SetUsageStatus.Allocated, Trim(notes, TextMaxLength), DateTimeOffset.UtcNow, null);
        return Result.Success(usage);
    }

    public bool IsOpen => Status is SetUsageStatus.Allocated or SetUsageStatus.Used;

    public Result MarkUsed()
    {
        if (Status is not SetUsageStatus.Allocated) return Result.Failure("Seule une affectation en cours peut être marquée utilisée.");
        Status = SetUsageStatus.Used;
        PerformedOnUtc ??= DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    public Result Return()
    {
        if (Status is not (SetUsageStatus.Allocated or SetUsageStatus.Used)) return Result.Failure("Affectation déjà clôturée.");
        Status = SetUsageStatus.Returned;
        PerformedOnUtc ??= DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status is not SetUsageStatus.Allocated) return Result.Failure("Seule une affectation non utilisée peut être annulée.");
        Status = SetUsageStatus.Cancelled;
        Touch();
        return Result.Success();
    }

    public static InstrumentSetUsage Rehydrate(
        Guid id, Guid setId, string interventionLabel, string? patientRef, DateOnly? scheduledOn, DateTimeOffset? performedOnUtc,
        string? performedBy, SetUsageStatus status, string? notes, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, setId, interventionLabel, patientRef, scheduledOn, performedOnUtc, performedBy, status, notes, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Trim(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
