# Skill — Patterns de requêtes Dapper

Écrire des accès données Dapper cohérents (lecture/écriture), sans EF.

## Connexion

```csharp
using var conn = await connectionFactory.OpenConnectionAsync(ct);
```

## Lecture (query) — filtrer soft-delete

```csharp
const string sql = """
    select id, name, kind, active
    from organization.establishments
    where deleted_at is null
    order by name
    limit @Take offset @Skip;
    """;
var rows = await conn.QueryAsync<EstablishmentDto>(
    new CommandDefinition(sql, new { Take = size, Skip = (page - 1) * size }, cancellationToken: ct));
```

## Écriture (command)

```csharp
const string sql = """
    insert into organization.establishments (id, name, kind, active, created_at, created_by)
    values (@Id, @Name, @Kind, @Active, now(), @CreatedBy);
    """;
await conn.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: ct));
```

## Pagination

Retourner un `PagedList<T>` : un `count(*) where deleted_at is null` + la page. Éviter `select *`.

## Règles

- **Toujours** `CommandDefinition(..., cancellationToken: ct)`.
- **Toujours** filtrer `deleted_at is null` en lecture.
- Renseigner `created_*/updated_*` explicitement (pas d'interceptor).
- `snake_case` en base ; activer `DefaultTypeMap.MatchNamesWithUnderscores = true` au démarrage, ou aliaser.
- Paramètres nommés (`@X`) — **jamais** de concaténation SQL (injection).
- Multi-tenant : la connexion (région/schéma) est résolue en amont (Finbuckle) — ne pas coder le tenant en dur.
