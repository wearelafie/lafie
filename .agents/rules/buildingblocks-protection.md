# Règle — Protection des BuildingBlocks

Les `src/BuildingBlocks/*` sont le socle partagé. Modifier avec prudence.

## Rôles (ne pas mélanger)

- **Lafie.Core** — types domaine purs (`Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `Result`). **Aucune dépendance** (ni package technique, ni autre projet Lafie).
- **Lafie.Shared** — contrats **inter-modules** : integration events (`IIntegrationEvent`, `IEventBus`), **ports de conformité**, `Mediator.Abstractions`. Dépend de Core.
- **Lafie.Persistence** — **Dapper/Npgsql** uniquement (`IDbConnectionFactory`, `DatabaseHealth`). Pas d'EF.
- **Lafie.Eventing** — Outbox + bus in-process.
- **Lafie.Web** — contrats web (`IModule`), behaviors Mediator / ProblemDetails / OpenApi (à venir).

## Interdits

- ❌ Mettre de la **logique métier / règles nationales** dans un BuildingBlock (elles vont dans les modules / packs de conformité).
- ❌ Faire dépendre `Lafie.Core` de quoi que ce soit.
- ❌ Introduire **EF Core** / `DbContext` (on est en Dapper).
- ❌ Créer une dépendance d'un BuildingBlock vers un **module**.

## Ajout d'un BuildingBlock différé

Caching (HybridCache/Valkey), Jobs (Hangfire), Mailing, Storage (MinIO/S3), Quota : créer le projet sous `src/BuildingBlocks/Lafie.<Nom>`, l'ajouter à `Lafie.slnx`, garder les dépendances minimales, documenter dans `docs/architecture/README.md` §3.
