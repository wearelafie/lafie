namespace Lafie.Caching.Abstractions;

/// <summary>Abstraction de cache (au-dessus de HybridCache : L1 mémoire + L2 Valkey optionnel).</summary>
public interface ICacheService
{
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}
