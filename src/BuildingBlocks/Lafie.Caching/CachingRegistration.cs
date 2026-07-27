using Lafie.Caching.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Caching;

/// <summary>Enregistrement DI du cache (HybridCache + ICacheService).</summary>
public static class CachingRegistration
{
    public static IServiceCollection AddLafieCaching(this IServiceCollection services)
    {
        // L2 Valkey (compatible Redis) à câbler via CachingOptions :
        //   services.AddStackExchangeRedisCache(o => o.Configuration = options.ConnectionString);
        // HybridCache utilise alors L1 (mémoire) + L2 (Valkey) automatiquement.
        services.AddHybridCache();
        services.AddSingleton<ICacheService, CacheService>();
        return services;
    }
}
