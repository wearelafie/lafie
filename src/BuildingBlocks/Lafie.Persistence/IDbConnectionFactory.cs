using System.Data;

namespace Lafie.Persistence;

/// <summary>
/// Fournit des connexions PostgreSQL ouvertes pour les requêtes Dapper.
/// Le module consommateur écrit son SQL explicite ; pas d'ORM lourd.
/// </summary>
public interface IDbConnectionFactory
{
    Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
