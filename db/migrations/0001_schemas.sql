-- Schéma par module (isolation logique, pas de FK cross-schema).
-- Exécuté automatiquement au premier démarrage du conteneur Postgres.

create schema if not exists organization;
create schema if not exists identity;
create schema if not exists patient;
create schema if not exists terminology;
create schema if not exists interop;
