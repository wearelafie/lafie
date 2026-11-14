-- Module Organization — registre des équipements (actifs biomédicaux / techniques).
-- Rattaché à un établissement ; localisation (espace) et affectation (service / unité)
-- optionnelles. L'état opérationnel est une énumération applicative (EquipmentStatus).

create table if not exists organization.equipment (
    id                         uuid        primary key,
    facility_id                uuid        not null references organization.organizations (id),
    inventory_number           text        not null,
    serial_number              text        null,
    name                       text        not null,
    category                   text        not null,
    brand                      text        null,
    model                      text        null,
    acquisition_date           date        null,
    supplier                   text        null,
    warranty_end               date        null,
    estimated_lifespan_months  int         null,
    space_id                   uuid        null references organization.spaces (id) on delete set null,
    service_id                 uuid        null references organization.services (id) on delete set null,
    care_unit_id               uuid        null references organization.care_units (id) on delete set null,
    status                     text        not null default 'Operational',
    notes                      text        null,
    display_order              int         not null default 0,
    created_on_utc             timestamptz not null,
    updated_on_utc             timestamptz null,
    archived_on_utc            timestamptz null,
    constraint uq_equipment_facility_inventory unique (facility_id, inventory_number)
);

create index if not exists ix_equipment_facility on organization.equipment (facility_id);
create index if not exists ix_equipment_category on organization.equipment (category);
create index if not exists ix_equipment_status on organization.equipment (status);
create index if not exists ix_equipment_space on organization.equipment (space_id);
create index if not exists ix_equipment_service on organization.equipment (service_id);
create index if not exists ix_equipment_care_unit on organization.equipment (care_unit_id);
create index if not exists ix_equipment_archived on organization.equipment (archived_on_utc);
create index if not exists ix_equipment_warranty_end on organization.equipment (warranty_end);

-- Référentiel des catégories d'équipement (administrable ensuite via Référentiels).
insert into organization.reference_values (category, code, label_fr, label_en, display_order) values
    ('EQUIPMENT_CATEGORY', 'IMAGING_SCANNER',    'Scanner (TDM)',              'CT scanner',               1),
    ('EQUIPMENT_CATEGORY', 'IMAGING_MRI',        'IRM',                        'MRI',                      2),
    ('EQUIPMENT_CATEGORY', 'IMAGING_ULTRASOUND', 'Échographe',                 'Ultrasound',               3),
    ('EQUIPMENT_CATEGORY', 'IMAGING_XRAY',       'Radiographie',               'X-ray',                    4),
    ('EQUIPMENT_CATEGORY', 'MONITORING',         'Moniteur multiparamétrique', 'Patient monitor',          5),
    ('EQUIPMENT_CATEGORY', 'VENTILATOR',         'Respirateur',                'Ventilator',               6),
    ('EQUIPMENT_CATEGORY', 'DEFIBRILLATOR',      'Défibrillateur',             'Defibrillator',            7),
    ('EQUIPMENT_CATEGORY', 'SURGICAL',           'Équipement chirurgical',     'Surgical equipment',       8),
    ('EQUIPMENT_CATEGORY', 'MEDICAL_BED',        'Lit médicalisé',             'Medical bed',              9),
    ('EQUIPMENT_CATEGORY', 'WHEELCHAIR',         'Fauteuil roulant',           'Wheelchair',              10),
    ('EQUIPMENT_CATEGORY', 'LABORATORY',         'Équipement de laboratoire',  'Laboratory equipment',    11),
    ('EQUIPMENT_CATEGORY', 'STERILIZATION',      'Stérilisation',              'Sterilization',           12),
    ('EQUIPMENT_CATEGORY', 'GENERATOR',          'Groupe électrogène',         'Power generator',         13),
    ('EQUIPMENT_CATEGORY', 'UPS',                'Onduleur',                   'UPS',                     14),
    ('EQUIPMENT_CATEGORY', 'IT_EQUIPMENT',       'Équipement informatique',    'IT equipment',            15),
    ('EQUIPMENT_CATEGORY', 'COLD_CHAIN',         'Chaîne du froid',            'Cold chain',              16),
    ('EQUIPMENT_CATEGORY', 'OTHER',              'Autre',                      'Other',                   99)
on conflict (category, code) do nothing;
