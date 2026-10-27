-- Appartenances multi-établissements : un utilisateur peut travailler dans plusieurs
-- établissements (= tenants). L'établissement actif est choisi à la connexion.
-- Le nom est dénormalisé (snapshot) pour éviter un couplage inter-modules au login.
create table if not exists identity.user_establishments (
    user_id        uuid        not null references identity.users (id) on delete cascade,
    facility_id    uuid        not null,
    facility_name  text        not null,
    is_default     boolean     not null default false,
    status         text        not null default 'Active',
    created_on_utc timestamptz not null,
    primary key (user_id, facility_id)
);

create index if not exists ix_user_establishments_user on identity.user_establishments (user_id);
