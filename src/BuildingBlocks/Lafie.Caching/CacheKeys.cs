namespace Lafie.Caching;

/// <summary>Fabrique de clés de cache cohérentes.</summary>
public static class CacheKeys
{
    public static string ForEntity(string entity, object id) => $"{entity}:{id}";
    public static string ForList(string entity, string qualifier) => $"{entity}:list:{qualifier}";
}
