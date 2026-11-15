-- Module Organization — GMAO : tickets d'incident (curatif) + pièces détachées.
-- Les tickets ciblent un équipement OU un espace (cible polymorphe target_type/target_id), tous deux dans le schéma organization.
create table if not exists organization.maintenance_tickets (
    id             uuid           primary key,
    facility_id    uuid           not null references organization.organizations (id),
    target_type    text           not null,                   -- Equipment | Space
    target_id      uuid           not null,
    title          text           not null,
    description    text           null,
    criticality    text           not null default 'Medium',  -- Low | Medium | High | Critical
    blocking       boolean        not null default false,
    status         text           not null default 'Open',    -- Open | Assigned | InProgress | Resolved | Closed
    assignee_type  text           null,                       -- Internal | External
    assignee_ref   text           null,                       -- nom du technicien interne ou prestataire externe
    reported_by    text           null,
    resolution     text           null,
    downtime_start timestamptz    null,
    downtime_end   timestamptz    null,
    created_on_utc timestamptz    not null,
    updated_on_utc timestamptz    null,
    constraint ck_mtickets_target     check (target_type in ('Equipment', 'Space')),
    constraint ck_mtickets_criticality check (criticality in ('Low', 'Medium', 'High', 'Critical')),
    constraint ck_mtickets_status     check (status in ('Open', 'Assigned', 'InProgress', 'Resolved', 'Closed')),
    constraint ck_mtickets_assignee   check (assignee_type is null or assignee_type in ('Internal', 'External'))
);

create index if not exists ix_mtickets_facility on organization.maintenance_tickets (facility_id);
create index if not exists ix_mtickets_target on organization.maintenance_tickets (target_type, target_id);
create index if not exists ix_mtickets_status on organization.maintenance_tickets (status);
create index if not exists ix_mtickets_criticality on organization.maintenance_tickets (criticality);

-- Pièces détachées remplacées lors d'une intervention (traçabilité).
create table if not exists organization.maintenance_ticket_parts (
    id             uuid           primary key,
    ticket_id      uuid           not null references organization.maintenance_tickets (id) on delete cascade,
    label          text           not null,
    reference      text           null,
    quantity       int            not null default 1,
    unit_cost      numeric(14, 2) null,
    created_on_utc timestamptz    not null
);

create index if not exists ix_mticket_parts_ticket on organization.maintenance_ticket_parts (ticket_id);

-- Planification préventive : récurrence sur les interventions préventives existantes.
alter table organization.equipment_maintenances
    add column if not exists recurrence_months int  null,
    add column if not exists next_due_on       date null;

create index if not exists ix_equipment_maintenances_next_due on organization.equipment_maintenances (next_due_on);
