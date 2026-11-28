import { del, getJson, postJson, putJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

// --- Catalogue médicaments ---

export const MEDICATION_FORMS = [
  'Tablet', 'Capsule', 'Syrup', 'Solution', 'Injectable', 'Infusion', 'Cream', 'Ointment',
  'Drops', 'Suppository', 'Inhaler', 'Patch', 'Powder', 'Other',
] as const
export type MedicationForm = (typeof MEDICATION_FORMS)[number]

export const MEDICATION_STATUSES = ['Active', 'Inactive'] as const
export type MedicationStatus = (typeof MEDICATION_STATUSES)[number]

export type Medication = {
  id: string
  facilityId: string
  code: string
  name: string
  inn: string | null
  form: MedicationForm
  strength: string | null
  unit: string
  atcCode: string | null
  therapeuticClass: string | null
  controlled: boolean
  status: MedicationStatus
  notes: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
}

export type MedicationFilter = {
  search?: string
  form?: string
  status?: string
  controlled?: boolean
  page?: number
  pageSize?: number
}

export type CreateMedicationInput = {
  code: string
  name: string
  inn?: string | null
  form: MedicationForm
  strength?: string | null
  unit: string
  atcCode?: string | null
  therapeuticClass?: string | null
  controlled: boolean
  notes?: string | null
}

export type UpdateMedicationInput = Omit<CreateMedicationInput, 'code'>

// --- Query keys ---

export const medicationKeys = {
  all: (facilityId: string) => ['medications', facilityId] as const,
  list: (facilityId: string, f: MedicationFilter) => ['medications', facilityId, 'list', f] as const,
  detail: (id: string) => ['medications', 'detail', id] as const,
}

// --- API ---

export function getMedications(facilityId: string, filter: MedicationFilter): Promise<PagedResult<Medication>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.form) q.set('form', filter.form)
  if (filter.status) q.set('status', filter.status)
  if (filter.controlled != null) q.set('controlled', String(filter.controlled))
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<Medication>>(`/api/facilities/${facilityId}/pharmacy/medications?${q.toString()}`)
}

export function getMedication(id: string): Promise<Medication> {
  return getJson<Medication>(`/api/pharmacy/medications/${id}`)
}

export function createMedication(facilityId: string, input: CreateMedicationInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/pharmacy/medications`, input)
}

export function updateMedication(id: string, input: UpdateMedicationInput): Promise<void> {
  return putJson(`/api/pharmacy/medications/${id}`, input)
}

export function setMedicationStatus(id: string, active: boolean): Promise<void> {
  return postJson<void>(`/api/pharmacy/medications/${id}/status`, { active })
}

// Réservé à une évolution ultérieure (le catalogue ne supprime pas, il déréférence).
export function deleteMedication(id: string): Promise<void> {
  return del(`/api/pharmacy/medications/${id}`)
}

// --- Stock & lots ---

export const STOCK_MOVEMENT_TYPES = ['Receipt', 'Issue', 'Adjustment', 'Disposal'] as const
export type StockMovementType = (typeof STOCK_MOVEMENT_TYPES)[number]

export type StockItem = {
  medicationId: string
  facilityId: string
  code: string
  name: string
  unit: string
  form: MedicationForm
  controlled: boolean
  onHand: number
  reorderThreshold: number
  lotCount: number
  earliestExpiry: string | null
  lowStock: boolean
}

export type StockLot = {
  id: string
  facilityId: string
  medicationId: string
  lotNumber: string | null
  expiryOn: string | null
  quantityInitial: number
  quantityOnHand: number
  unitCost: number | null
  supplierName: string | null
  receivedOn: string
  notes: string | null
  createdOnUtc: string
}

export type StockMovement = {
  id: string
  medicationId: string
  lotId: string | null
  lotNumber: string | null
  type: StockMovementType
  quantity: number
  reason: string | null
  performedBy: string | null
  occurredOnUtc: string
  notes: string | null
}

export type MedicationStock = {
  medication: Medication
  onHand: number
  reorderThreshold: number
  lots: StockLot[]
  movements: StockMovement[]
}

export type StockFilter = {
  search?: string
  low?: boolean
  page?: number
  pageSize?: number
}

export type ReceiveStockInput = {
  medicationId: string
  lotNumber?: string | null
  expiryOn?: string | null
  quantity: number
  unitCost?: number | null
  supplierName?: string | null
  receivedOn?: string | null
  notes?: string | null
  performedBy?: string | null
}

export type IssueStockInput = {
  medicationId: string
  quantity: number
  reason?: string | null
  performedBy?: string | null
  notes?: string | null
}

export const stockKeys = {
  all: (facilityId: string) => ['stock', facilityId] as const,
  list: (facilityId: string, f: StockFilter) => ['stock', facilityId, 'list', f] as const,
  detail: (facilityId: string, medicationId: string) => ['stock', facilityId, 'detail', medicationId] as const,
}

export function getStockItems(facilityId: string, filter: StockFilter): Promise<PagedResult<StockItem>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.low) q.set('low', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<StockItem>>(`/api/facilities/${facilityId}/pharmacy/stock?${q.toString()}`)
}

export function getMedicationStock(facilityId: string, medicationId: string): Promise<MedicationStock> {
  return getJson<MedicationStock>(`/api/facilities/${facilityId}/pharmacy/stock/medications/${medicationId}`)
}

export function receiveStock(facilityId: string, input: ReceiveStockInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/pharmacy/stock/receipts`, input)
}

export function issueStock(facilityId: string, input: IssueStockInput): Promise<void> {
  return postJson<void>(`/api/facilities/${facilityId}/pharmacy/stock/issues`, input)
}

export function setReorderThreshold(facilityId: string, medicationId: string, threshold: number, notes?: string | null): Promise<void> {
  return putJson(`/api/facilities/${facilityId}/pharmacy/stock/medications/${medicationId}/threshold`, { threshold, notes: notes ?? null })
}

export function adjustLot(lotId: string, newQuantityOnHand: number, reason?: string | null, performedBy?: string | null): Promise<void> {
  return postJson<void>(`/api/pharmacy/stock/lots/${lotId}/adjust`, { newQuantityOnHand, reason: reason ?? null, performedBy: performedBy ?? null })
}

// --- Fournisseurs ---

export type Supplier = {
  id: string
  facilityId: string
  code: string
  name: string
  contactName: string | null
  email: string | null
  phone: string | null
  address: string | null
  active: boolean
  notes: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
}

export type SupplierFilter = { search?: string; active?: boolean; page?: number; pageSize?: number }
export type SupplierInput = {
  code: string; name: string; contactName?: string | null; email?: string | null
  phone?: string | null; address?: string | null; notes?: string | null
}

export const supplierKeys = {
  all: (facilityId: string) => ['suppliers', facilityId] as const,
  list: (facilityId: string, f: SupplierFilter) => ['suppliers', facilityId, 'list', f] as const,
}

export function getSuppliers(facilityId: string, filter: SupplierFilter): Promise<PagedResult<Supplier>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.active != null) q.set('active', String(filter.active))
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<Supplier>>(`/api/facilities/${facilityId}/pharmacy/suppliers?${q.toString()}`)
}

export function createSupplier(facilityId: string, input: SupplierInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/pharmacy/suppliers`, input)
}

export function updateSupplier(id: string, input: Omit<SupplierInput, 'code'>): Promise<void> {
  return putJson(`/api/pharmacy/suppliers/${id}`, input)
}

export function setSupplierStatus(id: string, active: boolean): Promise<void> {
  return postJson<void>(`/api/pharmacy/suppliers/${id}/status`, { active })
}

// --- Bons de commande ---

export const PURCHASE_ORDER_STATUSES = ['Draft', 'Ordered', 'PartiallyReceived', 'Received', 'Cancelled'] as const
export type PurchaseOrderStatus = (typeof PURCHASE_ORDER_STATUSES)[number]

export type PurchaseOrder = {
  id: string
  facilityId: string
  supplierId: string
  supplierName: string
  reference: string
  status: PurchaseOrderStatus
  orderDate: string | null
  expectedOn: string | null
  lineCount: number
  totalOrdered: number
  totalReceived: number
  totalAmount: number
  notes: string | null
}

export type PurchaseOrderLine = {
  id: string
  purchaseOrderId: string
  medicationId: string
  medicationCode: string
  medicationName: string
  unit: string
  quantityOrdered: number
  quantityReceived: number
  unitPrice: number | null
}

export type PurchaseOrderDetail = { header: PurchaseOrder; lines: PurchaseOrderLine[] }

export type PurchaseOrderLineInput = { medicationId: string; quantityOrdered: number; unitPrice?: number | null }
export type PurchaseOrderFilter = { search?: string; status?: string; supplierId?: string; page?: number; pageSize?: number }
export type CreatePurchaseOrderInput = {
  supplierId: string; reference: string; orderDate?: string | null; expectedOn?: string | null
  notes?: string | null; lines: PurchaseOrderLineInput[]
}
export type ReceiveLineInput = {
  quantity: number; lotNumber?: string | null; expiryOn?: string | null; receivedOn?: string | null; performedBy?: string | null
}

export const purchaseOrderKeys = {
  all: (facilityId: string) => ['purchase-orders', facilityId] as const,
  list: (facilityId: string, f: PurchaseOrderFilter) => ['purchase-orders', facilityId, 'list', f] as const,
  detail: (id: string) => ['purchase-orders', 'detail', id] as const,
}

export function getPurchaseOrders(facilityId: string, filter: PurchaseOrderFilter): Promise<PagedResult<PurchaseOrder>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.status) q.set('status', filter.status)
  if (filter.supplierId) q.set('supplierId', filter.supplierId)
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<PurchaseOrder>>(`/api/facilities/${facilityId}/pharmacy/purchase-orders?${q.toString()}`)
}

export function getPurchaseOrder(id: string): Promise<PurchaseOrderDetail> {
  return getJson<PurchaseOrderDetail>(`/api/pharmacy/purchase-orders/${id}`)
}

export function createPurchaseOrder(facilityId: string, input: CreatePurchaseOrderInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/pharmacy/purchase-orders`, input)
}

export function updatePurchaseOrder(id: string, input: Omit<CreatePurchaseOrderInput, 'reference'>): Promise<void> {
  return putJson(`/api/pharmacy/purchase-orders/${id}`, input)
}

export function submitPurchaseOrder(id: string): Promise<void> {
  return postJson<void>(`/api/pharmacy/purchase-orders/${id}/submit`, {})
}

export function cancelPurchaseOrder(id: string): Promise<void> {
  return postJson<void>(`/api/pharmacy/purchase-orders/${id}/cancel`, {})
}

export function receivePurchaseOrderLine(orderId: string, lineId: string, input: ReceiveLineInput): Promise<void> {
  return postJson<void>(`/api/pharmacy/purchase-orders/${orderId}/lines/${lineId}/receive`, input)
}
