-- Module Organization — espaces physiques rattachés à un étage (bâtiment/établissement dénormalisés pour requête + intégrité FK).
create table if not exists organization.spaces (
    id             uuid           primary key,
    facility_id    uuid           not null references organization.organizations (id),
    building_id    uuid           not null references organization.buildings (id),
    floor_id       uuid           not null references organization.floors (id),
    code           text           not null,
    name           text           not null,
    type           text           not null,
    description    text           null,
    area           numeric(10, 2) null,
    max_capacity   int            null,
    accessible     boolean        not null default false,
    status         text           not null default 'Active',
    created_on_utc timestamptz    not null,
    updated_on_utc timestamptz    null,
    constraint uq_spaces_facility_code unique (facility_id, code)
);

create index if not exists ix_spaces_floor on organization.spaces (floor_id);
create index if not exists ix_spaces_building on organization.spaces (building_id);
create index if not exists ix_spaces_type on organization.spaces (type);
create index if not exists ix_spaces_status on organization.spaces (status);
