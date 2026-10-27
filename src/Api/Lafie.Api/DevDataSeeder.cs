using System.Reflection;
using Dapper;
using Lafie.Identity.Application.Security;
using Lafie.Identity.Application.Users;
using Lafie.Identity.Domain;
using Lafie.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lafie.Api;

/// <summary>
/// Jeu de données de DÉVELOPPEMENT : module Infrastructure (établissements → … → espaces)
/// et un compte administrateur de dev. Enregistré uniquement en environnement Development,
/// exécuté après les migrations. Idempotent : réexécuter ne duplique rien.
/// </summary>
public sealed class DevDataSeeder(
    IDbConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<DevDataSeeder> logger) : IHostedService
{
    private const string ResourceSuffix = ".Seed.dev_infrastructure.sql";
    private const string AdminEmail = "admin@lafie.local";
    private const string AdminUsername = "admin";
    private const string AdminPassword = "Admin123!"; // DEV uniquement.

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SeedInfrastructureAsync(cancellationToken);
        await SeedAdminAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedInfrastructureAsync(CancellationToken cancellationToken)
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
            logger.LogWarning(ex, "Seed de développement Infrastructure ignoré (erreur non bloquante).");
        }
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            if (await users.ExistsAsync(AdminEmail, AdminUsername, cancellationToken))
            {
                logger.LogInformation("Compte admin de développement déjà présent.");
                return;
            }

            var admin = User.Create(
                AdminUsername, AdminEmail, hasher.Hash(AdminPassword), hasher.Algorithm, UserType.Staff,
                firstName: "Admin", lastName: "Lafie", displayName: "Administrateur Lafie");
            await users.AddAsync(admin, cancellationToken);
            await users.AssignRoleAsync(admin.Id, "SUPER_ADMIN", cancellationToken);

            logger.LogInformation("Compte admin de développement créé ({Email}).", AdminEmail);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Seed du compte admin de développement ignoré (erreur non bloquante).");
        }
    }

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
