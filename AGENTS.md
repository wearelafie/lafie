# AGENTS.md — Lafie

Guide pour agents IA (Claude, Copilot, etc.) travaillant sur **Lafie**, plateforme open source **HIS/EMR** (gestion hospitalière + dossier médical électronique), **multi-juridiction** : Togo (principale), Europe, USA.

> Inspiré de la structure fullstackhero/dotnet-starter-kit, **adapté à notre stack** — voir [`.agents/rules/stack.md`](.agents/rules/stack.md). ⚠️ **Nos packages diffèrent** (Dapper ≠ EF Core, Mediator ≠ MediatR, pas de migrations EF).

## Architecture

- **Backend** : C# **.NET 10**, **modular monolith** en **DDD**, hexagonal (ports/adapters), **CQRS** (Mediator source-gen) + **event-driven** (Outbox + bus in-process). Détail : [`docs/architecture/`](docs/architecture/README.md).
- **Frontend** : SPA **React + TypeScript + Vite** (`clients/lafie-navigator`), Fluent UI v9. Détail : [`docs/frontend/`](docs/frontend/README.md).
- **Conformité** multi-juridiction via packs enfichables : [`docs/conformite/`](docs/conformite/README.md).

## BuildingBlocks (`src/BuildingBlocks/`)

Découpage par préoccupation : `Core` (domaine) · `Shared` (contrats + ports conformité + Mediator.Abstractions) · `Persistence` (**Dapper**) · `Eventing` (Outbox/bus) · `Web` (IModule). Différés : Caching/Jobs/Mailing/Storage/Quota. Règle : [`.agents/rules/buildingblocks-protection.md`](.agents/rules/buildingblocks-protection.md).

## Modules (`src/Modules/<M>/`)

Chaque module = 4 projets : `Domain` · `Application` · `Infrastructure` · `Api`. Aucun module ne référence un autre (communication via **integration events** Outbox ou **API publique** dans `Lafie.Shared`). Ports de conformité dans `Shared`, adapters pays dans `Interop.Infrastructure`.

## Règles clés (à respecter)

- **Persistance = Dapper** (SQL explicite). **PAS d'EF Core**, pas de `DbContext`, pas de migrations EF → schémas/tables en **SQL brut** (`db/`). Voir [`.agents/rules/persistence-dapper.md`](.agents/rules/persistence-dapper.md).
- **CQRS = Mediator** (martinothamar, source-generated). **PAS MediatR**. Voir [`.agents/rules/mediator.md`](.agents/rules/mediator.md).
- **Isolation** : Domain ne dépend que de `Lafie.Core` ; frontières vérifiées par `tests/Lafie.ArchitectureTests`.
- **Exécution** : build/run via **Docker Compose** (`docker compose up --build`) — Postgres `5433`, API `8081`, web `3000`. ⚠️ **Smart App Control** bloque les .exe/.dll locaux non signés (`0x800711C7`) → ne pas compter sur `dotnet run`/`dotnet test` natifs.
- **Git** : commits **sans empreinte IA** (pas de `Co-Authored-By`), **pas de push** sans demande explicite.

## Dossier agent

- [`.agents/rules/`](.agents/rules/) — conventions (architecture, buildingblocks, persistence-dapper, mediator, stack).
- [`.agents/skills/`](.agents/skills/) — procédures pas-à-pas (add-module, query-patterns-dapper, add-sql-migration).
