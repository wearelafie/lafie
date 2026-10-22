-- Enrichissement de l'établissement (HealthcareFacility) : identification, légal, adresse structurée, exploitation, statuts.

-- 1) Nouvelles colonnes (optionnelles d'abord, pour backfill).
alter table organization.organizations add column if not exists official_name text;
alter table organization.organizations add column if not exists display_name text;
alter table organization.organizations add column if not exists abbreviation text;
alter table organization.organizations add column if not exists facility_type_code text;
alter table organization.organizations add column if not exists ownership_type_code text;
alter table organization.organizations add column if not exists logo_url text;
alter table organization.organizations add column if not exists website_url text;
alter table organization.organizations add column if not exists established_on date;
alter table organization.organizations add column if not exists parent_organization_id uuid;

alter table organization.organizations add column if not exists legal_name text;
alter table organization.organizations add column if not exists legal_entity_type_code text;
alter table organization.organizations add column if not exists registration_number text;
alter table organization.organizations add column if not exists national_facility_code text;
alter table organization.organizations add column if not exists license_number text;
alter table organization.organizations add column if not exists license_issued_on date;
alter table organization.organizations add column if not exists license_expires_on date;
alter table organization.organizations add column if not exists accreditation_status_code text;
alter table organization.organizations add column if not exists tax_identification_number text;

alter table organization.organizations add column if not exists country_code text;
alter table organization.organizations add column if not exists region_code text;
alter table organization.organizations add column if not exists prefecture_code text;
alter table organization.organizations add column if not exists commune_code text;
alter table organization.organizations add column if not exists locality text;
alter table organization.organizations add column if not exists district_or_neighborhood text;
alter table organization.organizations add column if not exists street_address text;
alter table organization.organizations add column if not exists postal_code text;
alter table organization.organizations add column if not exists latitude numeric(9, 6);
alter table organization.organizations add column if not exists longitude numeric(9, 6);
alter table organization.organizations add column if not exists altitude_meters numeric(8, 2);
alter table organization.organizations add column if not exists location_accuracy text;
alter table organization.organizations add column if not exists time_zone_id text;

alter table organization.organizations add column if not exists opening_date date;
alter table organization.organizations add column if not exists default_currency_code text;
alter table organization.organizations add column if not exists default_language_code text;
alter table organization.organizations add column if not exists emergency_services_available boolean not null default false;
alter table organization.organizations add column if not exists public_facing boolean not null default false;
alter table organization.organizations add column if not exists operational_status text;
alter table organization.organizations add column if not exists configuration_status text;

-- 2) Backfill depuis l'ancien modèle (colonnes présentes si la table existait déjà).
do $$
begin
    if exists (select 1 from information_schema.columns where table_schema='organization' and table_name='organizations' and column_name='name') then
        update organization.organizations set official_name = coalesce(official_name, name);
    end if;
    if exists (select 1 from information_schema.columns where table_schema='organization' and table_name='organizations' and column_name='short_name') then
        update organization.organizations set display_name = coalesce(display_name, short_name);
    end if;
    if exists (select 1 from information_schema.columns where table_schema='organization' and table_name='organizations' and column_name='type') then
        update organization.organizations set facility_type_code = coalesce(facility_type_code, upper(type));
    end if;
    if exists (select 1 from information_schema.columns where table_schema='organization' and table_name='organizations' and column_name='city') then
        update organization.organizations set locality = coalesce(locality, city);
    end if;
    if exists (select 1 from information_schema.columns where table_schema='organization' and table_name='organizations' and column_name='country') then
        update organization.organizations set country_code = coalesce(country_code,
            case when country is not null and length(country) = 2 then upper(country) else 'TG' end);
    end if;
    if exists (select 1 from information_schema.columns where table_schema='organization' and table_name='organizations' and column_name='timezone') then
        update organization.organizations set time_zone_id = coalesce(time_zone_id, timezone);
    end if;
    if exists (select 1 from information_schema.columns where table_schema='organization' and table_name='organizations' and column_name='parent_id') then
        update organization.organizations set parent_organization_id = coalesce(parent_organization_id, parent_id);
    end if;
    if exists (select 1 from information_schema.columns where table_schema='organization' and table_name='organizations' and column_name='status') then
        update organization.organizations set operational_status = coalesce(operational_status, case status
            when 'Active' then 'Active' when 'Inactive' then 'TemporarilyClosed' when 'Draft' then 'Setup' else 'Active' end);
    end if;
end $$;

-- 3) Valeurs par défaut pour les lignes restantes, puis contraintes NOT NULL.
update organization.organizations set
    official_name = coalesce(official_name, 'Établissement'),
    facility_type_code = coalesce(facility_type_code, 'OTHER'),
    ownership_type_code = coalesce(ownership_type_code, 'PUBLIC'),
    country_code = coalesce(country_code, 'TG'),
    time_zone_id = coalesce(time_zone_id, 'Africa/Lome'),
    default_currency_code = coalesce(default_currency_code, 'XOF'),
    default_language_code = coalesce(default_language_code, 'fr'),
    operational_status = coalesce(operational_status, 'Active'),
    configuration_status = coalesce(configuration_status, 'Draft');

alter table organization.organizations alter column official_name set not null;
alter table organization.organizations alter column facility_type_code set not null;
alter table organization.organizations alter column ownership_type_code set not null;
alter table organization.organizations alter column country_code set not null;
alter table organization.organizations alter column time_zone_id set not null;
alter table organization.organizations alter column default_currency_code set not null;
alter table organization.organizations alter column default_language_code set not null;
alter table organization.organizations alter column operational_status set not null;
alter table organization.organizations alter column configuration_status set not null;

-- 4) Suppression des anciennes colonnes remplacées.
alter table organization.organizations drop column if exists name;
alter table organization.organizations drop column if exists type;
alter table organization.organizations drop column if exists status;
alter table organization.organizations drop column if exists short_name;
alter table organization.organizations drop column if exists city;
alter table organization.organizations drop column if exists country;
alter table organization.organizations drop column if exists timezone;
alter table organization.organizations drop column if exists parent_id;

-- 5) Index.
create unique index if not exists uq_organizations_code on organization.organizations (lower(code));
create index if not exists ix_organizations_facility_type on organization.organizations (facility_type_code);
create index if not exists ix_organizations_operational_status on organization.organizations (operational_status);
