using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lafie.Persistence.Migrations;

/// <summary>Applique les migrations au démarrage de l'hôte, avant d'accepter le trafic.</summary>
public sealed class MigrationHostedService(MigrationRunner runner, ILogger<MigrationHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Application des migrations de base de données…");
        var applied = await runner.RunAsync(cancellationToken);
        if (applied.Count == 0)
            logger.LogInformation("Base à jour : aucune migration à appliquer.");
        else
            logger.LogInformation("Migrations appliquées ({Count}) : {Ids}", applied.Count, string.Join(", ", applied));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
