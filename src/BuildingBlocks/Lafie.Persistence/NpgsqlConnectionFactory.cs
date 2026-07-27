using System.Data;
using Npgsql;

namespace Lafie.Persistence;

/// <summary>Fabrique de connexions Npgsql (une connexion neuve par appel, à disposer par l'appelant).</summary>
public sealed class NpgsqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
