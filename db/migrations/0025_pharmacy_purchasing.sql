-- Module Pharmacie — Phase 3 : fournisseurs et bons de commande.
-- Pas de FK cross-schéma : medication_id / facility_id référencés par identifiant applicatif.
-- Les lignes référencent leur bon de commande par FK (même schéma → autorisé).

create table if not exists pharmacy.suppliers (
    id             uuid        primary key,
    facility_id    uuid        not null,                    -- établissement propriétaire (cloisonnement multi-tenant)
    code           text        not null,                    -- code interne (unique par établissement)
    name           text        not null,
    contact_name   text        null,
    email          text        null,
    phone          text        null,
    address        text        null,
    active         boolean     not null default true,
    notes          text        null,
    created_on_utc timestamptz not null,
    updated_on_utc timestamptz null,
    constraint uq_suppliers_code unique (facility_id, code)
);

create index if not exists ix_suppliers_facility on pharmacy.suppliers (facility_id);
create index if not exists ix_suppliers_name on pharmacy.suppliers (name);

create table if not exists pharmacy.purchase_orders (
    id             uuid        primary key,
    facility_id    uuid        not null,                    -- établissement propriétaire (cloisonnement multi-tenant)
    supplier_id    uuid        not null references pharmacy.suppliers (id) on delete restrict,
    reference      text        not null,                    -- référence interne (unique par établissement)
    status         text        not null default 'Draft',    -- Draft | Ordered | PartiallyReceived | Received | Cancelled
    order_date     date        null,
    expected_on    date        null,
    notes          text        null,
    created_on_utc timestamptz not null,
    updated_on_utc timestamptz null,
    constraint ck_purchase_orders_status check (status in ('Draft', 'Ordered', 'PartiallyReceived', 'Received', 'Cancelled')),
    constraint uq_purchase_orders_reference unique (facility_id, reference)
);

create index if not exists ix_purchase_orders_facility on pharmacy.purchase_orders (facility_id);
create index if not exists ix_purchase_orders_supplier on pharmacy.purchase_orders (supplier_id);
create index if not exists ix_purchase_orders_status on pharmacy.purchase_orders (status);

create table if not exists pharmacy.purchase_order_lines (
    id                uuid        primary key,
    purchase_order_id uuid        not null references pharmacy.purchase_orders (id) on delete cascade,
    medication_id     uuid        not null,                 -- médicament commandé (pharmacy.medications)
    quantity_ordered  int         not null,
    quantity_received int         not null default 0,
    unit_price        numeric(14, 2) null,
    created_on_utc    timestamptz not null,
    updated_on_utc    timestamptz null,
    constraint ck_po_lines_qty_ordered check (quantity_ordered > 0),
    constraint ck_po_lines_qty_received check (quantity_received >= 0 and quantity_received <= quantity_ordered),
    constraint ck_po_lines_unit_price check (unit_price is null or unit_price >= 0)
);

create index if not exists ix_po_lines_order on pharmacy.purchase_order_lines (purchase_order_id);
create index if not exists ix_po_lines_medication on pharmacy.purchase_order_lines (medication_id);
