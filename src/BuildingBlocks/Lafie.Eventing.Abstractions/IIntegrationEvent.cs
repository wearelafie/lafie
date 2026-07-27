namespace Lafie.Eventing.Abstractions;

/// <summary>Contrat public échangé ENTRE modules (via le bus / l'Outbox).</summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    string? CorrelationId { get; }
    string? TenantId { get; }
}
