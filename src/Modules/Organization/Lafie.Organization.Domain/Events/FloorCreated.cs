using Lafie.Core.Domain;

namespace Lafie.Organization.Domain.Events;

/// <summary>Événement : un étage a été créé.</summary>
public sealed record FloorCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid FloorId,
    Guid BuildingId,
    string Name,
    string? CorrelationId = null,
    string? TenantId = null) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
