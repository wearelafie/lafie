using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>
/// Ticket de maintenance curative (GMAO) : incident déclaré sur un équipement ou un espace.
/// Un ticket <see cref="Blocking"/> ciblant un espace fait basculer l'espace en Blocked (orchestré par le handler).
/// </summary>
public sealed class MaintenanceTicket : AggregateRoot<Guid>
{
    public const int TitleMaxLength = 200;
    public const int TextMaxLength = 2000;

    public Guid FacilityId { get; private set; }
    public TicketTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TicketCriticality Criticality { get; private set; }
    public bool Blocking { get; private set; }
    public TicketStatus Status { get; private set; }
    public TicketAssigneeType? AssigneeType { get; private set; }
    public string? AssigneeRef { get; private set; }
    public string? ReportedBy { get; private set; }
    public string? Resolution { get; private set; }
    public DateTimeOffset? DowntimeStart { get; private set; }
    public DateTimeOffset? DowntimeEnd { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private MaintenanceTicket(
        Guid id, Guid facilityId, TicketTargetType targetType, Guid targetId, string title, string? description,
        TicketCriticality criticality, bool blocking, TicketStatus status, TicketAssigneeType? assigneeType, string? assigneeRef,
        string? reportedBy, string? resolution, DateTimeOffset? downtimeStart, DateTimeOffset? downtimeEnd,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        TargetType = targetType;
        TargetId = targetId;
        Title = title;
        Description = description;
        Criticality = criticality;
        Blocking = blocking;
        Status = status;
        AssigneeType = assigneeType;
        AssigneeRef = assigneeRef;
        ReportedBy = reportedBy;
        Resolution = resolution;
        DowntimeStart = downtimeStart;
        DowntimeEnd = downtimeEnd;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<MaintenanceTicket> Create(
        Guid facilityId, TicketTargetType targetType, Guid targetId, string title, string? description,
        TicketCriticality criticality, bool blocking, string? reportedBy)
    {
        if (facilityId == Guid.Empty) return Result.Failure<MaintenanceTicket>("Établissement obligatoire.");
        if (targetId == Guid.Empty) return Result.Failure<MaintenanceTicket>("Cible du ticket obligatoire.");
        var vTitle = NormalizeTitle(title);
        if (vTitle.IsFailure) return Result.Failure<MaintenanceTicket>(vTitle.Error!);

        var ticket = new MaintenanceTicket(
            Guid.NewGuid(), facilityId, targetType, targetId, vTitle.Value, Trim(description, TextMaxLength),
            criticality, blocking, TicketStatus.Open, null, null, Trim(reportedBy, TitleMaxLength), null,
            null, null, DateTimeOffset.UtcNow, null);
        return Result.Success(ticket);
    }

    public Result Edit(string title, string? description, TicketCriticality criticality, bool blocking)
    {
        var vTitle = NormalizeTitle(title);
        if (vTitle.IsFailure) return vTitle;
        Title = vTitle.Value;
        Description = Trim(description, TextMaxLength);
        Criticality = criticality;
        Blocking = blocking;
        Touch();
        return Result.Success();
    }

    public Result Assign(TicketAssigneeType assigneeType, string assigneeRef)
    {
        if (Status is TicketStatus.Closed) return Result.Failure("Ticket clôturé.");
        if (string.IsNullOrWhiteSpace(assigneeRef)) return Result.Failure("Intervenant obligatoire.");
        AssigneeType = assigneeType;
        AssigneeRef = assigneeRef.Trim();
        if (Status is TicketStatus.Open) Status = TicketStatus.Assigned;
        Touch();
        return Result.Success();
    }

    public Result Start()
    {
        if (Status is TicketStatus.Resolved or TicketStatus.Closed) return Result.Failure("Ticket déjà traité.");
        Status = TicketStatus.InProgress;
        DowntimeStart ??= DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    public Result Resolve(string? resolution)
    {
        if (Status is TicketStatus.Closed) return Result.Failure("Ticket clôturé.");
        Status = TicketStatus.Resolved;
        Resolution = Trim(resolution, TextMaxLength);
        DowntimeEnd ??= DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    public Result Close()
    {
        if (Status is TicketStatus.Open or TicketStatus.Assigned)
            return Result.Failure("Un ticket doit être résolu avant clôture.");
        Status = TicketStatus.Closed;
        Touch();
        return Result.Success();
    }

    /// <summary>Vrai tant que le ticket bloque effectivement sa cible (bloquant et non résolu/clôturé).</summary>
    public bool IsActivelyBlocking => Blocking && Status is not (TicketStatus.Resolved or TicketStatus.Closed);

    public static MaintenanceTicket Rehydrate(
        Guid id, Guid facilityId, TicketTargetType targetType, Guid targetId, string title, string? description,
        TicketCriticality criticality, bool blocking, TicketStatus status, TicketAssigneeType? assigneeType, string? assigneeRef,
        string? reportedBy, string? resolution, DateTimeOffset? downtimeStart, DateTimeOffset? downtimeEnd,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, targetType, targetId, title, description, criticality, blocking, status, assigneeType, assigneeRef,
            reportedBy, resolution, downtimeStart, downtimeEnd, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static Result<string> NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return Result.Failure<string>("Le titre du ticket est obligatoire.");
        title = title.Trim();
        return title.Length > TitleMaxLength
            ? Result.Failure<string>($"Le titre dépasse {TitleMaxLength} caractères.")
            : Result.Success(title);
    }

    private static string? Trim(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
