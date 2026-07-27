# Skill — Ajouter une « migration » SQL (Dapper, pas EF)

On n'utilise **pas** les migrations EF. Le schéma évolue par **scripts SQL versionnés** dans `db/`.

## Où

- `db/init/*.sql` — exécutés au **premier** démarrage du conteneur Postgres (montés dans `/docker-entrypoint-initdb.d`). Idéal pour les schémas + tables initiales.
- Convention de nommage : préfixe numérique ordonné, ex. `01-schemas.sql`, `02-organization.sql`, `03-patient.sql`.

## Étapes

1. Créer/éditer le script, **idempotent** :
   ```sql
   -- db/init/02-organization.sql
   create schema if not exists organization;

   create table if not exists organization.establishments (
     id          uuid primary key,
     name        text not null,
     kind        text not null,
     active      boolean not null default true,
     created_at  timestamptz not null default now(),
     created_by  text,
     updated_at  timestamptz,
     updated_by  text,
     deleted_at  timestamptz
   );
   ```
2. **Appliquer** :
   - Base neuve : `docker compose down -v && docker compose up -d` (recrée le volume → rejoue `db/init`).
   - Base existante : exécuter le script manuellement — `docker exec -i lafie-postgres psql -U lafie -d lafie < db/init/02-organization.sql` (scripts idempotents = rejouables).
3. Vérifier : `docker exec lafie-postgres psql -U lafie -d lafie -c "\dt organization.*"`.

## Règles

- Toujours `if not exists` / idempotent (les scripts peuvent être rejoués).
- Un fichier par module/domaine ; ordre par préfixe numérique.
- Colonnes d'audit + `deleted_at` (soft-delete) par convention (cf. `rules/persistence-dapper.md`).
- Évolution future : envisager un outil de migration SQL (DbUp/roundhouse) si le nombre de scripts grandit — documenter le choix.
