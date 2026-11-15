-- Module Organization — Hygiène hospitalière & bio-nettoyage.
-- Tâches de nettoyage ciblant un espace, avec protocole adapté au risque et checklist à valider.
-- La tâche active bascule l'espace en Cleaning (orchestré par le handler), sauf si l'espace est Blocked (maintenance prioritaire).
create table if not exists organization.cleaning_tasks (
    id              uuid        primary key,
    facility_id     uuid        not null references organization.organizations (id),
    space_id        uuid        not null references organization.spaces (id),
    protocol        text        not null default 'Standard',  -- Standard | Deep | Terminal
    trigger         text        not null default 'OnDemand',  -- PostDischarge | Scheduled | OnDemand
    status          text        not null default 'Pending',   -- Pending | InProgress | Completed | Cancelled
    requested_by    text        null,
    assigned_to     text        null,
    notes           text        null,
    started_on_utc  timestamptz null,
    completed_on_utc timestamptz null,
    created_on_utc  timestamptz not null,
    updated_on_utc  timestamptz null,
    constraint ck_cleaning_protocol check (protocol in ('Standard', 'Deep', 'Terminal')),
    constraint ck_cleaning_trigger  check (trigger in ('PostDischarge', 'Scheduled', 'OnDemand')),
    constraint ck_cleaning_status   check (status in ('Pending', 'InProgress', 'Completed', 'Cancelled'))
);

create index if not exists ix_cleaning_facility on organization.cleaning_tasks (facility_id);
create index if not exists ix_cleaning_space on organization.cleaning_tasks (space_id);
create index if not exists ix_cleaning_status on organization.cleaning_tasks (status);

-- Étapes de la checklist du protocole (semées à la création, validées une à une).
create table if not exists organization.cleaning_task_items (
    id             uuid        primary key,
    task_id        uuid        not null references organization.cleaning_tasks (id) on delete cascade,
    label          text        not null,
    done           boolean     not null default false,
    done_on_utc    timestamptz null,
    sort_order     int         not null default 0,
    created_on_utc timestamptz not null
);

create index if not exists ix_cleaning_items_task on organization.cleaning_task_items (task_id);
