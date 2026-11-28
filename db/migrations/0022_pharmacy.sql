-- Module Pharmacie — socle + catalogue médicaments.
-- Schéma dédié (isolation logique par module, pas de FK cross-schéma : les liens vers
-- Organization/Patient/Identity se font par identifiant applicatif).
create schema if not exists pharmacy;

-- Catalogue des médicaments référencés par établissement (référentiel produits).
-- Le stock, les lots et la dispensation sont gérés par des tables distinctes (phases suivantes)
-- qui référencent cette fiche par son identifiant.
create table if not exists pharmacy.medications (
    id                uuid        primary key,
    facility_id       uuid        not null,                       -- établissement propriétaire (cloisonnement multi-tenant)
    code              text        not null,                       -- code interne (unique par établissement)
    name              text        not null,                       -- libellé / nom commercial
    inn               text        null,                           -- DCI (dénomination commune internationale)
    form              text        not null,                       -- forme galénique (enum MedicationForm)
    strength          text        null,                           -- dosage, ex. « 500 mg »
    unit              text        not null,                       -- unité de dispensation, ex. « comprimé », « flacon »
    atc_code          text        null,                           -- code ATC
    therapeutic_class text        null,                           -- classe thérapeutique
    controlled        boolean     not null default false,         -- stupéfiant / liste contrôlée
    status            text        not null default 'Active',      -- Active | Inactive
    notes             text        null,
    created_on_utc    timestamptz not null,
    updated_on_utc    timestamptz null,
    constraint ck_medications_form check (form in (
        'Tablet', 'Capsule', 'Syrup', 'Solution', 'Injectable', 'Infusion', 'Cream', 'Ointment',
        'Drops', 'Suppository', 'Inhaler', 'Patch', 'Powder', 'Other')),
    constraint ck_medications_status check (status in ('Active', 'Inactive')),
    constraint uq_medications_code unique (facility_id, code)
);

create index if not exists ix_medications_facility on pharmacy.medications (facility_id);
create index if not exists ix_medications_status on pharmacy.medications (status);
create index if not exists ix_medications_name on pharmacy.medications (name);
create index if not exists ix_medications_atc on pharmacy.medications (atc_code);
