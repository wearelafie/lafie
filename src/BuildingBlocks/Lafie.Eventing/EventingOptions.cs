namespace Lafie.Eventing;

/// <summary>Options d'eventing. Broker (RabbitMq) à activer plus tard ; in-process par défaut.</summary>
public sealed class EventingOptions
{
    public const string SectionName = "Eventing";
    public bool UseBroker { get; set; }
    public string? BrokerConnectionString { get; set; }
}
