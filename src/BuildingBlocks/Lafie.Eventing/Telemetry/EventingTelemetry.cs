using System.Diagnostics;

namespace Lafie.Eventing.Telemetry;

/// <summary>Source d'activité OpenTelemetry pour l'eventing.</summary>
public static class EventingTelemetry
{
    public const string ActivitySourceName = "Lafie.Eventing";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
