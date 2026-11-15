-- Module Organization — Gestion des déchets d'activités de soins (DASRI & assimilés).
-- Deux entités : collectes pesées (production au point de soin) et bordereaux d'élimination
-- (regroupement pour enlèvement/transport/traitement, traçabilité réglementaire type BSD).

-- Bordereau d'élimination : regroupe des collectes pour un enlèvement vers une filière de traitement.
create table if not exists organization.waste_elimination_slips (
    id              uuid        primary key,
    facility_id     uuid        not null references organization.organizations (id),
    reference       text        not null,
    stream          text        not null,                     -- Dasri | Sharps | Cytotoxic | Anatomical | Daom | Chemical | Pharmaceutical
    status          text        not null default 'Draft',     -- Draft | Shipped | Treated | Cancelled
    carrier         text        null,                         -- transporteur / collecteur
    destination     text        null,                         -- site de traitement
    treatment       text        null,                         -- Incineration | Autoclaving | Landfill | Other
    total_weight_kg numeric(10, 3) not null default 0,        -- instantané cumulé à l'expédition
    shipped_on_utc  timestamptz null,
    treated_on_utc  timestamptz null,
    notes           text        null,
    created_on_utc  timestamptz not null,
    updated_on_utc  timestamptz null,
    constraint ck_waste_slip_stream check (stream in ('Dasri', 'Sharps', 'Cytotoxic', 'Anatomical', 'Daom', 'Chemical', 'Pharmaceutical')),
    constraint ck_waste_slip_status check (status in ('Draft', 'Shipped', 'Treated', 'Cancelled')),
    constraint ck_waste_slip_treatment check (treatment is null or treatment in ('Incineration', 'Autoclaving', 'Landfill', 'Other')),
    constraint uq_waste_slip_reference unique (facility_id, reference)
);

create index if not exists ix_waste_slips_facility on organization.waste_elimination_slips (facility_id);
create index if not exists ix_waste_slips_status on organization.waste_elimination_slips (status);

-- Collecte pesée : un enlèvement de déchets à un point de production, catégorisé et pesé.
create table if not exists organization.waste_collections (
    id              uuid        primary key,
    facility_id     uuid        not null references organization.organizations (id),
    stream          text        not null,                     -- idem bordereau
    weight_kg       numeric(10, 3) not null default 0,
    container_count int         not null default 1,
    status          text        not null default 'Collected', -- Collected | Assigned | Eliminated | Cancelled
    space_id        uuid        null references organization.spaces (id),
    slip_id         uuid        null references organization.waste_elimination_slips (id) on delete set null,
    collected_on    date        null,
    collected_by    text        null,
    notes           text        null,
    created_on_utc  timestamptz not null,
    updated_on_utc  timestamptz null,
    constraint ck_waste_coll_stream check (stream in ('Dasri', 'Sharps', 'Cytotoxic', 'Anatomical', 'Daom', 'Chemical', 'Pharmaceutical')),
    constraint ck_waste_coll_status check (status in ('Collected', 'Assigned', 'Eliminated', 'Cancelled'))
);

create index if not exists ix_waste_coll_facility on organization.waste_collections (facility_id);
create index if not exists ix_waste_coll_status on organization.waste_collections (status);
create index if not exists ix_waste_coll_slip on organization.waste_collections (slip_id);
create index if not exists ix_waste_coll_space on organization.waste_collections (space_id);
