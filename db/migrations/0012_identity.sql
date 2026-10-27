-- Module Identity : comptes, professionnels de santé, affectations, RBAC (rôles/permissions).
create schema if not exists identity;

-- Comptes utilisateurs (authentification/autorisation).
create table if not exists identity.users (
    id                       uuid        primary key,
    tenant_id                text        null,
    username                 text        not null,
    email                    text        not null,
    email_verified           boolean     not null default false,
    phone                    text        null,
    phone_verified           boolean     not null default false,
    password_hash            text        null,
    password_algo            text        null,
    password_changed_on_utc  timestamptz null,
    must_change_password     boolean     not null default false,
    auth_provider            text        not null default 'Local',
    external_issuer          text        null,
    external_subject         text        null,
    mfa_enabled              boolean     not null default false,
    mfa_type                 text        null,
    user_type                text        not null default 'Staff',
    status                   text        not null default 'Active',
    failed_login_count       int         not null default 0,
    locked_until_utc         timestamptz null,
    last_login_on_utc        timestamptz null,
    last_login_ip            text        null,
    first_name               text        null,
    last_name                text        null,
    title                    text        null,
    display_name             text        null,
    avatar_url               text        null,
    preferred_language       text        null,
    time_zone                text        null,
    practitioner_id          uuid        null,
    employee_id              uuid        null,
    created_on_utc           timestamptz not null,
    updated_on_utc           timestamptz null,
    archived_on_utc          timestamptz null
);
create unique index if not exists uq_users_email on identity.users (lower(email));
create unique index if not exists uq_users_username on identity.users (lower(username));

-- Professionnels de santé (FHIR Practitioner).
create table if not exists identity.practitioners (
    id                 uuid        primary key,
    tenant_id          text        null,
    family             text        not null,
    given              text        not null,
    title              text        null,
    gender             text        null,
    birth_date         date        null,
    professional_email text        null,
    professional_phone text        null,
    photo_url          text        null,
    status             text        not null default 'Active',
    created_on_utc     timestamptz not null,
    updated_on_utc     timestamptz null,
    archived_on_utc    timestamptz null
);

create table if not exists identity.practitioner_identifiers (
    id             uuid primary key,
    practitioner_id uuid not null references identity.practitioners (id) on delete cascade,
    system         text not null,
    value          text not null,
    period_start   date null,
    period_end     date null
);
create index if not exists ix_pract_identifiers_pract on identity.practitioner_identifiers (practitioner_id);

create table if not exists identity.practitioner_qualifications (
    id             uuid primary key,
    practitioner_id uuid not null references identity.practitioners (id) on delete cascade,
    code           text not null,
    issuer         text null,
    country        text null,
    period_start   date null,
    period_end     date null
);
create index if not exists ix_pract_quals_pract on identity.practitioner_qualifications (practitioner_id);

create table if not exists identity.practitioner_specialties (
    id             uuid primary key,
    practitioner_id uuid not null references identity.practitioners (id) on delete cascade,
    code           text not null,
    display        text null
);
create index if not exists ix_pract_specialties_pract on identity.practitioner_specialties (practitioner_id);

-- Affectations (FHIR PractitionerRole) : professionnel -> établissement/service/unité.
-- Pas de FK inter-modules (couplage par id uniquement).
create table if not exists identity.affectations (
    id             uuid        primary key,
    tenant_id      text        null,
    practitioner_id uuid       not null references identity.practitioners (id) on delete cascade,
    facility_id    uuid        not null,
    service_id     uuid        null,
    care_unit_id   uuid        null,
    role_code      text        not null,
    specialty_code text        null,
    period_start   date        null,
    period_end     date        null,
    is_primary     boolean     not null default false,
    status         text        not null default 'Active',
    created_on_utc timestamptz not null
);
create index if not exists ix_affectations_pract on identity.affectations (practitioner_id);
create index if not exists ix_affectations_facility on identity.affectations (facility_id);

-- RBAC : permissions (catalogue), rôles, et leurs liens.
create table if not exists identity.permissions (
    code        text primary key,
    group_name  text not null,
    description text not null
);

create table if not exists identity.roles (
    id             uuid        primary key,
    tenant_id      text        null,
    code           text        not null,
    name           text        not null,
    description    text        null,
    is_system      boolean     not null default false,
    created_on_utc timestamptz not null
);
create unique index if not exists uq_roles_system_code on identity.roles (lower(code)) where tenant_id is null;

create table if not exists identity.role_permissions (
    role_id         uuid not null references identity.roles (id) on delete cascade,
    permission_code text not null references identity.permissions (code) on delete cascade,
    primary key (role_id, permission_code)
);

create table if not exists identity.user_roles (
    user_id        uuid not null references identity.users (id) on delete cascade,
    role_id        uuid not null references identity.roles (id) on delete cascade,
    scope          text not null default '',
    created_on_utc timestamptz not null,
    primary key (user_id, role_id, scope)
);

-- ============================================================
-- Seed : catalogue de permissions.
-- ============================================================
insert into identity.permissions (code, group_name, description) values
    ('*',                       'all',            'Toutes les permissions (super-admin)'),
    ('identity.user.read',      'identity',       'Voir les comptes utilisateurs'),
    ('identity.user.manage',    'identity',       'Gérer les comptes utilisateurs'),
    ('identity.role.read',      'identity',       'Voir les rôles'),
    ('identity.role.manage',    'identity',       'Gérer les rôles et habilitations'),
    ('infrastructure.read',     'infrastructure', 'Voir les établissements/bâtiments/services'),
    ('infrastructure.manage',   'infrastructure', 'Gérer les établissements/bâtiments/services'),
    ('reference.read',          'reference',      'Voir les données de référence'),
    ('reference.manage',        'reference',      'Gérer les données de référence'),
    ('patient.read',            'patient',        'Voir les patients'),
    ('patient.create',          'patient',        'Créer des patients'),
    ('patient.update',          'patient',        'Modifier des patients'),
    ('patient.merge',           'patient',        'Fusionner des dossiers patients'),
    ('clinical.read',           'clinical',       'Voir le dossier clinique'),
    ('clinical.write',          'clinical',       'Écrire dans le dossier clinique'),
    ('clinical.prescribe',      'clinical',       'Prescrire'),
    ('nursing.observe',         'nursing',        'Saisir observations/constantes'),
    ('nursing.administer',      'nursing',        'Administrer des traitements (MAR)'),
    ('pharmacy.read',           'pharmacy',       'Voir la pharmacie/prescriptions'),
    ('pharmacy.dispense',       'pharmacy',       'Dispenser des médicaments'),
    ('pharmacy.stock.manage',   'pharmacy',       'Gérer le stock pharmacie'),
    ('lab.request',             'lab',            'Demander des analyses'),
    ('lab.result.read',         'lab',            'Voir les résultats de labo'),
    ('lab.result.validate',     'lab',            'Valider les résultats de labo'),
    ('appointment.read',        'appointment',    'Voir les rendez-vous'),
    ('appointment.manage',      'appointment',    'Gérer les rendez-vous'),
    ('billing.read',            'billing',        'Voir la facturation'),
    ('billing.manage',          'billing',        'Gérer la facturation'),
    ('billing.payment.manage',  'billing',        'Encaisser les paiements'),
    ('accounting.read',         'accounting',     'Voir la comptabilité'),
    ('accounting.manage',       'accounting',     'Gérer la comptabilité'),
    ('hr.read',                 'hr',             'Voir le personnel'),
    ('hr.manage',               'hr',             'Gérer le personnel'),
    ('frontdesk.reception',     'frontdesk',      'Accueil / enregistrement'),
    ('frontdesk.queue.manage',  'frontdesk',      'Gérer la file d''attente'),
    ('audit.read',              'audit',          'Consulter le journal d''audit'),
    ('audit.manage',            'audit',          'Gérer la politique d''audit'),
    ('report.read',             'report',         'Voir les rapports/indicateurs');

-- ============================================================
-- Seed : rôles système (globaux, tenant_id null).
-- ============================================================
insert into identity.roles (id, tenant_id, code, name, description, is_system, created_on_utc) values
    (gen_random_uuid(), null, 'SUPER_ADMIN',     'Super-administrateur',   'Accès total à la plateforme', true, now()),
    (gen_random_uuid(), null, 'FACILITY_ADMIN',  'Admin établissement',    'Administration d''un établissement', true, now()),
    (gen_random_uuid(), null, 'DOCTOR',          'Médecin',                'Soins et prescription', true, now()),
    (gen_random_uuid(), null, 'NURSE',           'Infirmier',              'Soins infirmiers', true, now()),
    (gen_random_uuid(), null, 'MIDWIFE',         'Sage-femme',             'Maternité et soins', true, now()),
    (gen_random_uuid(), null, 'PHARMACIST',      'Pharmacien',             'Pharmacie et dispensation', true, now()),
    (gen_random_uuid(), null, 'LAB_TECH',        'Laborantin',             'Laboratoire', true, now()),
    (gen_random_uuid(), null, 'RECEPTIONIST',    'Secrétariat / Accueil',  'Accueil et rendez-vous', true, now()),
    (gen_random_uuid(), null, 'CASHIER',         'Caissier / Facturation', 'Facturation et encaissement', true, now()),
    (gen_random_uuid(), null, 'ACCOUNTANT',      'Comptable',              'Comptabilité', true, now()),
    (gen_random_uuid(), null, 'HR_MANAGER',      'Ressources humaines',    'Gestion du personnel', true, now()),
    (gen_random_uuid(), null, 'QUALITY_OFFICER', 'Qualité / Vigilance',    'Qualité et vigilance', true, now()),
    (gen_random_uuid(), null, 'AUDITOR_DPO',     'Auditeur / DPO',         'Audit et protection des données', true, now()),
    (gen_random_uuid(), null, 'READONLY',        'Lecture seule',          'Consultation en lecture seule', true, now());

-- ============================================================
-- Seed : matrice rôle -> permissions.
-- ============================================================
insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on true
where r.code = 'SUPER_ADMIN' and r.tenant_id is null and p.code = '*';

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'identity.user.read','identity.user.manage','identity.role.read','identity.role.manage',
    'infrastructure.read','infrastructure.manage','reference.read','reference.manage',
    'patient.read','billing.read','hr.read','accounting.read','audit.read','report.read'])
where r.code = 'FACILITY_ADMIN' and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'patient.read','clinical.read','clinical.write','clinical.prescribe',
    'pharmacy.read','lab.request','lab.result.read','appointment.read','appointment.manage','report.read'])
where r.code = 'DOCTOR' and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'patient.read','clinical.read','nursing.observe','nursing.administer',
    'pharmacy.read','lab.result.read','appointment.read'])
where r.code in ('NURSE','MIDWIFE') and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'patient.read','clinical.read','pharmacy.read','pharmacy.dispense','pharmacy.stock.manage'])
where r.code = 'PHARMACIST' and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'patient.read','clinical.read','lab.request','lab.result.read','lab.result.validate'])
where r.code = 'LAB_TECH' and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'patient.read','patient.create','patient.update','appointment.read','appointment.manage',
    'frontdesk.reception','frontdesk.queue.manage','billing.read'])
where r.code = 'RECEPTIONIST' and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'patient.read','billing.read','billing.manage','billing.payment.manage'])
where r.code = 'CASHIER' and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'billing.read','accounting.read','accounting.manage'])
where r.code = 'ACCOUNTANT' and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'hr.read','hr.manage'])
where r.code = 'HR_MANAGER' and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'patient.read','clinical.read','pharmacy.read','lab.result.read','audit.read','report.read'])
where r.code = 'QUALITY_OFFICER' and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'patient.read','clinical.read','pharmacy.read','lab.result.read','billing.read',
    'infrastructure.read','reference.read','identity.user.read','hr.read','accounting.read',
    'audit.read','audit.manage','report.read'])
where r.code = 'AUDITOR_DPO' and r.tenant_id is null;

insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'patient.read','clinical.read','pharmacy.read','lab.result.read','billing.read',
    'infrastructure.read','reference.read','report.read'])
where r.code = 'READONLY' and r.tenant_id is null;
