-- Module Organization — bâtiments rattachés à un établissement.
create table if not exists organization.buildings (
    id             uuid        primary key,
    facility_id    uuid        not null references organization.organizations (id),
    code           text        not null,
    name           text        not null,
    description    text        null,
    location       text        null,
    status         text        not null default 'Active',
    created_on_utc timestamptz not null,
    updated_on_utc timestamptz null,
    constraint uq_buildings_facility_code unique (facility_id, code)
);

create index if not exists ix_buildings_facility on organization.buildings (facility_id);
create index if not exists ix_buildings_status on organization.buildings (status);
