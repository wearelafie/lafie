using Mediator;

namespace Lafie.Core.Domain;

/// <summary>Événement de domaine. Étend INotification (Mediator) pour être publiable.</summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredOnUtc { get; }
    string? CorrelationId { get; }
    string? TenantId { get; }
}
