namespace Lafie.Eventing.Abstractions;

/// <summary>Handler d'un événement d'intégration.</summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
