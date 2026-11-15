-- Seed de DÉVELOPPEMENT des 4 modules « services techniques » du module Organization :
-- GMAO (tickets de maintenance), Hygiène (bio-nettoyage), Stérilisation (CSSD) et Déchets (DASRI).
-- Réutilise les établissements/espaces seedés par dev_infrastructure.sql (lookup par code).
-- Chaque bloc est idempotent via une sentinelle propre. JAMAIS appliqué en production.

-- ============================================================
-- 1) GMAO — tickets de maintenance curative (+ pièces détachées)
-- ============================================================
do $$
declare
    v_chu   constant uuid        := '0a000000-0000-4000-8000-000000000001';
    v_clin  constant uuid        := '0a000000-0000-4000-8000-000000000002';
    v_now   constant timestamptz := now();
    v_t1    constant uuid        := '0c000000-0000-4000-8000-000000000001'; -- sentinelle GMAO
    sp_img uuid; sp_bloc uuid; sp_ch103 uuid; sp_att uuid; sp_bbloc uuid;
begin
    if not exists (select 1 from organization.organizations where id = v_chu) then return; end if;
    if exists (select 1 from organization.maintenance_tickets where id = v_t1) then return; end if;

    select id into sp_img   from organization.spaces where facility_id = v_chu and code = 'C-RDC-IMG';
    select id into sp_bloc  from organization.spaces where facility_id = v_chu and code = 'B-RDC-BLOC';
    select id into sp_ch103 from organization.spaces where facility_id = v_chu and code = 'B-E1-103';
    select id into sp_att   from organization.spaces where facility_id = v_chu and code = 'A-RDC-ATT';

    insert into organization.maintenance_tickets
        (id, facility_id, target_type, target_id, title, description, criticality, blocking, status,
         assignee_type, assignee_ref, reported_by, resolution, downtime_start, downtime_end, created_on_utc, updated_on_utc)
    values
        (v_t1, v_chu, 'Space', sp_img, 'Scanner indisponible — artefacts à l''image',
         'Images inexploitables depuis ce matin, examens reportés.', 'High', true, 'InProgress',
         'Internal', 'Équipe biomédicale', 'Dr Koffi', null, v_now - interval '2 days', null, v_now - interval '2 days', v_now),
        (gen_random_uuid(), v_chu, 'Space', sp_bloc, 'Climatisation du bloc défaillante',
         'Température du bloc trop élevée, risque sur l''asepsie.', 'Critical', true, 'Assigned',
         'External', 'ClimTogo SARL', 'Surveillante de bloc', null, null, null, v_now - interval '1 day', v_now),
        (gen_random_uuid(), v_chu, 'Space', sp_ch103, 'Fuite de plomberie — chambre 103',
         'Infiltration au plafond, chambre mise hors service.', 'Medium', false, 'Resolved',
         'Internal', 'Technicien Afo', 'Poste infirmier', 'Joint défectueux remplacé, séchage vérifié.',
         v_now - interval '5 days', v_now - interval '4 days', v_now - interval '5 days', v_now - interval '4 days'),
        (gen_random_uuid(), v_chu, 'Space', sp_att, 'Éclairage à remplacer — salle d''attente',
         'Deux tubes néon grillés.', 'Low', false, 'Open', null, null, 'Agent d''accueil', null, null, null, v_now, null);

    insert into organization.maintenance_ticket_parts (id, ticket_id, label, reference, quantity, unit_cost, created_on_utc)
    values (gen_random_uuid(), v_t1, 'Tube à rayons X', 'XR-TUBE-SOMATOM', 1, 1500000.00, v_now);

    if exists (select 1 from organization.organizations where id = v_clin) then
        select id into sp_bbloc from organization.spaces where facility_id = v_clin and code = 'P-E1-BLOC';
        insert into organization.maintenance_tickets
            (id, facility_id, target_type, target_id, title, description, criticality, blocking, status, reported_by, created_on_utc)
        values
            (gen_random_uuid(), v_clin, 'Space', sp_bbloc, 'Table opératoire bloquée en position',
             'Réglage hydraulique hors service.', 'High', true, 'Open', 'Dr Mensah', v_now);
    end if;
end $$;

-- ============================================================
-- 2) Hygiène — tâches de bio-nettoyage (+ checklist)
-- ============================================================
do $$
declare
    v_chu   constant uuid        := '0a000000-0000-4000-8000-000000000001';
    v_clin  constant uuid        := '0a000000-0000-4000-8000-000000000002';
    v_now   constant timestamptz := now();
    v_task1 constant uuid        := '0d000000-0000-4000-8000-000000000001'; -- sentinelle Hygiène
    t_id uuid; sp uuid;
begin
    if not exists (select 1 from organization.organizations where id = v_chu) then return; end if;
    if exists (select 1 from organization.cleaning_tasks where id = v_task1) then return; end if;

    -- Tâche 1 : bio-nettoyage terminal chambre 101 (en cours)
    select id into sp from organization.spaces where facility_id = v_chu and code = 'B-E1-101';
    insert into organization.cleaning_tasks
        (id, facility_id, space_id, protocol, trigger, status, requested_by, assigned_to, notes, started_on_utc, created_on_utc, updated_on_utc)
    values (v_task1, v_chu, sp, 'Terminal', 'PostDischarge', 'InProgress', 'Poste infirmier', 'Agent Yao',
            'Sortie patient, bio-nettoyage complet avant admission.', v_now - interval '1 hour', v_now - interval '1 hour', v_now);
    insert into organization.cleaning_task_items (id, task_id, label, done, done_on_utc, sort_order, created_on_utc) values
        (gen_random_uuid(), v_task1, 'Évacuation des déchets et du linge',        true,  v_now - interval '50 minutes', 0, v_now - interval '1 hour'),
        (gen_random_uuid(), v_task1, 'Détergent-désinfectant des surfaces hautes', true,  v_now - interval '35 minutes', 1, v_now - interval '1 hour'),
        (gen_random_uuid(), v_task1, 'Désinfection des sols',                      false, null,                          2, v_now - interval '1 hour'),
        (gen_random_uuid(), v_task1, 'Réfection du lit',                            false, null,                          3, v_now - interval '1 hour');

    -- Tâche 2 : bloc opératoire, nettoyage approfondi (en attente)
    select id into sp from organization.spaces where facility_id = v_chu and code = 'B-RDC-BLOC';
    t_id := gen_random_uuid();
    insert into organization.cleaning_tasks
        (id, facility_id, space_id, protocol, trigger, status, requested_by, notes, created_on_utc)
    values (t_id, v_chu, sp, 'Deep', 'Scheduled', 'Pending', 'Surveillante de bloc',
            'Bio-nettoyage approfondi entre deux programmes opératoires.', v_now);
    insert into organization.cleaning_task_items (id, task_id, label, done, sort_order, created_on_utc) values
        (gen_random_uuid(), t_id, 'Pré-désinfection du matériel',   false, 0, v_now),
        (gen_random_uuid(), t_id, 'Nettoyage sols et parois',       false, 1, v_now),
        (gen_random_uuid(), t_id, 'Désinfection par voie aérienne', false, 2, v_now);

    -- Tâche 3 : salle d'attente, nettoyage standard (terminé)
    select id into sp from organization.spaces where facility_id = v_chu and code = 'A-RDC-ATT';
    t_id := gen_random_uuid();
    insert into organization.cleaning_tasks
        (id, facility_id, space_id, protocol, trigger, status, requested_by, assigned_to, started_on_utc, completed_on_utc, created_on_utc, updated_on_utc)
    values (t_id, v_chu, sp, 'Standard', 'OnDemand', 'Completed', 'Agent d''accueil', 'Agent Ama',
            v_now - interval '3 hours', v_now - interval '2 hours', v_now - interval '3 hours', v_now - interval '2 hours');
    insert into organization.cleaning_task_items (id, task_id, label, done, done_on_utc, sort_order, created_on_utc) values
        (gen_random_uuid(), t_id, 'Dépoussiérage et surfaces',   true, v_now - interval '2 hours 30 minutes', 0, v_now - interval '3 hours'),
        (gen_random_uuid(), t_id, 'Lavage-désinfection des sols', true, v_now - interval '2 hours 10 minutes', 1, v_now - interval '3 hours');

    -- Clinique Biasa : maternité (en attente)
    if exists (select 1 from organization.organizations where id = v_clin) then
        select id into sp from organization.spaces where facility_id = v_clin and code = 'P-E1-MAT';
        t_id := gen_random_uuid();
        insert into organization.cleaning_tasks
            (id, facility_id, space_id, protocol, trigger, status, requested_by, notes, created_on_utc)
        values (t_id, v_clin, sp, 'Terminal', 'PostDischarge', 'Pending', 'Sage-femme',
                'Bio-nettoyage terminal après accouchement.', v_now);
        insert into organization.cleaning_task_items (id, task_id, label, done, sort_order, created_on_utc) values
            (gen_random_uuid(), t_id, 'Évacuation déchets et linge', false, 0, v_now),
            (gen_random_uuid(), t_id, 'Désinfection complète',        false, 1, v_now);
    end if;
end $$;

-- ============================================================
-- 3) Stérilisation centrale (CSSD) — boîtes, cycles, charges, affectations
-- ============================================================
do $$
declare
    v_chu   constant uuid        := '0a000000-0000-4000-8000-000000000001';
    v_clin  constant uuid        := '0a000000-0000-4000-8000-000000000002';
    v_now   constant timestamptz := now();
    v_set1  constant uuid        := '0e000000-0000-4000-8000-000000000001'; -- sentinelle Stérilisation
    cyc1 uuid; cyc2 uuid; cyc3 uuid; set2 uuid; set3 uuid; set4 uuid;
begin
    if not exists (select 1 from organization.organizations where id = v_chu) then return; end if;
    if exists (select 1 from organization.instrument_sets where id = v_set1) then return; end if;

    -- Cycles d'autoclave
    cyc1 := gen_random_uuid();
    insert into organization.sterilization_cycles
        (id, facility_id, autoclave, cycle_number, program, status, operator_name, temperature_c, pressure_bar, duration_min, started_on_utc, ended_on_utc, result_notes, created_on_utc, updated_on_utc)
    values (cyc1, v_chu, 'Autoclave A', 'CY-2026-001', '134 °C · 18 min (prion)', 'Passed', 'Agent CSSD Koffi',
            134.00, 2.10, 18, v_now - interval '1 day', v_now - interval '1 day' + interval '40 minutes',
            'Intégrateur viré, charge conforme.', v_now - interval '1 day', v_now);
    cyc2 := gen_random_uuid();
    insert into organization.sterilization_cycles
        (id, facility_id, autoclave, cycle_number, program, status, operator_name, temperature_c, pressure_bar, duration_min, started_on_utc, ended_on_utc, result_notes, created_on_utc, updated_on_utc)
    values (cyc2, v_chu, 'Autoclave A', 'CY-2026-002', '121 °C · 20 min', 'Failed', 'Agent CSSD Koffi',
            118.00, 1.80, 20, v_now - interval '12 hours', v_now - interval '12 hours' + interval '35 minutes',
            'Témoin de passage non viré — charge déclassée en quarantaine.', v_now - interval '12 hours', v_now);
    cyc3 := gen_random_uuid();
    insert into organization.sterilization_cycles
        (id, facility_id, autoclave, cycle_number, program, status, operator_name, started_on_utc, created_on_utc)
    values (cyc3, v_chu, 'Autoclave B', 'CY-2026-003', '134 °C · 18 min', 'Running', 'Agent CSSD Ama',
            v_now - interval '20 minutes', v_now - interval '20 minutes');

    -- Boîtes d'instruments (états cohérents avec les cycles)
    set2 := gen_random_uuid(); set3 := gen_random_uuid(); set4 := gen_random_uuid();
    insert into organization.instrument_sets
        (id, facility_id, code, name, description, category, item_count, status, last_cycle_id, sterilized_on_utc, expiry_on, created_on_utc, updated_on_utc)
    values
        (v_set1, v_chu, 'SET-ORTHO-01', 'Boîte orthopédie n°1', 'Ancillaire ostéosynthèse', 'Orthopédie', 24,
         'Sterile', cyc1, v_now - interval '1 day', (v_now + interval '30 days')::date, v_now - interval '10 days', v_now),
        (set2, v_chu, 'SET-VISC-01', 'Boîte viscérale n°1', 'Laparotomie', 'Chirurgie viscérale', 30,
         'InSterilization', null, null, null, v_now - interval '9 days', v_now - interval '20 minutes'),
        (set3, v_chu, 'SET-ORL-01', 'Boîte ORL n°1', 'Amygdalectomie / adénoïdectomie', 'ORL', 15,
         'InUse', null, null, null, v_now - interval '8 days', v_now),
        (set4, v_chu, 'SET-GYNE-01', 'Boîte gynécologie n°1', 'Instrumentation obstétricale', 'Gynécologie', 18,
         'Quarantine', cyc2, null, null, v_now - interval '7 days', v_now - interval '12 hours');

    -- Charges (quelles boîtes dans quels cycles)
    insert into organization.sterilization_cycle_sets (cycle_id, set_id) values
        (cyc1, v_set1), (cyc2, set4), (cyc3, set2);

    -- Affectation à une intervention (boîte en cours d'utilisation)
    insert into organization.instrument_set_usages
        (id, set_id, intervention_label, patient_ref, scheduled_on, performed_by, status, notes, created_on_utc)
    values (gen_random_uuid(), set3, 'Amygdalectomie — bloc B', 'PAT-2026-0481', (v_now)::date, 'Dr ORL',
            'Allocated', 'Boîte sortie pour le programme du jour.', v_now);

    -- Clinique Biasa : une boîte à retraiter
    if exists (select 1 from organization.organizations where id = v_clin) then
        insert into organization.instrument_sets
            (id, facility_id, code, name, description, category, item_count, status, created_on_utc)
        values (gen_random_uuid(), v_clin, 'SET-MAT-01', 'Boîte césarienne', 'Instrumentation césarienne', 'Obstétrique', 20,
                'Soiled', v_now - interval '2 days');
    end if;
end $$;

-- ============================================================
-- 4) Déchets DASRI — collectes pesées + bordereaux d'élimination
-- ============================================================
do $$
declare
    v_chu    constant uuid        := '0a000000-0000-4000-8000-000000000001';
    v_clin   constant uuid        := '0a000000-0000-4000-8000-000000000002';
    v_now    constant timestamptz := now();
    v_slip1  constant uuid        := '0f000000-0000-4000-8000-000000000001'; -- sentinelle Déchets
    slip2 uuid; slip3 uuid; slipb uuid;
    sp_bloc uuid; sp_urg uuid; sp_att uuid; sp_lab uuid; sp_bbloc uuid;
begin
    if not exists (select 1 from organization.organizations where id = v_chu) then return; end if;
    if exists (select 1 from organization.waste_elimination_slips where id = v_slip1) then return; end if;

    select id into sp_bloc from organization.spaces where facility_id = v_chu and code = 'B-RDC-BLOC';
    select id into sp_urg  from organization.spaces where facility_id = v_chu and code = 'B-RDC-URG';
    select id into sp_att  from organization.spaces where facility_id = v_chu and code = 'A-RDC-ATT';
    select id into sp_lab  from organization.spaces where facility_id = v_chu and code = 'C-RDC-LAB';

    -- Bordereaux
    insert into organization.waste_elimination_slips
        (id, facility_id, reference, stream, status, carrier, destination, treatment, total_weight_kg, shipped_on_utc, treated_on_utc, notes, created_on_utc, updated_on_utc)
    values
        (v_slip1, v_chu, 'BSD-2026-0001', 'Dasri', 'Treated', 'Edif Togo', 'Incinérateur régional de Lomé', 'Incineration',
         45.750, v_now - interval '3 days', v_now - interval '1 day', 'Enlèvement hebdomadaire.', v_now - interval '3 days', v_now - interval '1 day');
    slip2 := gen_random_uuid();
    insert into organization.waste_elimination_slips
        (id, facility_id, reference, stream, status, carrier, destination, treatment, total_weight_kg, shipped_on_utc, notes, created_on_utc, updated_on_utc)
    values
        (slip2, v_chu, 'BSD-2026-0002', 'Dasri', 'Shipped', 'Edif Togo', 'Incinérateur régional de Lomé', 'Incineration',
         32.400, v_now - interval '1 day', 'En transit vers la filière.', v_now - interval '1 day', v_now);
    slip3 := gen_random_uuid();
    insert into organization.waste_elimination_slips
        (id, facility_id, reference, stream, status, notes, created_on_utc)
    values
        (slip3, v_chu, 'BSD-2026-0003', 'Sharps', 'Draft', 'Regroupement des OPCT en cours.', v_now);

    -- Collectes rattachées au bordereau traité (BSD-0001) — éliminées
    insert into organization.waste_collections
        (id, facility_id, stream, weight_kg, container_count, status, space_id, slip_id, collected_on, collected_by, created_on_utc)
    values
        (gen_random_uuid(), v_chu, 'Dasri', 23.500, 4, 'Eliminated', sp_bloc, v_slip1, (v_now - interval '4 days')::date, 'Agent CSSD', v_now - interval '4 days'),
        (gen_random_uuid(), v_chu, 'Dasri', 22.250, 3, 'Eliminated', sp_urg,  v_slip1, (v_now - interval '4 days')::date, 'Agent CSSD', v_now - interval '4 days');

    -- Collectes rattachées au bordereau expédié (BSD-0002) — assignées
    insert into organization.waste_collections
        (id, facility_id, stream, weight_kg, container_count, status, space_id, slip_id, collected_on, collected_by, created_on_utc)
    values
        (gen_random_uuid(), v_chu, 'Dasri', 18.900, 3, 'Assigned', sp_bloc, slip2, (v_now - interval '2 days')::date, 'Agent CSSD', v_now - interval '2 days'),
        (gen_random_uuid(), v_chu, 'Dasri', 13.500, 2, 'Assigned', sp_lab,  slip2, (v_now - interval '2 days')::date, 'Agent CSSD', v_now - interval '2 days');

    -- Collectes non rattachées (disponibles) — collectées
    insert into organization.waste_collections
        (id, facility_id, stream, weight_kg, container_count, status, space_id, collected_on, collected_by, created_on_utc)
    values
        (gen_random_uuid(), v_chu, 'Dasri', 12.300, 2, 'Collected', sp_bloc, (v_now)::date, 'Agent CSSD', v_now),
        (gen_random_uuid(), v_chu, 'Sharps', 3.100, 1, 'Collected', sp_urg,  (v_now)::date, 'Agent CSSD', v_now),
        (gen_random_uuid(), v_chu, 'Daom',  20.000, 2, 'Collected', sp_att,  (v_now)::date, 'Agent d''entretien', v_now);

    -- Clinique Biasa : un bordereau brouillon + une collecte
    if exists (select 1 from organization.organizations where id = v_clin) then
        select id into sp_bbloc from organization.spaces where facility_id = v_clin and code = 'P-E1-BLOC';
        slipb := gen_random_uuid();
        insert into organization.waste_elimination_slips
            (id, facility_id, reference, stream, status, created_on_utc)
        values (slipb, v_clin, 'BSD-B-0001', 'Dasri', 'Draft', v_now);
        insert into organization.waste_collections
            (id, facility_id, stream, weight_kg, container_count, status, space_id, collected_on, collected_by, created_on_utc)
        values (gen_random_uuid(), v_clin, 'Dasri', 8.200, 1, 'Collected', sp_bbloc, (v_now)::date, 'Infirmière de bloc', v_now);
    end if;
end $$;
