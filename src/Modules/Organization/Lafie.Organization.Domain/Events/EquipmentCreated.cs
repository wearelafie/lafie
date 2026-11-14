using Lafie.Core.Domain;

namespace Lafie.Organization.Domain.Events;

/// <summary>Événement : un équipement a été enregistré.</summary>
public sealed record EquipmentCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid EquipmentId,
    Guid FacilityId,
    string Name,
    string? CorrelationId = null,
    string? TenantId = null) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
