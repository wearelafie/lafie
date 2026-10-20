using Lafie.Core.Domain;

namespace Lafie.Organization.Domain.Events;

/// <summary>Événement : une organisation a été créée.</summary>
public sealed record OrganizationCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid OrganizationId,
    string Name,
    OrganizationType Type,
    string? CorrelationId = null,
    string? TenantId = null) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
