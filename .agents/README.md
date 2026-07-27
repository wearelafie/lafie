# .agents

Contexte et procédures pour agents IA travaillant sur Lafie. Point d'entrée : [`../AGENTS.md`](../AGENTS.md) (et [`../CLAUDE.md`](../CLAUDE.md)).

## `rules/` — conventions à respecter
- [stack.md](rules/stack.md) — **différences de packages** (Dapper≠EF, Mediator≠MediatR, …)
- [architecture.md](rules/architecture.md) — modular monolith DDD, modules, isolation
- [buildingblocks-protection.md](rules/buildingblocks-protection.md) — rôles des BuildingBlocks, interdits
- [persistence-dapper.md](rules/persistence-dapper.md) — Dapper, conventions de table, pas d'EF
- [mediator.md](rules/mediator.md) — CQRS avec Mediator (source-gen)

## `skills/` — procédures pas-à-pas
- [add-module](skills/add-module/SKILL.md) — créer un module (4 couches)
- [query-patterns-dapper](skills/query-patterns-dapper/SKILL.md) — requêtes Dapper
- [add-sql-migration](skills/add-sql-migration/SKILL.md) — évolution du schéma en SQL

## À venir
Rules : eventing, conformité/multitenancy (Finbuckle), auth (JWT+Identity/Dapper), caching (Valkey), testing, frontend.
Skills : add-feature, add-entity, add-integration-event, add-react-page, mediator-reference, testing-guide.
Workflows : architecture-guard, code-reviewer, module-creator.
