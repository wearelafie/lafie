-- Module Organization — Stérilisation centrale (CSSD).
-- Trois entités : boîtes/sets d'instruments (cycle de vie), cycles d'autoclave (traçabilité),
-- et affectation d'un set à une intervention (lien « chirurgie », texte libre faute de module Chirurgie).

-- Boîtes d'instruments réutilisables, avec état de cycle de vie et traçabilité de la dernière stérilisation.
create table if not exists organization.instrument_sets (
    id                uuid        primary key,
    facility_id       uuid        not null references organization.organizations (id),
    code              text        not null,
    name              text        not null,
    description       text        null,
    category          text        null,
    item_count        int         not null default 0,
    status            text        not null default 'Soiled',   -- Sterile | InUse | Soiled | InSterilization | Quarantine | Retired
    last_cycle_id     uuid        null,
    sterilized_on_utc timestamptz null,
    expiry_on         date        null,
    created_on_utc    timestamptz not null,
    updated_on_utc    timestamptz null,
    constraint ck_instr_sets_status check (status in ('Sterile', 'InUse', 'Soiled', 'InSterilization', 'Quarantine', 'Retired')),
    constraint uq_instr_sets_code unique (facility_id, code)
);

create index if not exists ix_instr_sets_facility on organization.instrument_sets (facility_id);
create index if not exists ix_instr_sets_status on organization.instrument_sets (status);
create index if not exists ix_instr_sets_expiry on organization.instrument_sets (expiry_on);

-- Cycles d'autoclave : un passage de stérilisation, avec paramètres et résultat.
create table if not exists organization.sterilization_cycles (
    id             uuid           primary key,
    facility_id    uuid           not null references organization.organizations (id),
    autoclave      text           not null,
    cycle_number   text           null,
    program        text           not null,                     -- ex: 134°C-18min prion, 121°C-20min
    status         text           not null default 'Planned',   -- Planned | Running | Passed | Failed
    operator_name  text           null,
    temperature_c  numeric(6, 2)  null,
    pressure_bar   numeric(6, 2)  null,
    duration_min   int            null,
    started_on_utc timestamptz    null,
    ended_on_utc   timestamptz    null,
    result_notes   text           null,
    created_on_utc timestamptz    not null,
    updated_on_utc timestamptz    null,
    constraint ck_steri_cycle_status check (status in ('Planned', 'Running', 'Passed', 'Failed'))
);

create index if not exists ix_steri_cycles_facility on organization.sterilization_cycles (facility_id);
create index if not exists ix_steri_cycles_status on organization.sterilization_cycles (status);

-- Charge d'un cycle : quelles boîtes y sont passées (traçabilité set <-> cycle).
create table if not exists organization.sterilization_cycle_sets (
    cycle_id uuid not null references organization.sterilization_cycles (id) on delete cascade,
    set_id   uuid not null references organization.instrument_sets (id) on delete cascade,
    primary key (cycle_id, set_id)
);

create index if not exists ix_steri_cycle_sets_set on organization.sterilization_cycle_sets (set_id);

-- Affectation d'une boîte stérile à une intervention (lien « chirurgie »).
create table if not exists organization.instrument_set_usages (
    id                 uuid        primary key,
    set_id             uuid        not null references organization.instrument_sets (id) on delete cascade,
    intervention_label text        not null,
    patient_ref        text        null,
    scheduled_on       date        null,
    performed_on_utc   timestamptz null,
    performed_by       text        null,
    status             text        not null default 'Allocated', -- Allocated | Used | Returned | Cancelled
    notes              text        null,
    created_on_utc     timestamptz not null,
    updated_on_utc     timestamptz null,
    constraint ck_set_usage_status check (status in ('Allocated', 'Used', 'Returned', 'Cancelled'))
);

create index if not exists ix_set_usages_set on organization.instrument_set_usages (set_id);
create index if not exists ix_set_usages_status on organization.instrument_set_usages (status);
