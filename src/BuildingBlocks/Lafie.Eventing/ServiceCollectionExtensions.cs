using Lafie.Eventing.Abstractions;
using Lafie.Eventing.InMemory;
using Lafie.Eventing.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Eventing;

/// <summary>Enregistrement DI de l'eventing (bus in-process + sérialiseur ; Outbox/broker à venir).</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLafieEventing(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddSingleton<IEventSerializer, JsonEventSerializer>();
        return services;
    }
}
