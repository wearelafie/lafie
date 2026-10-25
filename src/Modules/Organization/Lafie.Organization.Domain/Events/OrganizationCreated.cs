using Lafie.Core.Domain;

namespace Lafie.Organization.Domain.Events;

/// <summary>Événement : un établissement a été créé.</summary>
public sealed record OrganizationCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid OrganizationId,
    string OfficialName,
    string FacilityTypeCode,
    string? CorrelationId = null,
    string? TenantId = null) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
