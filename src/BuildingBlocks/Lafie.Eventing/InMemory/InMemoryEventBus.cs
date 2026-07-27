using Lafie.Eventing.Abstractions;

namespace Lafie.Eventing.InMemory;

/// <summary>Bus in-process (squelette). Abstraction identique le jour d'un broker (RabbitMq/Kafka).</summary>
public sealed class InMemoryEventBus : IEventBus
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        // TODO(phase suivante) : router vers les IIntegrationEventHandler enregistrés + Outbox.
        return Task.CompletedTask;
    }
}
