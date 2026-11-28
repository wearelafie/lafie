-- Jeu de données de DÉVELOPPEMENT — module Pharmacie (stock & lots).
-- Idempotent : conflit sur clé primaire / unique ignoré. Réutilise les médicaments de dev_pharmacy.sql
-- (CHU = 0a..01). Illustre : stock confortable, rupture proche (low stock), lot proche péremption, stupéfiant.

-- Seuils de réapprovisionnement.
insert into pharmacy.stock_items (id, facility_id, medication_id, reorder_threshold, notes, created_on_utc, updated_on_utc)
values
    ('0e100000-0000-4000-8000-000000000001', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000001', 100, null, now(), null),  -- Paracétamol
    ('0e100000-0000-4000-8000-000000000002', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000002', 200, null, now(), null),  -- Amoxicilline (sera en alerte)
    ('0e100000-0000-4000-8000-000000000005', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000005', 10,  'Stupéfiant — seuil réglementaire.', now(), null)  -- Morphine
on conflict (facility_id, medication_id) do nothing;

-- Lots reçus.
insert into pharmacy.stock_lots
    (id, facility_id, medication_id, lot_number, expiry_on, quantity_initial, quantity_on_hand, unit_cost, supplier_name, received_on, notes, created_on_utc, updated_on_utc)
values
    -- Paracétamol : deux lots, stock confortable, un lot proche péremption (FEFO le consommera d'abord).
    ('0e200000-0000-4000-8000-000000000001', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000001', 'PARA-2512', '2026-12-20', 300, 300, 12.50, 'UBIPHARM', '2026-09-01', null, now(), null),
    ('0e200000-0000-4000-8000-000000000002', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000001', 'PARA-2706', '2027-06-30', 500, 500, 11.90, 'UBIPHARM', '2026-11-10', null, now(), null),
    -- Amoxicilline : lot unique faible → en alerte (50 <= seuil 200).
    ('0e200000-0000-4000-8000-000000000003', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000002', 'AMOX-2703', '2027-03-31', 400, 50, 28.00, 'CAMEG', '2026-08-15', null, now(), null),
    -- Morphine : stupéfiant, lot unique.
    ('0e200000-0000-4000-8000-000000000005', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000005', 'MORPH-2801', '2028-01-31', 40, 40, 95.00, 'CAMEG', '2026-10-05', null, now(), null)
on conflict (id) do nothing;

-- Mouvements d'entrée correspondants (grand livre).
insert into pharmacy.stock_movements
    (id, facility_id, medication_id, lot_id, type, quantity, reason, performed_by, occurred_on_utc, notes)
values
    ('0e300000-0000-4000-8000-000000000001', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000001', '0e200000-0000-4000-8000-000000000001', 'Receipt', 300, null, 'seed', now(), null),
    ('0e300000-0000-4000-8000-000000000002', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000001', '0e200000-0000-4000-8000-000000000002', 'Receipt', 500, null, 'seed', now(), null),
    ('0e300000-0000-4000-8000-000000000003', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000002', '0e200000-0000-4000-8000-000000000003', 'Receipt', 400, null, 'seed', now(), null),
    ('0e300000-0000-4000-8000-000000000005', '0a000000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000005', '0e200000-0000-4000-8000-000000000005', 'Receipt', 40,  null, 'seed', now(), null)
on conflict (id) do nothing;
