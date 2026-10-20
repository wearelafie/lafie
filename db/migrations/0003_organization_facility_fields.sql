-- Enrichissement Facilities : statut (enum textuel) + champs descriptifs/contact + updated_on_utc.
alter table organization.organizations add column if not exists status text not null default 'Active';
alter table organization.organizations add column if not exists short_name text null;
alter table organization.organizations add column if not exists description text null;
alter table organization.organizations add column if not exists phone text null;
alter table organization.organizations add column if not exists email text null;
alter table organization.organizations add column if not exists timezone text not null default 'Africa/Lome';
alter table organization.organizations add column if not exists city text null;
alter table organization.organizations add column if not exists country text null;
alter table organization.organizations add column if not exists updated_on_utc timestamptz null;

-- Reprise de l'ancien booléen `active` vers `status`, puis suppression.
do $$
begin
    if exists (
        select 1 from information_schema.columns
        where table_schema = 'organization' and table_name = 'organizations' and column_name = 'active'
    ) then
        update organization.organizations set status = case when active then 'Active' else 'Inactive' end;
        alter table organization.organizations drop column active;
    end if;
end $$;

create index if not exists ix_organizations_status on organization.organizations (status);
