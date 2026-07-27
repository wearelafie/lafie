# Règle — Architecture

- **Modular monolith** DDD hexagonal. Un seul déployable ; frontières de modules strictes (extraction en services possible plus tard).
- **Un module = 4 projets** : `Domain` (agrégats, VO, invariants, domain events ; dépend de `Lafie.Core` uniquement) · `Application` (commands/queries + handlers Mediator ; dépend de Domain + `Lafie.Shared`) · `Infrastructure` (Dapper, impl. ports ; dépend de Application + `Lafie.Persistence`/`Lafie.Eventing`) · `Api` (endpoints minimal API, DI ; dépend de Application + Infrastructure).
- **Aucun module ne référence un autre module.** Communication :
  - **Integration events** (async) via Outbox + `IEventBus` (`Lafie.Eventing`) — préféré.
  - **API publique** de module exposée dans `Lafie.Shared`.
- **Conformité multi-juridiction** : ports dans `Lafie.Shared`, adapters par pays (Togo/UE/US/FR) dans `Lafie.Interop.Infrastructure`, sélection par tenant via DI.
- **Persistance** : schema-per-module + **DB-per-région** (souveraineté). Forme domaine = source de vérité ; FHIR en JSONB pour audit/échange.
- Frontières **vérifiées** par `tests/Lafie.ArchitectureTests` (NetArchTest) : Domain n'utilise que Core ; modules indépendants ; Core isolé.

Référence complète : [`docs/architecture/README.md`](../../docs/architecture/README.md) + ADR [`decisions.md`](../../docs/architecture/decisions.md).
