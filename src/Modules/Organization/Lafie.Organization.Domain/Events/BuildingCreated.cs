using Lafie.Core.Domain;

namespace Lafie.Organization.Domain.Events;

/// <summary>Événement : un bâtiment a été créé.</summary>
public sealed record BuildingCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid BuildingId,
    Guid FacilityId,
    string Name,
    string? CorrelationId = null,
    string? TenantId = null) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
