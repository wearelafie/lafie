# CLAUDE.md

Instructions Claude Code pour **Lafie**. La référence complète est [`AGENTS.md`](AGENTS.md) — lis-la en premier.

## Rappels critiques (spécifiques à cet environnement)

- **Stack ≠ fullstackhero** : **Dapper** (pas EF Core), **Mediator** martinothamar (pas MediatR), pas de migrations EF (SQL brut dans `db/`), HybridCache/Valkey, Finbuckle. Détail : [`.agents/rules/stack.md`](.agents/rules/stack.md).
- **Build/exécution via Docker** (`docker compose up --build`). **Smart App Control** (Enforce) bloque les binaires locaux non signés → `dotnet run`/`dotnet test` natifs échouent souvent (`0x800711C7`) ; c'est l'environnement, pas le code. Le build (`dotnet build Lafie.slnx`) fonctionne.
- **Solution = `Lafie.slnx`** (format XML .NET 10), pas `.sln`.
- **Git** : jamais de `Co-Authored-By`/empreinte IA dans les commits ; **pas de push** sans demande explicite.
- **rtk** (Rust Token Killer) est actif globalement (hook) → les sorties de commandes sont compressées.

## Où trouver quoi

- Architecture / décisions : [`docs/architecture/`](docs/architecture/README.md) (README, decisions ADR, modules, stack).
- Conformité (Togo/UE/US/FR) : [`docs/conformite/`](docs/conformite/README.md).
- Frontend : [`docs/frontend/`](docs/frontend/README.md).
- Conventions & procédures agent : [`.agents/`](.agents/).
