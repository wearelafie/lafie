-- Module Identity — permissions fines du module Pharmacie.
-- Catalogue : FK role_permissions.permission_code -> permissions.code, donc les codes doivent exister d'abord.
insert into identity.permissions (code, group_name, description) values
    ('pharmacy.read',     'pharmacy', 'Voir la pharmacie (catalogue, stock, dispensation)'),
    ('pharmacy.manage',   'pharmacy', 'Gérer le catalogue, le stock et les commandes de la pharmacie'),
    ('pharmacy.dispense', 'pharmacy', 'Dispenser / délivrer des médicaments aux patients')
on conflict (code) do nothing;

-- Admin établissement : accès complet à la pharmacie.
insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'pharmacy.read', 'pharmacy.manage', 'pharmacy.dispense'])
where r.code = 'FACILITY_ADMIN' and r.tenant_id is null
on conflict do nothing;

-- Pharmacien : accès complet métier à la pharmacie.
insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'pharmacy.read', 'pharmacy.manage', 'pharmacy.dispense'])
where r.code = 'PHARMACIST' and r.tenant_id is null
on conflict do nothing;

-- Prescripteurs / soignants : consultation du catalogue (prescription en phase ultérieure).
insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = 'pharmacy.read'
where r.code in ('DOCTOR', 'NURSE', 'MIDWIFE') and r.tenant_id is null
on conflict do nothing;
