# Règle — Persistance (Dapper, PAS EF Core)

## Principes

- Accès données via **Dapper + Npgsql**. SQL **explicite**. Aucun `DbContext`, aucune migration EF.
- Connexions via `IDbConnectionFactory` (`Lafie.Persistence`) : `await using var conn = await factory.OpenConnectionAsync(ct);`.
- **Schema-per-module** : chaque module écrit dans son schéma (`organization`, `patient`, …). Pas de FK cross-schema.
- **DB-per-région** (souveraineté) : le tenant (Finbuckle) détermine la connexion/région.

## Conventions de table

- Clés : `id uuid primary key`.
- Audit : `created_at timestamptz`, `created_by`, `updated_at`, `updated_by` — renseignés dans le repository/handler (pas d'interceptor).
- Soft-delete : `deleted_at timestamptz null` + **filtre systématique** `where deleted_at is null` dans les requêtes de lecture.
- `snake_case` en base ; mapping explicite ou `Dapper` `MatchNamesWithUnderscores = true`.

## Repository (exemple d'agrégat)

```csharp
internal sealed class OrganizationRepository(IDbConnectionFactory factory)
{
    public async Task<Guid> AddAsync(Establishment e, CancellationToken ct)
    {
        using var conn = await factory.OpenConnectionAsync(ct);
        const string sql = """
            insert into organization.establishments (id, name, kind, active, created_at)
            values (@Id, @Name, @Kind, @Active, now());
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql, e, cancellationToken: ct));
        return e.Id;
    }
}
```

## Migrations / schéma

- **Pas d'EF migrations.** Schémas + tables en **SQL brut** dans `db/` (monté dans Postgres, ou runner idempotent `create table if not exists`). Voir skill [`add-sql-migration`](../skills/add-sql-migration/SKILL.md).
- Repositories = SQL explicite ; specifications = query objects/builders SQL si besoin.
