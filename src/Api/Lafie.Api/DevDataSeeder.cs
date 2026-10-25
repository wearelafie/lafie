using System.Reflection;
using Dapper;
using Lafie.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lafie.Api;

/// <summary>
/// Injecte un jeu de données de développement pour le module Infrastructure
/// (établissements → bâtiments → étages → espaces). Enregistré uniquement en
/// environnement Development et exécuté après les migrations. Le script SQL est
/// idempotent : réexécuter ne duplique rien.
/// </summary>
public sealed class DevDataSeeder(IDbConnectionFactory connectionFactory, ILogger<DevDataSeeder> logger)
    : IHostedService
{
    private const string ResourceSuffix = ".Seed.dev_infrastructure.sql";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var sql = await ReadSeedScriptAsync(cancellationToken);
            var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));

            var facilities = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                "select count(*) from organization.organizations where archived_on_utc is null",
                cancellationToken: cancellationToken));
            logger.LogInformation("Seed de développement Infrastructure appliqué (établissements actifs : {Count}).", facilities);
        }
        catch (Exception ex)
        {
            // Le seed de dev ne doit jamais empêcher l'application de démarrer.
            logger.LogWarning(ex, "Seed de développement Infrastructure ignoré (erreur non bloquante).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<string> ReadSeedScriptAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(DevDataSeeder).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Script de seed introuvable (ressource *{ResourceSuffix}).");

        await using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
