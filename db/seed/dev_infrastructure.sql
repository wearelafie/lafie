-- Seed de DÉVELOPPEMENT du module Infrastructure : établissements → bâtiments → étages → espaces.
-- Idempotent : le bloc entier ne s'exécute que si l'établissement sentinelle n'existe pas encore.
-- N'est JAMAIS appliqué en production (exécuté uniquement par DevDataSeeder en environnement Development).
do $$
declare
    v_chu  constant uuid        := '0a000000-0000-4000-8000-000000000001';
    v_clin constant uuid        := '0a000000-0000-4000-8000-000000000002';
    v_now  constant timestamptz := now();
    b_id uuid;
    f_id uuid;
begin
    -- Déjà seedé → ne rien faire (idempotent).
    if exists (select 1 from organization.organizations where id = v_chu) then
        return;
    end if;

    -- ============================================================
    -- Établissement 1 : CHU Sylvanus Olympio (public, universitaire, Lomé)
    -- ============================================================
    insert into organization.organizations
        (id, code, official_name, display_name, abbreviation, facility_type_code, ownership_type_code,
         country_code, region_code, locality, street_address, phone, email, website_url,
         time_zone_id, default_currency_code, default_language_code,
         operational_status, configuration_status, emergency_services_available, public_facing,
         established_on, created_on_utc)
    values
        (v_chu, 'CHU-SO', 'Centre Hospitalier Universitaire Sylvanus Olympio', 'CHU Sylvanus Olympio', 'CHU-SO',
         'UNIVERSITY_HOSPITAL', 'PUBLIC', 'TG', 'TG-M', 'Lomé', 'Boulevard du 13 Janvier', '+228 22 21 25 01',
         'contact@chu-so.tg', 'https://chu-so.tg', 'Africa/Lome', 'XOF', 'fr',
         'Active', 'Completed', true, true, date '1965-01-01', v_now);

    -- --- Bâtiment A : Consultations externes ---
    b_id := gen_random_uuid();
    insert into organization.buildings (id, facility_id, code, name, description, location, status, created_on_utc)
    values (b_id, v_chu, 'BAT-A', 'Bâtiment A — Consultations externes', 'Consultations, accueil et pharmacie', 'Entrée principale', 'Active', v_now);

    f_id := gen_random_uuid();
    insert into organization.floors (id, building_id, code, name, level, display_order, status, created_on_utc)
    values (f_id, b_id, 'A-RDC', 'Rez-de-chaussée', 0, 0, 'Active', v_now);
    insert into organization.spaces (id, facility_id, building_id, floor_id, code, name, type, area, max_capacity, accessible, status, created_on_utc) values
        (gen_random_uuid(), v_chu, b_id, f_id, 'A-RDC-ACC', 'Accueil',          'Office',           25,  10, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'A-RDC-ATT', 'Salle d''attente', 'WaitingRoom',      60,  40, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'A-RDC-C1',  'Consultation 1',   'ConsultationRoom', 16,   3, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'A-RDC-C2',  'Consultation 2',   'ConsultationRoom', 16,   3, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'A-RDC-PHA', 'Pharmacie',        'Pharmacy',         30,   5, true,  'Active',      v_now);

    f_id := gen_random_uuid();
    insert into organization.floors (id, building_id, code, name, level, display_order, status, created_on_utc)
    values (f_id, b_id, 'A-E1', 'Étage 1', 1, 1, 'Active', v_now);
    insert into organization.spaces (id, facility_id, building_id, floor_id, code, name, type, area, max_capacity, accessible, status, created_on_utc) values
        (gen_random_uuid(), v_chu, b_id, f_id, 'A-E1-DIR',  'Bureau médecin-chef', 'Office',        18,   4, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'A-E1-SOIN', 'Salle de soins',      'TreatmentRoom', 22,   2, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'A-E1-LAB',  'Laboratoire',         'Laboratory',    35,   6, true,  'Active',      v_now);

    -- --- Bâtiment B : Hospitalisation ---
    b_id := gen_random_uuid();
    insert into organization.buildings (id, facility_id, code, name, description, location, status, created_on_utc)
    values (b_id, v_chu, 'BAT-B', 'Bâtiment B — Hospitalisation', 'Urgences, bloc et chambres', 'Aile est', 'Active', v_now);

    f_id := gen_random_uuid();
    insert into organization.floors (id, building_id, code, name, level, display_order, status, created_on_utc)
    values (f_id, b_id, 'B-RDC', 'Rez-de-chaussée', 0, 0, 'Active', v_now);
    insert into organization.spaces (id, facility_id, building_id, floor_id, code, name, type, area, max_capacity, accessible, status, created_on_utc) values
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-RDC-URG', 'Urgences',        'EmergencyRoom',     80,  20, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-RDC-BLOC','Bloc opératoire', 'OperatingRoom',     45,   8, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-RDC-REA', 'Réanimation',     'IntensiveCareRoom', 60,  10, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-RDC-REV', 'Salle de réveil', 'RecoveryRoom',      30,   6, true,  'Active',      v_now);

    f_id := gen_random_uuid();
    insert into organization.floors (id, building_id, code, name, level, display_order, status, created_on_utc)
    values (f_id, b_id, 'B-E1', 'Étage 1 — Médecine', 1, 1, 'Active', v_now);
    insert into organization.spaces (id, facility_id, building_id, floor_id, code, name, type, area, max_capacity, accessible, status, created_on_utc) values
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-E1-101', 'Chambre 101',     'PatientRoom', 20, 2, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-E1-102', 'Chambre 102',     'PatientRoom', 20, 2, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-E1-103', 'Chambre 103',     'PatientRoom', 20, 2, false, 'Maintenance', v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-E1-PI',  'Poste infirmier', 'Office',      14, 4, true,  'Active',      v_now);

    f_id := gen_random_uuid();
    insert into organization.floors (id, building_id, code, name, level, display_order, status, created_on_utc)
    values (f_id, b_id, 'B-E2', 'Étage 2 — Chirurgie', 2, 2, 'Active', v_now);
    insert into organization.spaces (id, facility_id, building_id, floor_id, code, name, type, area, max_capacity, accessible, status, created_on_utc) values
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-E2-201', 'Chambre 201', 'PatientRoom', 22, 2, true,  'Active',  v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-E2-202', 'Chambre 202', 'PatientRoom', 22, 2, true,  'Active',  v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'B-E2-RES', 'Réserve',     'Storage',     12, 0, false, 'Active',  v_now);

    -- --- Bâtiment C : Plateau technique ---
    b_id := gen_random_uuid();
    insert into organization.buildings (id, facility_id, code, name, description, location, status, created_on_utc)
    values (b_id, v_chu, 'BAT-C', 'Bâtiment C — Plateau technique', 'Imagerie, laboratoire, stérilisation', 'Aile ouest', 'Maintenance', v_now);

    f_id := gen_random_uuid();
    insert into organization.floors (id, building_id, code, name, level, display_order, status, created_on_utc)
    values (f_id, b_id, 'C-RDC', 'Rez-de-chaussée', 0, 0, 'Active', v_now);
    insert into organization.spaces (id, facility_id, building_id, floor_id, code, name, type, area, max_capacity, accessible, status, created_on_utc) values
        (gen_random_uuid(), v_chu, b_id, f_id, 'C-RDC-IMG', 'Imagerie',          'ImagingRoom',   40, 5, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'C-RDC-LAB', 'Laboratoire central','Laboratory',   55, 8, true,  'Active',      v_now),
        (gen_random_uuid(), v_chu, b_id, f_id, 'C-RDC-STE', 'Stérilisation',     'TechnicalRoom', 25, 3, true,  'Active',      v_now);

    -- ============================================================
    -- Établissement 2 : Clinique Biasa (privé, Lomé)
    -- ============================================================
    insert into organization.organizations
        (id, code, official_name, display_name, abbreviation, facility_type_code, ownership_type_code,
         country_code, region_code, locality, street_address, phone, email,
         time_zone_id, default_currency_code, default_language_code,
         operational_status, configuration_status, emergency_services_available, public_facing,
         created_on_utc)
    values
        (v_clin, 'CLIN-BIASA', 'Clinique Biasa', 'Clinique Biasa', 'BIASA',
         'CLINIC', 'PRIVATE_FOR_PROFIT', 'TG', 'TG-M', 'Lomé', 'Rue des Clercs', '+228 22 20 10 10',
         'accueil@clinique-biasa.tg', 'Africa/Lome', 'XOF', 'fr',
         'Active', 'Completed', false, true, v_now);

    -- --- Bâtiment principal ---
    b_id := gen_random_uuid();
    insert into organization.buildings (id, facility_id, code, name, description, location, status, created_on_utc)
    values (b_id, v_clin, 'BAT-P', 'Bâtiment principal', 'Consultations, maternité et bloc', null, 'Active', v_now);

    f_id := gen_random_uuid();
    insert into organization.floors (id, building_id, code, name, level, display_order, status, created_on_utc)
    values (f_id, b_id, 'P-RDC', 'Rez-de-chaussée', 0, 0, 'Active', v_now);
    insert into organization.spaces (id, facility_id, building_id, floor_id, code, name, type, area, max_capacity, accessible, status, created_on_utc) values
        (gen_random_uuid(), v_clin, b_id, f_id, 'P-RDC-ACC', 'Accueil',      'Office',           18, 6, true, 'Active', v_now),
        (gen_random_uuid(), v_clin, b_id, f_id, 'P-RDC-CON', 'Consultation', 'ConsultationRoom', 14, 3, true, 'Active', v_now),
        (gen_random_uuid(), v_clin, b_id, f_id, 'P-RDC-PHA', 'Pharmacie',    'Pharmacy',         20, 4, true, 'Active', v_now);

    f_id := gen_random_uuid();
    insert into organization.floors (id, building_id, code, name, level, display_order, status, created_on_utc)
    values (f_id, b_id, 'P-E1', 'Étage 1', 1, 1, 'Active', v_now);
    insert into organization.spaces (id, facility_id, building_id, floor_id, code, name, type, area, max_capacity, accessible, status, created_on_utc) values
        (gen_random_uuid(), v_clin, b_id, f_id, 'P-E1-MAT', 'Maternité',   'PatientRoom',   28, 4, true, 'Active', v_now),
        (gen_random_uuid(), v_clin, b_id, f_id, 'P-E1-BLOC','Bloc',        'OperatingRoom', 35, 6, true, 'Active', v_now),
        (gen_random_uuid(), v_clin, b_id, f_id, 'P-E1-CH1', 'Chambre 1',   'PatientRoom',   18, 2, true, 'Active', v_now);
end $$;

-- ============================================================
-- Services & unités de soins (bloc indépendant, sentinelle propre).
-- Réutilise les établissements/espaces seedés ci-dessus (lookup par id/code).
-- ============================================================
do $$
declare
    v_chu   constant uuid        := '0a000000-0000-4000-8000-000000000001';
    v_clin  constant uuid        := '0a000000-0000-4000-8000-000000000002';
    v_sent  constant uuid        := '0b000000-0000-4000-8000-000000000001'; -- service sentinelle
    v_now   constant timestamptz := now();
    s_id uuid;
    s_img uuid;
    u_id uuid;
begin
    -- Pré-requis : établissement CHU seedé ; et pas déjà fait.
    if not exists (select 1 from organization.organizations where id = v_chu) then
        return;
    end if;
    if exists (select 1 from organization.services where id = v_sent) then
        return;
    end if;

    -- === CHU : services cliniques ===
    s_id := v_sent;
    insert into organization.services (id, facility_id, parent_service_id, code, name, category, head, display_order, status, created_on_utc)
    values (s_id, v_chu, null, 'CARD', 'Cardiologie', 'Clinical', 'Dr Koffi', 1, 'Active', v_now);

    u_id := gen_random_uuid();
    insert into organization.care_units (id, facility_id, service_id, code, name, type, bed_capacity, display_order, status, created_on_utc)
    values (u_id, v_chu, s_id, 'CARD-HOSP', 'Hospitalisation Cardiologie', 'Hospitalization', 20, 1, 'Active', v_now);
    insert into organization.care_unit_spaces (care_unit_id, space_id, created_on_utc)
        select u_id, sp.id, v_now from organization.spaces sp where sp.facility_id = v_chu and sp.code in ('B-E1-101', 'B-E1-102');

    u_id := gen_random_uuid();
    insert into organization.care_units (id, facility_id, service_id, code, name, type, bed_capacity, display_order, status, created_on_utc)
    values (u_id, v_chu, s_id, 'CARD-USIC', 'USIC', 'IntensiveCare', 8, 2, 'Active', v_now);
    insert into organization.care_unit_spaces (care_unit_id, space_id, created_on_utc)
        select u_id, sp.id, v_now from organization.spaces sp where sp.facility_id = v_chu and sp.code = 'B-RDC-REA';

    s_id := gen_random_uuid();
    insert into organization.services (id, facility_id, parent_service_id, code, name, category, head, display_order, status, created_on_utc)
    values (s_id, v_chu, null, 'PED', 'Pédiatrie', 'Clinical', 'Dr Mensah', 2, 'Active', v_now);
    u_id := gen_random_uuid();
    insert into organization.care_units (id, facility_id, service_id, code, name, type, bed_capacity, display_order, status, created_on_utc)
    values (u_id, v_chu, s_id, 'PED-HOSP', 'Hospitalisation Pédiatrie', 'Hospitalization', 15, 1, 'Active', v_now);
    insert into organization.care_unit_spaces (care_unit_id, space_id, created_on_utc)
        select u_id, sp.id, v_now from organization.spaces sp where sp.facility_id = v_chu and sp.code in ('B-E2-201', 'B-E2-202');

    -- === CHU : services médico-techniques (dont hiérarchie Imagerie > Scanner/IRM) ===
    s_id := gen_random_uuid();
    insert into organization.services (id, facility_id, parent_service_id, code, name, category, display_order, status, created_on_utc)
    values (s_id, v_chu, null, 'LAB', 'Laboratoire', 'MedicoTechnical', 3, 'Active', v_now);
    u_id := gen_random_uuid();
    insert into organization.care_units (id, facility_id, service_id, code, name, type, display_order, status, created_on_utc)
    values (u_id, v_chu, s_id, 'LAB-CENT', 'Laboratoire central', 'Other', 1, 'Active', v_now);
    insert into organization.care_unit_spaces (care_unit_id, space_id, created_on_utc)
        select u_id, sp.id, v_now from organization.spaces sp where sp.facility_id = v_chu and sp.code = 'C-RDC-LAB';

    s_img := gen_random_uuid();
    insert into organization.services (id, facility_id, parent_service_id, code, name, category, display_order, status, created_on_utc)
    values (s_img, v_chu, null, 'IMG', 'Imagerie', 'MedicoTechnical', 4, 'Active', v_now);
    insert into organization.services (id, facility_id, parent_service_id, code, name, category, display_order, status, created_on_utc) values
        (gen_random_uuid(), v_chu, s_img, 'IMG-SCAN', 'Scanner', 'MedicoTechnical', 1, 'Active', v_now),
        (gen_random_uuid(), v_chu, s_img, 'IMG-IRM',  'IRM',     'MedicoTechnical', 2, 'Active', v_now);
    u_id := gen_random_uuid();
    insert into organization.care_units (id, facility_id, service_id, code, name, type, display_order, status, created_on_utc)
    values (u_id, v_chu, s_img, 'IMG-UNIT', 'Unité d''imagerie', 'Ambulatory', 1, 'Active', v_now);
    insert into organization.care_unit_spaces (care_unit_id, space_id, created_on_utc)
        select u_id, sp.id, v_now from organization.spaces sp where sp.facility_id = v_chu and sp.code = 'C-RDC-IMG';

    -- === CHU : service administratif (sans unité) ===
    insert into organization.services (id, facility_id, parent_service_id, code, name, category, head, display_order, status, created_on_utc)
    values (gen_random_uuid(), v_chu, null, 'DIR', 'Direction', 'Administrative', 'Mme Adjo', 5, 'Active', v_now);

    -- === Clinique Biasa : maternité ===
    if exists (select 1 from organization.organizations where id = v_clin) then
        s_id := gen_random_uuid();
        insert into organization.services (id, facility_id, parent_service_id, code, name, category, display_order, status, created_on_utc)
        values (s_id, v_clin, null, 'MAT', 'Maternité', 'Clinical', 1, 'Active', v_now);
        u_id := gen_random_uuid();
        insert into organization.care_units (id, facility_id, service_id, code, name, type, bed_capacity, display_order, status, created_on_utc)
        values (u_id, v_clin, s_id, 'MAT-HOSP', 'Unité Maternité', 'Maternity', 10, 1, 'Active', v_now);
        insert into organization.care_unit_spaces (care_unit_id, space_id, created_on_utc)
            select u_id, sp.id, v_now from organization.spaces sp where sp.facility_id = v_clin and sp.code = 'P-E1-MAT';
    end if;
end $$;
