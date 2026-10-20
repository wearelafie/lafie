using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Persistence.Migrations;

/// <summary>Enregistrement DI du runner de migrations (scripts embarqués dans l'assembly fourni).</summary>
public static class MigrationRegistration
{
    public static IServiceCollection AddLafieMigrations(this IServiceCollection services, Assembly migrationsAssembly)
    {
        ArgumentNullException.ThrowIfNull(migrationsAssembly);
        services.AddSingleton(sp => new MigrationRunner(
            sp.GetRequiredService<IDbConnectionFactory>(), migrationsAssembly));
        services.AddHostedService<MigrationHostedService>();
        return services;
    }
}
