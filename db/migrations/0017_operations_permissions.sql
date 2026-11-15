-- Module Identity — permissions fines pour les services techniques (GMAO, hygiène, stérilisation, déchets).
-- Catalogue : FK role_permissions.permission_code -> permissions.code, donc les codes doivent exister d'abord.
insert into identity.permissions (code, group_name, description) values
    ('maintenance.read',         'maintenance',   'Voir la maintenance / GMAO'),
    ('maintenance.manage',       'maintenance',   'Gérer la maintenance, affecter les interventions'),
    ('maintenance.ticket.create','maintenance',   'Déclarer un incident technique'),
    ('hygiene.read',             'hygiene',       'Voir le bio-nettoyage'),
    ('hygiene.manage',           'hygiene',       'Gérer les tâches de bio-nettoyage'),
    ('sterilization.read',       'sterilization', 'Voir la stérilisation (CSSD)'),
    ('sterilization.manage',     'sterilization', 'Gérer les cycles et boîtes de stérilisation'),
    ('waste.read',               'waste',         'Voir les déchets biomédicaux (DASRI)'),
    ('waste.manage',             'waste',         'Gérer la collecte et l''élimination des déchets')
on conflict (code) do nothing;

-- Admin établissement : accès complet aux services techniques.
insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'maintenance.read','maintenance.manage','maintenance.ticket.create',
    'hygiene.read','hygiene.manage','sterilization.read','sterilization.manage',
    'waste.read','waste.manage'])
where r.code = 'FACILITY_ADMIN' and r.tenant_id is null
on conflict do nothing;

-- Soignants : peuvent déclarer un incident (panne constatée) et voir la maintenance.
insert into identity.role_permissions (role_id, permission_code)
select r.id, p.code from identity.roles r join identity.permissions p on p.code = any(array[
    'maintenance.read','maintenance.ticket.create','hygiene.read'])
where r.code in ('DOCTOR','NURSE','MIDWIFE') and r.tenant_id is null
on conflict do nothing;
