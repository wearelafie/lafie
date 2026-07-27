namespace Lafie.Eventing.Abstractions;

/// <summary>Publie des événements d'intégration (in-process au départ ; broker-ready).</summary>
public interface IEventBus
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
