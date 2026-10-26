using Lafie.Core.Domain;

namespace Lafie.Organization.Domain.Events;

/// <summary>Événement : une unité de soins a été créée.</summary>
public sealed record CareUnitCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid CareUnitId,
    Guid FacilityId,
    Guid ServiceId,
    string Name,
    string? CorrelationId = null,
    string? TenantId = null) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
