-- Module Pharmacie — Phase 4 : prescriptions et dispensation.
-- Le patient est identifié en texte libre (nom + référence) en attendant le module Patient ;
-- patient_id (uuid null) est réservé pour le rattachement futur. Pas de FK cross-schéma.
-- Les lignes référencent leur prescription par FK (même schéma → autorisé).

create table if not exists pharmacy.prescriptions (
    id                uuid        primary key,
    facility_id       uuid        not null,                 -- établissement (cloisonnement multi-tenant)
    patient_id        uuid        null,                     -- réservé : rattachement futur au module Patient
    patient_name      text        not null,                 -- identification patient (texte libre)
    patient_reference text        null,                     -- n° dossier / identifiant patient (texte libre)
    prescriber_name   text        null,                     -- prescripteur (texte libre)
    reference         text        null,                     -- référence interne de l'ordonnance
    status            text        not null default 'Active',-- Active | Completed | Cancelled
    prescribed_on     date        null,
    notes             text        null,
    created_on_utc    timestamptz not null,
    updated_on_utc    timestamptz null,
    constraint ck_prescriptions_status check (status in ('Active', 'Completed', 'Cancelled'))
);

create index if not exists ix_prescriptions_facility on pharmacy.prescriptions (facility_id);
create index if not exists ix_prescriptions_status on pharmacy.prescriptions (status);
create index if not exists ix_prescriptions_patient on pharmacy.prescriptions (facility_id, patient_name);

create table if not exists pharmacy.prescription_lines (
    id                 uuid        primary key,
    prescription_id    uuid        not null references pharmacy.prescriptions (id) on delete cascade,
    medication_id      uuid        not null,                -- médicament prescrit (pharmacy.medications)
    dosage             text        null,                    -- posologie (ex. « 1 cp 3x/jour, 7 jours »)
    quantity_prescribed int        not null,
    quantity_dispensed  int        not null default 0,
    created_on_utc     timestamptz not null,
    updated_on_utc     timestamptz null,
    constraint ck_rx_lines_qty_prescribed check (quantity_prescribed > 0),
    constraint ck_rx_lines_qty_dispensed check (quantity_dispensed >= 0 and quantity_dispensed <= quantity_prescribed)
);

create index if not exists ix_rx_lines_prescription on pharmacy.prescription_lines (prescription_id);
create index if not exists ix_rx_lines_medication on pharmacy.prescription_lines (medication_id);
