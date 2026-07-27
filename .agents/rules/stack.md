# Règle — Stack & différences de packages

Lafie s'inspire de **fullstackhero/dotnet-starter-kit** pour la *structure*, mais **nos packages diffèrent**. Toujours utiliser les nôtres.

## Tableau des différences (⚠️ NE PAS confondre)

| Préoccupation | fullstackhero | **Lafie (à utiliser)** |
| --- | --- | --- |
| Accès données | EF Core (DbContext, interceptors, Ardalis.Specification) | **Dapper + Npgsql** (SQL explicite). **Aucun EF Core, aucun DbContext.** |
| Migrations | EF migrations | **SQL brut** versionné dans `db/` (init + scripts). Skill `add-sql-migration`. |
| CQRS / médiation | MediatR-like | **Mediator** (martinothamar, **source-generated**) : `Mediator.Abstractions` + `Mediator.SourceGenerator`. `AddMediator()`. |
| Cache | Redis | **HybridCache** sur **Valkey** (compatible Redis). |
| Eventing | Outbox/Inbox/RabbitMq | Outbox + **bus in-process** (`Lafie.Eventing`) ; broker plus tard. |
| Multitenancy | Finbuckle | **Finbuckle** (idem) — résout tenant → connexion/schéma Dapper. |
| Auth | JWT + ASP.NET Identity | idem, mais **Identity + Dapper** = stores custom (pas d'EF stores). |
| Storage | S3/Local | **MinIO/S3** (idem, plus tard). |
| Jobs | Hangfire | **Hangfire** (idem, plus tard). |
| FHIR | — | **Firely SDK** `Hl7.Fhir.R4` (interop santé). |

## Versions clés (backend)

.NET **10** · solution **`Lafie.slnx`** (XML) · Mediator 3.0.2 · Dapper 2.1.79 · Npgsql 10.0.3.

## Frontend

React 19 + TS + **Vite** · Fluent UI v9 · TanStack Query/Router/Table · React Hook Form + Zod · i18next · Luxon · Vitest/Playwright · vite-plugin-pwa · **@iconify-json/fluent-color** (icônes de menu, via unplugin-icons). Détail : [`docs/frontend/README.md`](../../docs/frontend/README.md).

## Patterns EF réinterprétés en Dapper

| Pattern EF | Équivalent Dapper |
| --- | --- |
| soft-delete (interceptor) | colonne `deleted_at` + filtre SQL systématique |
| audit (interceptor) | colonnes `created_at/by`, `updated_at/by` renseignées en repo/handler |
| domain events | collectés sur l'agrégat, publiés après commit via Outbox/`IEventBus` |
| specification | query objects / builders SQL |
| tenant DbContext | Finbuckle → sélection connexion/schéma Dapper |
