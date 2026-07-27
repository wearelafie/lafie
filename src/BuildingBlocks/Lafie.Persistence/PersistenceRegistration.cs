using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Persistence;

/// <summary>Enregistrement DI de la persistance Dapper/Npgsql partagée.</summary>
public static class PersistenceRegistration
{
    public static IServiceCollection AddLafiePersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));
        services.AddSingleton<DatabaseHealth>();
        return services;
    }
}
