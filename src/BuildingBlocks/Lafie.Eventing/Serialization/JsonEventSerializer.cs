using System.Text.Json;
using Lafie.Eventing.Abstractions;

namespace Lafie.Eventing.Serialization;

/// <summary>Sérialiseur JSON par défaut des événements d'intégration.</summary>
public sealed class JsonEventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public string Serialize(IIntegrationEvent integrationEvent)
        => JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Options);

    public IIntegrationEvent? Deserialize(string payload, Type type)
        => JsonSerializer.Deserialize(payload, type, Options) as IIntegrationEvent;
}
