using Lafie.Caching.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lafie.Caching;

internal sealed class CacheService(HybridCache cache) : ICacheService
{
    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync(
            key,
            factory,
            expiration is null ? null : new HybridCacheEntryOptions { Expiration = expiration },
            cancellationToken: cancellationToken);

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        => cache.RemoveAsync(key, cancellationToken);
}
