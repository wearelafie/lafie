using Lafie.Core.Domain;

namespace Lafie.Organization.Domain.Events;

/// <summary>Événement : un service a été créé.</summary>
public sealed record ServiceCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid ServiceId,
    Guid FacilityId,
    string Name,
    string? CorrelationId = null,
    string? TenantId = null) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
