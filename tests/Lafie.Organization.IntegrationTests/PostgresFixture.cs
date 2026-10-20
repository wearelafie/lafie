using System.Reflection;
using Lafie.Persistence;
using Lafie.Persistence.Migrations;
using Testcontainers.PostgreSql;

namespace Lafie.Organization.IntegrationTests;

/// <summary>
/// Démarre un vrai PostgreSQL (Testcontainers), applique les migrations réelles via le MigrationRunner,
/// et expose une fabrique de connexions pour les tests d'intégration du repository.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("lafie")
        .WithUsername("lafie")
        .WithPassword("lafie")
        .Build();

    public IDbConnectionFactory ConnectionFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionFactory = new NpgsqlConnectionFactory(_container.GetConnectionString());
        var runner = new MigrationRunner(ConnectionFactory, typeof(PostgresFixture).Assembly);
        await runner.RunAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
