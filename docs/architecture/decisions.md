# Journal des décisions d'architecture (ADR)

Décisions structurantes de Lafie. Format léger. Statut au 2026-07-21 : **validé**, aucun code écrit.

## ADR-001 — Modular monolith hexagonal

**Décision.** Un modular monolith (ports/adapters) plutôt que microservices.
**Pourquoi.** Stade cadrage : coût opérationnel des microservices injustifié. Les frontières DDD + Outbox permettent une extraction ultérieure sans réécriture.
**Conséquence.** Frontières de modules strictes, vérifiées par tests d'architecture en CI.

## ADR-002 — DDD tactique + noyau agnostique + packs de conformité

**Décision.** Noyau clinique sans aucune règle nationale ; conformité par pays via ports/adapters (inversion de dépendance).
**Pourquoi.** Cible multi-juridiction (Togo/UE/USA/France) → pas de conformité unique.
**Conséquence.** **Ports** définis dans `Lafie.Shared` (BuildingBlocks) ; **adapters par pays** dans `Lafie.Interop.Infrastructure` (`Togo/Eu/Us/Fr`), sélectionnés par tenant via DI. Le noyau ne référence jamais un pack. Voir [`../conformite/06-conformance-packs.md`](../conformite/06-conformance-packs.md).

## ADR-002b — Structure par domaine fonctionnel (Lafie.*) ✅ validé

**Décision.** Modular monolith par domaine fonctionnel : `Api` / `Web` / `Mobile` + `BuildingBlocks` (`SharedKernel`, `Shared`, `Infrastructure`) + `Modules` (Identity, Organization, Patient, Clinical, Pharmacy, Laboratory, Billing, **Terminology**, **Interop**). Nommage `Lafie.*` (cohérent avec le repo).
**Pourquoi.** Lisibilité, convention répandue (Modular Monolith / eShop). Terminology + Interop ajoutés pour couvrir l'interop santé/FHIR absente des modules métier.
**Conséquence.** Chaque module = 4 couches Domain/Application/Infrastructure/Api. Voir la structure complète dans [README.md](README.md) §2.

## ADR-003 — CQRS pragmatique (pas d'event sourcing initial) ✅ validé

**Décision.** Séparation commands/queries (**Mediator**, source-generated — cf. ADR-011). Read model = projections / requêtes SQL dédiées. **Pas d'event sourcing** au départ.
**Pourquoi.** Simplicité et vitesse de livraison ; l'ES ajoute une complexité non justifiée maintenant.
**Conséquence.** Event sourcing activable **par module** plus tard si un besoin d'audit/temporalité l'exige. Domain events dispatchés vers integration events via Outbox.

## ADR-004 — Outbox transactionnel + bus in-process

**Décision.** Integration events publiés via Outbox, consommés par un bus in-process ; abstraction prête pour un broker externe.
**Pourquoi.** Fiabilité de livraison (idempotence) sans infra broker maintenant ; migration vers RabbitMQ/Kafka non bloquante.

## ADR-005 — Persistance : schema-per-module + DB-per-région ✅ validé

**Décision.** Un schéma PostgreSQL par module (pas de FK cross-schema) ; une base **par région** (souveraineté/résidence).
**Pourquoi.** Isolation logique des modules + exigences de résidence des données (Togo local / HDS FR / HIPAA US).
**Conséquence.** Accès **Dapper** par module (voir ADR-009) ; tenant → région → connexion + pack. Pas de base unique multi-pays. Forme domaine = source de vérité ; FHIR en JSONB pour audit/échange.

## ADR-009 — Dapper (pas d'EF Core) ✅ validé

**Décision.** Persistance via **Dapper + Npgsql** (SQL explicite), **pas** d'Entity Framework Core.
**Pourquoi.** Choix explicite de l'utilisateur (2026-07-22) : contrôle du SQL, pas d'abstraction ORM lourde.
**Conséquence.** `IDbConnectionFactory` (Npgsql) dans `Lafie.Infrastructure` ; requêtes Dapper dans les `*.Infrastructure` des modules. **Pas de migrations EF** — schémas/tables en **SQL brut** (scripts `db/init` montés dans Postgres). Repositories = SQL explicite.

## ADR-010 — Environnement de run : Docker Compose (Postgres + api)

**Décision.** Développer/exécuter via **Docker Compose** : Postgres + API en conteneurs.
**Pourquoi.** Contourne **Smart App Control** (build/run Linux, DLL non bloquées), fournit la base, aligné déploiement serveur.
**Conséquence.** `docker-compose.yml` (Postgres `5433:5432`, api `8081:8080`) + `Dockerfile` multi-stage. Port hôte 5432 pris par Laragon → 5433 ; 8080 pris → 8081. Endpoints de preuve : `/health`, `/health/db` (Dapper `select 1`).

## ADR-006 — Clients fins, domaine côté serveur ✅ validé

**Décision.** Frontend = **SPA React + TypeScript (Vite)** consommant `Lafie.Api` par HTTP ; logique domaine côté serveur. (Remplace le choix initial MAUI/Blazor — voir ADR-012.)
**Pourquoi.** Une seule source de vérité métier côté serveur ; client fin découplé.
**Conséquence.** Mobile = extension future (React Native, partage de code TS, ou PWA), non retenue au départ. À rouvrir selon connectivité terrain au Togo.

## ADR-007 — FHIR R4 + IPS comme pivot ; Firely SDK

**Décision.** FHIR R4 (4.0.1) au cœur, IPS comme format d'échange pivot inter-pays, (dé)sérialisation via Firely `Hl7.Fhir.R4`.
**Pourquoi.** Standard international portable Togo/UE/USA ; IPS adopté UE + OMS.
**Conséquence.** Module Interop = anti-corruption layer domaine ↔ FHIR. Validation de profils selon le pack actif.

## ADR-008 — Premier jalon : squelette de solution seul ✅ validé

**Décision.** Livrer d'abord la structure de solution (projets, frontières, building blocks, tests d'architecture) **sans logique métier**.
**Pourquoi.** Figer les frontières avant d'écrire du domaine.
**Conséquence.** Voir la section 11 de [README.md](README.md). Création **en attente d'un go explicite**.

## ADR-012 — Frontend React/TS/Vite (remplace les clients C#) ✅ validé

**Décision.** Le frontend est une **SPA React + TypeScript (Vite)** sous `clients/lafie-navigator`. Les projets clients C# (Blazor `Lafie.Web`, RCL `Lafie.Ui`, MAUI `Lafie.Mobile`) sont **supprimés**. Le filtre `Lafie.Backend.slnf` est retiré (redondant : `Lafie.slnx` = backend + tests).
**Pourquoi.** Choix de l'utilisateur (2026-07-22). Écosystème front JS/TS plus large ; découplage total front/back ; toolchain Node indépendante de la solution .NET.
**Conséquence.** `clients/lafie-navigator` = React/TS/Vite avec son propre `Dockerfile` (build Node → **nginx**). Le front consomme l'API via **`/api`** (proxy nginx en conteneur, proxy Vite en dev → pas de CORS). `docker-compose` ajoute un service `web` (`3000:80`). L'image API reste découplée (`clients/` dans `.dockerignore`). **Mobile** : ultérieur (React Native / PWA). Remplace ADR-006 (clients MAUI/Blazor).

## ADR-011 — Stack étendue (Mediator source-gen, Finbuckle, JWT+Identity, obs.) ✅ validé

**Décision.** Adopter la stack cible détaillée dans [stack.md](stack.md) : **Mediator** (martinothamar, source-generated) pour le CQRS, **Finbuckle** pour la multitenancy, **JWT + ASP.NET Identity** pour l'auth, **HybridCache/Valkey · Hangfire · S3/MinIO**, **Serilog + OpenTelemetry**, **OpenAPI + Scalar**, **ProblemDetails RFC 9457**.
**Pourquoi.** Spec fournie par l'utilisateur (2026-07-22). **Persistance maintenue en Dapper** (la ligne EF Core de la spec est écartée — cf. ADR-009) ; patterns EF réinterprétés pour Dapper (soft-delete/audit/domain events/tenant — voir stack.md).
**Conséquence.** Mediator remplace les interfaces CQRS maison de `Lafie.Shared`. Adoption **par phases** (baseline → auth → multitenancy → cross-cutting). ⚠️ ASP.NET Identity suppose EF : en Dapper, prévoir stores custom ou JWT-only (à trancher au module Identity).

## ADR-013 — BuildingBlocks par préoccupation (inspiré fullstackhero) + dossier agent ✅ validé

**Décision.** Restructurer les BuildingBlocks en projets par préoccupation à la fullstackhero/dotnet-starter-kit, **adaptés à notre stack** : `Lafie.Core` (ex-SharedKernel), `Lafie.Shared`, `Lafie.Persistence` (Dapper), `Lafie.Eventing`, `Lafie.Web` ; différés : Caching/Jobs/Mailing/Storage/Quota. Mettre en place un dossier **`.agents/`** (rules + skills) + `AGENTS.md` + `CLAUDE.md`.
**Pourquoi.** Demande utilisateur (2026-07-24) : aligner la base sur un kit éprouvé, tout en conservant NOS choix de packages (Dapper≠EF Core, Mediator≠MediatR, pas de migrations EF, HybridCache/Valkey, Finbuckle).
**Conséquence.** Renommages : SharedKernel→Core, Infrastructure→Persistence+Eventing (split). Web ajouté (`IModule`). Tests d'archi mis à jour (préfixes Core/Persistence/Eventing/Web). Build 28 projets, 0/0. Conventions dans `.agents/rules/*` (stack, architecture, buildingblocks-protection, persistence-dapper, mediator) ; procédures `.agents/skills/*` (add-module, query-patterns-dapper, add-sql-migration).

## Stack retenue

.NET 10 · ASP.NET Core · Firely `Hl7.Fhir.R4` · **Dapper + Npgsql** · FluentValidation · Outbox/bus in-process · Docker Compose (Postgres + web) · **React + TypeScript + Vite** (frontend, nginx) · NetArchTest/ArchUnitNET · OpenTelemetry.

Solution au format **`Lafie.slnx`** (XML, défaut .NET 10). Squelette Phase 0 livré (26 projets, build 0/0). ⚠️ Gotcha : **Smart App Control (Enforce)** bloque le chargement des DLL locales non signées (`0x800711C7`) → exécution du host reportée (WSL/Docker ou désactivation SAC). Détails : [README.md](README.md) §13.

## Points à trancher plus tard

- Qualification **DM logiciel** (MDR UE + FDA US) si aide à la décision clinique → conditionne ISO 13485/14971/IEC 62304.
- Mobile (React Native / PWA) selon connectivité Togo.
- Passage éventuel à un broker externe (si extraction de modules).
- Adoption **.NET Aspire** pour l'orchestration locale (optionnel).
