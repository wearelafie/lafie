-- Jeu de données de DÉVELOPPEMENT — module Pharmacie (catalogue médicaments).
-- Idempotent : conflit sur (facility_id, code) ignoré. Réutilise les établissements seedés
-- par dev_infrastructure.sql (CHU = 0a..01, Clinique Biasa = 0a..02).

insert into pharmacy.medications
    (id, facility_id, code, name, inn, form, strength, unit, atc_code, therapeutic_class, controlled, status, notes, created_on_utc, updated_on_utc)
values
    -- CHU Sylvanus Olympio
    ('0d000000-0000-4000-8000-000000000001', '0a000000-0000-4000-8000-000000000001', 'MED-PARA500', 'Paracétamol 500 mg', 'Paracétamol',    'Tablet',     '500 mg',  'comprimé', 'N02BE01', 'Antalgique / antipyrétique',   false, 'Active', null, now(), null),
    ('0d000000-0000-4000-8000-000000000002', '0a000000-0000-4000-8000-000000000001', 'MED-AMOX1G',  'Amoxicilline 1 g',   'Amoxicilline',   'Tablet',     '1 g',     'comprimé', 'J01CA04', 'Antibiotique (bêta-lactamine)', false, 'Active', null, now(), null),
    ('0d000000-0000-4000-8000-000000000003', '0a000000-0000-4000-8000-000000000001', 'MED-METRO',   'Métronidazole 250 mg','Métronidazole', 'Tablet',     '250 mg',  'comprimé', 'J01XD01', 'Antibactérien / antiparasitaire', false, 'Active', null, now(), null),
    ('0d000000-0000-4000-8000-000000000004', '0a000000-0000-4000-8000-000000000001', 'MED-CEFTRI',  'Ceftriaxone 1 g',    'Ceftriaxone',    'Injectable', '1 g',     'flacon',   'J01DD04', 'Antibiotique (céphalosporine)', false, 'Active', null, now(), null),
    ('0d000000-0000-4000-8000-000000000005', '0a000000-0000-4000-8000-000000000001', 'MED-MORPH',   'Morphine 10 mg/mL',  'Morphine',       'Injectable', '10 mg/mL','ampoule',  'N02AA01', 'Antalgique opioïde',            true,  'Active', 'Stupéfiant — traçabilité renforcée.', now(), null),
    ('0d000000-0000-4000-8000-000000000006', '0a000000-0000-4000-8000-000000000001', 'MED-ORS',     'Sels de réhydratation orale', 'SRO',    'Powder',     null,      'sachet',   'A07CA',   'Réhydratation',                 false, 'Active', null, now(), null),
    -- Clinique Biasa
    ('0d000000-0000-4000-8000-000000000101', '0a000000-0000-4000-8000-000000000002', 'MED-PARA500', 'Paracétamol 500 mg', 'Paracétamol',    'Tablet',     '500 mg',  'comprimé', 'N02BE01', 'Antalgique / antipyrétique',   false, 'Active', null, now(), null),
    ('0d000000-0000-4000-8000-000000000102', '0a000000-0000-4000-8000-000000000002', 'MED-IBUP400', 'Ibuprofène 400 mg',  'Ibuprofène',     'Tablet',     '400 mg',  'comprimé', 'M01AE01', 'Anti-inflammatoire (AINS)',     false, 'Active', null, now(), null)
on conflict (facility_id, code) do nothing;
