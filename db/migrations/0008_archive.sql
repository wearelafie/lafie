-- Soft-delete : horodatage d'archivage sur établissements, bâtiments, étages, espaces.
alter table organization.organizations add column if not exists archived_on_utc timestamptz null;
alter table organization.buildings     add column if not exists archived_on_utc timestamptz null;
alter table organization.floors        add column if not exists archived_on_utc timestamptz null;
alter table organization.spaces        add column if not exists archived_on_utc timestamptz null;

create index if not exists ix_organizations_archived on organization.organizations (archived_on_utc);
create index if not exists ix_buildings_archived on organization.buildings (archived_on_utc);
create index if not exists ix_floors_archived on organization.floors (archived_on_utc);
create index if not exists ix_spaces_archived on organization.spaces (archived_on_utc);
