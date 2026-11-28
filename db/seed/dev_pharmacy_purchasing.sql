-- Jeu de données de DÉVELOPPEMENT — module Pharmacie (fournisseurs & bons de commande).
-- Idempotent (conflit clé primaire/unique ignoré). Réutilise CHU (0a..01) et ses médicaments (dev_pharmacy.sql).

insert into pharmacy.suppliers (id, facility_id, code, name, contact_name, email, phone, address, active, notes, created_on_utc, updated_on_utc)
values
    ('0f100000-0000-4000-8000-000000000001', '0a000000-0000-4000-8000-000000000001', 'FOUR-UBIPHARM', 'UBIPHARM Togo', 'Kodjo Mensah', 'contact@ubipharm.tg', '+228 22 21 00 00', 'Zone portuaire, Lomé', true, null, now(), null),
    ('0f100000-0000-4000-8000-000000000002', '0a000000-0000-4000-8000-000000000001', 'FOUR-CAMEG',    'CAMEG Togo',     'Afi Dossou',   'achats@cameg.tg',    '+228 22 25 11 22', 'Boulevard du Mono, Lomé', true, null, now(), null)
on conflict (facility_id, code) do nothing;

-- Bon de commande COMMANDÉ (réceptionnable) avec deux lignes.
insert into pharmacy.purchase_orders (id, facility_id, supplier_id, reference, status, order_date, expected_on, notes, created_on_utc, updated_on_utc)
values
    ('0f200000-0000-4000-8000-000000000001', '0a000000-0000-4000-8000-000000000001', '0f100000-0000-4000-8000-000000000001', 'BC-2026-001', 'Ordered', '2026-11-20', '2026-12-05', 'Réapprovisionnement trimestriel.', now(), null)
on conflict (facility_id, reference) do nothing;

insert into pharmacy.purchase_order_lines (id, purchase_order_id, medication_id, quantity_ordered, quantity_received, unit_price, created_on_utc, updated_on_utc)
values
    ('0f300000-0000-4000-8000-000000000001', '0f200000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000001', 1000, 0, 10.00, now(), null),
    ('0f300000-0000-4000-8000-000000000002', '0f200000-0000-4000-8000-000000000001', '0d000000-0000-4000-8000-000000000002',  500, 0, 28.00, now(), null)
on conflict (id) do nothing;
