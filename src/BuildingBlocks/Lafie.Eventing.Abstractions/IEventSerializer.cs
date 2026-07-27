namespace Lafie.Eventing.Abstractions;

/// <summary>Sérialise/désérialise les événements d'intégration (Outbox, broker).</summary>
public interface IEventSerializer
{
    string Serialize(IIntegrationEvent integrationEvent);
    IIntegrationEvent? Deserialize(string payload, Type type);
}
