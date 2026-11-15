-- Module Organization — état OPÉRATIONNEL runtime des espaces (distinct du statut de cycle de vie `status`).
-- status = configuration (Active/Inactive/Maintenance) ; operational_state = temps réel (disponibilité/occupation/nettoyage/blocage).
alter table organization.spaces
    add column if not exists operational_state text not null default 'Available';

alter table organization.spaces
    drop constraint if exists ck_spaces_operational_state;
alter table organization.spaces
    add constraint ck_spaces_operational_state
    check (operational_state in ('Available', 'Occupied', 'Cleaning', 'Blocked'));

create index if not exists ix_spaces_operational_state on organization.spaces (operational_state);
