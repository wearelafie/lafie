-- Module Organization — établissements / services / unités / localisations.
create table if not exists organization.organizations (
    id             uuid        primary key,
    name           text        not null,
    type           text        not null,
    code           text        null,
    active         boolean     not null default true,
    parent_id      uuid        null,
    created_on_utc timestamptz not null
);

create index if not exists ix_organizations_parent on organization.organizations (parent_id);
create index if not exists ix_organizations_type on organization.organizations (type);
