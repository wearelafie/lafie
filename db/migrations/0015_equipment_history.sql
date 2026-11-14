-- Module Organization — Phase 2 des équipements : historique des déplacements et
-- maintenance (préventive / corrective, incluant les réparations).

-- Déplacements (journal append-only ; de/vers un espace de l'établissement).
create table if not exists organization.equipment_movements (
    id             uuid        primary key,
    equipment_id   uuid        not null references organization.equipment (id) on delete cascade,
    moved_on       date        not null,
    from_space_id  uuid        null references organization.spaces (id) on delete set null,
    to_space_id    uuid        null references organization.spaces (id) on delete set null,
    reason         text        null,
    performed_by   text        null,
    notes          text        null,
    created_on_utc timestamptz not null
);

create index if not exists ix_equipment_movements_equipment on organization.equipment_movements (equipment_id);
create index if not exists ix_equipment_movements_moved_on on organization.equipment_movements (moved_on);

-- Maintenance (préventive / corrective) et réparations.
create table if not exists organization.equipment_maintenances (
    id             uuid           primary key,
    equipment_id   uuid           not null references organization.equipment (id) on delete cascade,
    type           text           not null,                    -- Preventive | Corrective
    status         text           not null default 'Planned',  -- Planned | InProgress | Completed | Cancelled
    scheduled_on   date           null,
    performed_on   date           null,
    provider       text           null,
    cost           numeric(14, 2) null,
    description    text           not null,
    notes          text           null,
    created_on_utc timestamptz    not null,
    updated_on_utc timestamptz    null
);

create index if not exists ix_equipment_maintenances_equipment on organization.equipment_maintenances (equipment_id);
create index if not exists ix_equipment_maintenances_status on organization.equipment_maintenances (status);
create index if not exists ix_equipment_maintenances_type on organization.equipment_maintenances (type);
create index if not exists ix_equipment_maintenances_scheduled on organization.equipment_maintenances (scheduled_on);
