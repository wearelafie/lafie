-- Module Pharmacie — Phase 2 : stock, lots (FEFO) et grand livre des mouvements.
-- Pas de FK cross-schéma : medication_id référence pharmacy.medications par identifiant applicatif.

-- Fiche de stock par (établissement, médicament) : porte le seuil de réapprovisionnement.
-- La quantité en main n'est PAS stockée ici — elle est la somme des lots actifs.
create table if not exists pharmacy.stock_items (
    id                uuid        primary key,
    facility_id       uuid        not null,                   -- établissement propriétaire (cloisonnement multi-tenant)
    medication_id     uuid        not null,                   -- médicament concerné (pharmacy.medications)
    reorder_threshold int         not null default 0,         -- seuil d'alerte réappro (0 = pas d'alerte)
    notes             text        null,
    created_on_utc    timestamptz not null,
    updated_on_utc    timestamptz null,
    constraint ck_stock_items_threshold check (reorder_threshold >= 0),
    constraint uq_stock_items_medication unique (facility_id, medication_id)
);

create index if not exists ix_stock_items_facility on pharmacy.stock_items (facility_id);

-- Lot (batch) reçu en stock : numéro de lot, péremption, quantité restante. Sortie FEFO par péremption.
create table if not exists pharmacy.stock_lots (
    id                uuid        primary key,
    facility_id       uuid        not null,                   -- établissement propriétaire (cloisonnement multi-tenant)
    medication_id     uuid        not null,                   -- médicament du lot (pharmacy.medications)
    lot_number        text        null,                       -- numéro de lot fournisseur
    expiry_on         date        null,                       -- date de péremption (null = non datée)
    quantity_initial  int         not null,                   -- quantité reçue à l'origine
    quantity_on_hand  int         not null,                   -- quantité restante
    unit_cost         numeric(14, 2) null,                    -- coût unitaire d'achat
    supplier_name     text        null,
    received_on       date        not null,                   -- date de réception
    notes             text        null,
    created_on_utc    timestamptz not null,
    updated_on_utc    timestamptz null,
    constraint ck_stock_lots_qty_initial check (quantity_initial > 0),
    constraint ck_stock_lots_qty_on_hand check (quantity_on_hand >= 0),
    constraint ck_stock_lots_unit_cost check (unit_cost is null or unit_cost >= 0)
);

create index if not exists ix_stock_lots_facility on pharmacy.stock_lots (facility_id);
create index if not exists ix_stock_lots_medication on pharmacy.stock_lots (facility_id, medication_id);
-- FEFO : lots actifs d'un médicament par péremption croissante.
create index if not exists ix_stock_lots_fefo on pharmacy.stock_lots (facility_id, medication_id, expiry_on)
    where quantity_on_hand > 0;

-- Grand livre immuable des mouvements de stock : quantité signée (+ en entrée, − en sortie).
create table if not exists pharmacy.stock_movements (
    id                uuid        primary key,
    facility_id       uuid        not null,                   -- établissement propriétaire (cloisonnement multi-tenant)
    medication_id     uuid        not null,                   -- médicament concerné (pharmacy.medications)
    lot_id            uuid        null,                       -- lot touché (pharmacy.stock_lots), null si non imputé
    type              text        not null,                   -- Receipt | Issue | Adjustment | Disposal
    quantity          int         not null,                   -- signée : positive en entrée, négative en sortie
    reason            text        null,
    performed_by      text        null,
    occurred_on_utc   timestamptz not null,
    notes             text        null,
    constraint ck_stock_movements_type check (type in ('Receipt', 'Issue', 'Adjustment', 'Disposal'))
);

create index if not exists ix_stock_movements_facility on pharmacy.stock_movements (facility_id);
create index if not exists ix_stock_movements_medication on pharmacy.stock_movements (facility_id, medication_id, occurred_on_utc desc);
create index if not exists ix_stock_movements_lot on pharmacy.stock_movements (lot_id);
