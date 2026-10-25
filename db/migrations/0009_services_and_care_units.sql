-- Module Organization — services / départements (hiérarchiques) et unités de soins,
-- plus l'association M:N des unités aux espaces physiques.

-- Services (hiérarchie via parent_service_id auto-référencé).
create table if not exists organization.services (
    id                uuid        primary key,
    facility_id       uuid        not null references organization.organizations (id),
    parent_service_id uuid        null references organization.services (id),
    code              text        not null,
    name              text        not null,
    category          text        not null,
    description       text        null,
    head              text        null,
    display_order     int         not null default 0,
    status            text        not null default 'Active',
    created_on_utc    timestamptz not null,
    updated_on_utc    timestamptz null,
    archived_on_utc   timestamptz null,
    constraint uq_services_facility_code unique (facility_id, code)
);

create index if not exists ix_services_facility on organization.services (facility_id);
create index if not exists ix_services_parent on organization.services (parent_service_id);
create index if not exists ix_services_status on organization.services (status);
create index if not exists ix_services_archived on organization.services (archived_on_utc);

-- Unités de soins rattachées à un service (établissement dénormalisé pour requête + unicité code/établissement).
create table if not exists organization.care_units (
    id              uuid        primary key,
    facility_id     uuid        not null references organization.organizations (id),
    service_id      uuid        not null references organization.services (id),
    code            text        not null,
    name            text        not null,
    type            text        not null,
    bed_capacity    int         null,
    description     text        null,
    display_order   int         not null default 0,
    status          text        not null default 'Active',
    created_on_utc  timestamptz not null,
    updated_on_utc  timestamptz null,
    archived_on_utc timestamptz null,
    constraint uq_care_units_facility_code unique (facility_id, code)
);

create index if not exists ix_care_units_service on organization.care_units (service_id);
create index if not exists ix_care_units_facility on organization.care_units (facility_id);
create index if not exists ix_care_units_status on organization.care_units (status);
create index if not exists ix_care_units_archived on organization.care_units (archived_on_utc);

-- Association M:N unité de soins <-> espace (suppression en cascade des deux côtés).
create table if not exists organization.care_unit_spaces (
    care_unit_id   uuid        not null references organization.care_units (id) on delete cascade,
    space_id       uuid        not null references organization.spaces (id) on delete cascade,
    created_on_utc timestamptz not null,
    primary key (care_unit_id, space_id)
);

create index if not exists ix_care_unit_spaces_space on organization.care_unit_spaces (space_id);
