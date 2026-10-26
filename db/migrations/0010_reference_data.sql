-- Données de référence du module Organization : listes de codes configurables + divisions géographiques.

-- 1) Listes de codes génériques (catégorie + code + libellés FR/EN).
create table if not exists organization.reference_values (
    category      text    not null,
    code          text    not null,
    label_fr      text    not null,
    label_en      text    not null,
    display_order int     not null default 0,
    active        boolean not null default true,
    primary key (category, code)
);

create index if not exists ix_reference_values_category on organization.reference_values (category);

insert into organization.reference_values (category, code, label_fr, label_en, display_order) values
    -- Types d'établissement
    ('FACILITY_TYPE', 'HOSPITAL',             'Hôpital',                  'Hospital',              1),
    ('FACILITY_TYPE', 'UNIVERSITY_HOSPITAL',  'Hôpital universitaire',    'University hospital',   2),
    ('FACILITY_TYPE', 'REGIONAL_HOSPITAL',    'Hôpital régional',         'Regional hospital',     3),
    ('FACILITY_TYPE', 'DISTRICT_HOSPITAL',    'Hôpital de district',      'District hospital',     4),
    ('FACILITY_TYPE', 'CLINIC',               'Clinique',                 'Clinic',                5),
    ('FACILITY_TYPE', 'MEDICAL_SOCIAL_CENTER','Centre médico-social',     'Medical-social center', 6),
    ('FACILITY_TYPE', 'HEALTH_CENTER',        'Centre de santé',          'Health center',         7),
    ('FACILITY_TYPE', 'DISPENSARY',           'Dispensaire',              'Dispensary',            8),
    ('FACILITY_TYPE', 'SPECIALIZED',          'Établissement spécialisé', 'Specialized facility',  9),
    ('FACILITY_TYPE', 'OTHER',                'Autre',                    'Other',                 99),
    -- Types de propriété
    ('OWNERSHIP_TYPE', 'PUBLIC',             'Public',             'Public',             1),
    ('OWNERSHIP_TYPE', 'PRIVATE_FOR_PROFIT', 'Privé lucratif',     'Private for-profit', 2),
    ('OWNERSHIP_TYPE', 'PRIVATE_NON_PROFIT', 'Privé non lucratif', 'Private non-profit', 3),
    ('OWNERSHIP_TYPE', 'FAITH_BASED',        'Confessionnel',      'Faith-based',        4),
    ('OWNERSHIP_TYPE', 'OTHER',              'Autre',              'Other',              99),
    -- Formes juridiques
    ('LEGAL_ENTITY_TYPE', 'SA',          'Société anonyme',               'Public limited company',   1),
    ('LEGAL_ENTITY_TYPE', 'SARL',        'SARL',                          'Limited liability company',2),
    ('LEGAL_ENTITY_TYPE', 'GIE',         'Groupement d''intérêt économique','Economic interest group',3),
    ('LEGAL_ENTITY_TYPE', 'ASSOCIATION', 'Association',                   'Association',              4),
    ('LEGAL_ENTITY_TYPE', 'FOUNDATION',  'Fondation',                     'Foundation',              5),
    ('LEGAL_ENTITY_TYPE', 'PUBLIC_ESTABLISHMENT', 'Établissement public', 'Public establishment',    6),
    ('LEGAL_ENTITY_TYPE', 'INDIVIDUAL',  'Entreprise individuelle',       'Sole proprietorship',     7),
    ('LEGAL_ENTITY_TYPE', 'OTHER',       'Autre',                         'Other',                   99),
    -- Statuts d'accréditation
    ('ACCREDITATION_STATUS', 'NOT_ACCREDITED', 'Non accrédité', 'Not accredited', 1),
    ('ACCREDITATION_STATUS', 'IN_PROGRESS',    'En cours',      'In progress',    2),
    ('ACCREDITATION_STATUS', 'ACCREDITED',     'Accrédité',     'Accredited',     3),
    ('ACCREDITATION_STATUS', 'SUSPENDED',      'Suspendu',      'Suspended',      4),
    ('ACCREDITATION_STATUS', 'EXPIRED',        'Expiré',        'Expired',        5);

-- 2) Divisions administratives (arbre : pays → région → préfecture → commune).
create table if not exists organization.geo_divisions (
    id            uuid primary key,
    level         text not null,          -- Country | Region | Prefecture | Commune
    code          text not null unique,
    name          text not null,
    parent_id     uuid null references organization.geo_divisions (id),
    display_order int  not null default 0
);

create index if not exists ix_geo_divisions_level on organization.geo_divisions (level);
create index if not exists ix_geo_divisions_parent on organization.geo_divisions (parent_id);

-- Pays
insert into organization.geo_divisions (id, level, code, name, parent_id, display_order)
values (gen_random_uuid(), 'Country', 'TG', 'Togo', null, 1);

-- Régions (codes ISO 3166-2:TG)
insert into organization.geo_divisions (id, level, code, name, parent_id, display_order)
select gen_random_uuid(), 'Region', r.code, r.name, (select id from organization.geo_divisions where code = 'TG'), r.ord
from (values
    ('TG-M', 'Maritime', 1),
    ('TG-P', 'Plateaux', 2),
    ('TG-C', 'Centrale', 3),
    ('TG-K', 'Kara',     4),
    ('TG-S', 'Savanes',  5)
) as r(code, name, ord);

-- Préfectures (39) rattachées à leur région par code.
insert into organization.geo_divisions (id, level, code, name, parent_id, display_order)
select gen_random_uuid(), 'Prefecture', p.code, p.name,
       (select id from organization.geo_divisions where code = p.region), p.ord
from (values
    -- Maritime
    ('M-GOLFE', 'Golfe', 'TG-M', 1), ('M-AGOE', 'Agoè-Nyivé', 'TG-M', 2), ('M-LACS', 'Lacs', 'TG-M', 3),
    ('M-VO', 'Vo', 'TG-M', 4), ('M-YOTO', 'Yoto', 'TG-M', 5), ('M-ZIO', 'Zio', 'TG-M', 6),
    ('M-AVE', 'Avé', 'TG-M', 7), ('M-BASMONO', 'Bas-Mono', 'TG-M', 8),
    -- Plateaux
    ('P-OGOU', 'Ogou', 'TG-P', 1), ('P-ESTMONO', 'Est-Mono', 'TG-P', 2), ('P-MOYENMONO', 'Moyen-Mono', 'TG-P', 3),
    ('P-HAHO', 'Haho', 'TG-P', 4), ('P-KLOTO', 'Kloto', 'TG-P', 5), ('P-AGOU', 'Agou', 'TG-P', 6),
    ('P-DANYI', 'Danyi', 'TG-P', 7), ('P-KPELE', 'Kpélé', 'TG-P', 8), ('P-AMOU', 'Amou', 'TG-P', 9),
    ('P-WAWA', 'Wawa', 'TG-P', 10), ('P-AKEBOU', 'Akébou', 'TG-P', 11), ('P-ANIE', 'Anié', 'TG-P', 12),
    -- Centrale
    ('C-TCHAOUDJO', 'Tchaoudjo', 'TG-C', 1), ('C-TCHAMBA', 'Tchamba', 'TG-C', 2), ('C-SOTOUBOUA', 'Sotouboua', 'TG-C', 3),
    ('C-BLITTA', 'Blitta', 'TG-C', 4), ('C-MO', 'Mô', 'TG-C', 5),
    -- Kara
    ('K-KOZAH', 'Kozah', 'TG-K', 1), ('K-BINAH', 'Binah', 'TG-K', 2), ('K-DOUFELGOU', 'Doufelgou', 'TG-K', 3),
    ('K-KERAN', 'Kéran', 'TG-K', 4), ('K-ASSOLI', 'Assoli', 'TG-K', 5), ('K-BASSAR', 'Bassar', 'TG-K', 6),
    ('K-DANKPEN', 'Dankpen', 'TG-K', 7),
    -- Savanes
    ('S-TONE', 'Tône', 'TG-S', 1), ('S-OTI', 'Oti', 'TG-S', 2), ('S-OTISUD', 'Oti-Sud', 'TG-S', 3),
    ('S-KPENDJAL', 'Kpendjal', 'TG-S', 4), ('S-KPENDJALOUEST', 'Kpendjal-Ouest', 'TG-S', 5),
    ('S-TANDJOARE', 'Tandjoaré', 'TG-S', 6), ('S-CINKASSE', 'Cinkassé', 'TG-S', 7)
) as p(code, name, region, ord);
