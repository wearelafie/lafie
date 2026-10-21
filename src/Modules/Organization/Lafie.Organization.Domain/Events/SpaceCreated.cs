using Lafie.Core.Domain;

namespace Lafie.Organization.Domain.Events;

/// <summary>Événement : un espace a été créé.</summary>
public sealed record SpaceCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid SpaceId,
    Guid FloorId,
    SpaceType Type,
    string Name,
    string? CorrelationId = null,
    string? TenantId = null) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
