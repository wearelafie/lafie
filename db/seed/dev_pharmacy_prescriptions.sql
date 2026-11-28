-- Jeu de données de DÉVELOPPEMENT — module Pharmacie (prescriptions & dispensation).
-- Idempotent. Réutilise CHU (0a..01) et ses médicaments (dev_pharmacy.sql). Patient en texte libre.

insert into pharmacy.prescriptions
    (id, facility_id, patient_id, patient_name, patient_reference, prescriber_name, reference, status, prescribed_on, notes, created_on_utc, updated_on_utc)
values
    ('0c100000-0000-4000-8000-000000000001', '0a000000-0000-4000-8000-000000000001', null, 'Ama Koffi', 'PAT-00123', 'Dr Komlan Koffi', 'ORD-2026-001', 'Active', '2026-11-27', null, now(), null)
on conflict (id) do nothing;

insert into pharmacy.prescription_lines
    (id, prescription_id, medication_id, dosage, quantity_prescribed, quantity_dispensed, created_on_utc, updated_on_utc)
values
    ('0c200000-0000-4000-8000-000000000001', '0c100000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000001', '1 comprime 3x/jour pendant 5 jours', 15, 0, now(), null),
    ('0c200000-0000-4000-8000-000000000002', '0c100000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000002', '1 comprime matin et soir, 7 jours',  14, 0, now(), null)
on conflict (id) do nothing;
