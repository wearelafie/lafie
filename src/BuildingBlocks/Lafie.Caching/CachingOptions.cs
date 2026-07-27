namespace Lafie.Caching;

/// <summary>Options de cache. Valkey (compatible Redis) en L2 distribué si activé.</summary>
public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    /// <summary>Active le cache distribué Valkey/Redis (sinon L1 mémoire uniquement).</summary>
    public bool UseValkey { get; set; }

    /// <summary>Chaîne de connexion Valkey/Redis.</summary>
    public string? ConnectionString { get; set; }
}
