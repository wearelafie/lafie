-- Module Organization — étages rattachés à un bâtiment.
create table if not exists organization.floors (
    id             uuid        primary key,
    building_id    uuid        not null references organization.buildings (id),
    code           text        not null,
    name           text        not null,
    level          int         null,
    display_order  int         not null default 0,
    description    text        null,
    status         text        not null default 'Active',
    created_on_utc timestamptz not null,
    updated_on_utc timestamptz null,
    constraint uq_floors_building_code unique (building_id, code)
);

create index if not exists ix_floors_building on organization.floors (building_id);
create index if not exists ix_floors_status on organization.floors (status);
