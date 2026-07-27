using Dapper;

namespace Lafie.Persistence;

/// <summary>Vérifie la joignabilité de la base via une requête Dapper minimale.</summary>
public sealed class DatabaseHealth(IDbConnectionFactory connectionFactory)
{
    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var value = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition("select 1", cancellationToken: cancellationToken));
        return value == 1;
    }
}
