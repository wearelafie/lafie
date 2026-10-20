using System.Data;
using System.Reflection;
using Dapper;

namespace Lafie.Persistence.Migrations;

/// <summary>
/// Applique les migrations SQL versionnées (scripts <c>.sql</c> embarqués, triés par nom),
/// idempotent : chaque script appliqué une seule fois, suivi dans <c>public.schema_migrations</c>.
/// </summary>
public sealed class MigrationRunner(IDbConnectionFactory connectionFactory, Assembly migrationsAssembly)
{
    private const string ResourceMarker = ".Migrations.";

    public async Task<IReadOnlyList<string>> RunAsync(CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            create table if not exists public.schema_migrations (
                id             text        primary key,
                applied_on_utc timestamptz not null default now()
            );
            """, cancellationToken: cancellationToken));

        var applied = (await connection.QueryAsync<string>(
            new CommandDefinition("select id from public.schema_migrations", cancellationToken: cancellationToken)))
            .ToHashSet(StringComparer.Ordinal);

        var scripts = migrationsAssembly.GetManifestResourceNames()
            .Where(n => n.Contains(ResourceMarker, StringComparison.Ordinal) && n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var newlyApplied = new List<string>();

        foreach (var resource in scripts)
        {
            var id = resource[(resource.IndexOf(ResourceMarker, StringComparison.Ordinal) + ResourceMarker.Length)..];
            if (applied.Contains(id)) continue;

            var sql = await ReadResourceAsync(resource, cancellationToken);

            using var transaction = connection.BeginTransaction();
            await connection.ExecuteAsync(new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken));
            await connection.ExecuteAsync(new CommandDefinition(
                "insert into public.schema_migrations (id) values (@id)",
                new { id }, transaction, cancellationToken: cancellationToken));
            transaction.Commit();

            newlyApplied.Add(id);
        }

        return newlyApplied;
    }

    private async Task<string> ReadResourceAsync(string resourceName, CancellationToken cancellationToken)
    {
        await using var stream = migrationsAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Ressource de migration introuvable : {resourceName}");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
