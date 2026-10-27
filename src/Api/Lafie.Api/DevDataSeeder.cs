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

    // Établissements seedés par dev_infrastructure.sql (ids fixes).
    private static readonly Guid ChuId = Guid.Parse("0a000000-0000-4000-8000-000000000001");
    private static readonly Guid BiasaId = Guid.Parse("0a000000-0000-4000-8000-000000000002");
    private const string ChuName = "Centre Hospitalier Universitaire Sylvanus Olympio";
    private const string BiasaName = "Clinique Biasa";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SeedInfrastructureAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
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

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // 1) Super-admin plateforme (aucune appartenance → contexte plateforme).
            if (!await users.ExistsAsync(AdminEmail, AdminUsername, cancellationToken))
            {
                var admin = User.Create(AdminUsername, AdminEmail, hasher.Hash(AdminPassword), hasher.Algorithm,
                    UserType.Staff, firstName: "Admin", lastName: "Lafie", displayName: "Administrateur Lafie");
                await users.AddAsync(admin, cancellationToken);
                await users.AssignRoleAsync(admin.Id, "SUPER_ADMIN", cancellationToken: cancellationToken);
                logger.LogInformation("Compte admin de développement créé ({Email}).", AdminEmail);
            }

            // 2) Médecin multi-établissements (CHU par défaut + Biasa) → teste la sélection d'établissement.
            if (!await users.ExistsAsync("dr.koffi@chu-so.tg", "dr.koffi", cancellationToken))
            {
                var doc = User.Create("dr.koffi", "dr.koffi@chu-so.tg", hasher.Hash("Doctor123!"), hasher.Algorithm,
                    UserType.Practitioner, firstName: "Komlan", lastName: "Koffi", displayName: "Dr Komlan Koffi");
                await users.AddAsync(doc, cancellationToken);
                // Pas d'établissement par défaut → l'écran de sélection s'affiche à la connexion.
                await users.AddEstablishmentAsync(doc.Id, ChuId, ChuName, isDefault: false, cancellationToken);
                await users.AddEstablishmentAsync(doc.Id, BiasaId, BiasaName, isDefault: false, cancellationToken);
                await users.AssignRoleAsync(doc.Id, "DOCTOR", $"facility:{ChuId}", cancellationToken);
                await users.AssignRoleAsync(doc.Id, "DOCTOR", $"facility:{BiasaId}", cancellationToken);
                logger.LogInformation("Compte médecin de développement créé (dr.koffi@chu-so.tg).");
            }

            // 3) Admin d'établissement (CHU uniquement) → teste l'ABAC périmètre établissement.
            if (!await users.ExistsAsync("admin.chu@chu-so.tg", "admin.chu", cancellationToken))
            {
                var fadmin = User.Create("admin.chu", "admin.chu@chu-so.tg", hasher.Hash("Admin123!"), hasher.Algorithm,
                    UserType.Staff, firstName: "Afi", lastName: "Mensah", displayName: "Afi Mensah");
                await users.AddAsync(fadmin, cancellationToken);
                await users.AddEstablishmentAsync(fadmin.Id, ChuId, ChuName, isDefault: true, cancellationToken);
                await users.AssignRoleAsync(fadmin.Id, "FACILITY_ADMIN", $"facility:{ChuId}", cancellationToken);
                logger.LogInformation("Compte admin établissement de développement créé (admin.chu@chu-so.tg).");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Seed des comptes de développement ignoré (erreur non bloquante).");
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
