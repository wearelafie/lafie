namespace Lafie.Eventing.Abstractions;

/// <summary>Base immuable d'un événement d'intégration.</summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; init; }
    public string? TenantId { get; init; }
}
