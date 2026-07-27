# Skill — Ajouter un module

Créer un nouveau module métier (4 couches) dans `src/Modules/<M>/`, aligné sur l'architecture.

## Étapes

1. **Projets** (net10.0) :
   ```bash
   M=Scheduling   # exemple
   B=src/Modules/$M
   for l in Domain Application Infrastructure Api; do
     dotnet new classlib -n Lafie.$M.$l -o $B/Lafie.$M.$l
   done
   ```
2. **Références (respecter l'hexagonal)** :
   ```bash
   dotnet add $B/Lafie.$M.Domain         reference src/BuildingBlocks/Lafie.Core
   dotnet add $B/Lafie.$M.Application     reference $B/Lafie.$M.Domain src/BuildingBlocks/Lafie.Shared
   dotnet add $B/Lafie.$M.Infrastructure  reference $B/Lafie.$M.Application src/BuildingBlocks/Lafie.Persistence src/BuildingBlocks/Lafie.Eventing
   dotnet add $B/Lafie.$M.Api             reference $B/Lafie.$M.Application $B/Lafie.$M.Infrastructure src/BuildingBlocks/Lafie.Web
   dotnet add $B/Lafie.$M.Api             package Microsoft.Extensions.DependencyInjection.Abstractions
   ```
3. **Enregistrement DI** : dans `Lafie.$M.Api`, `public static class $M{Module}` avec `Add{M}Module(IServiceCollection)` (registre DbContext-less : repos Dapper, handlers). Le host appelle `Add{M}Module()`.
4. **Endpoints** : `MapGroup("/<pluriel>")` via minimal API (ou `IModule` de `Lafie.Web`).
5. **Solution** : `dotnet sln Lafie.slnx add $(find $B -name '*.csproj')`.
6. **Host** : ajouter la ProjectReference vers `Lafie.$M.Api` + l'appel `Add{M}Module()` dans `Program.cs`.
7. **Schéma DB** : ajouter le schéma dans `db/init` (cf. skill `add-sql-migration`).
8. **Vérifier** : `dotnet build Lafie.slnx` (0/0). Les tests d'archi doivent rester verts (module indépendant, Domain→Core seul).

## Rappels

- Aucune référence vers un autre **module** (events/API publique via `Lafie.Shared`).
- Dapper (pas EF), Mediator (pas MediatR).
