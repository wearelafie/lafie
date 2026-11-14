import { del, getJson, postJson, putJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

export const EQUIPMENT_STATUSES = ['Operational', 'UnderMaintenance', 'OutOfService', 'Decommissioned'] as const
export type EquipmentStatus = (typeof EQUIPMENT_STATUSES)[number]

/** Catégorie de référence administrable (Référentiels). */
export const EQUIPMENT_CATEGORY_REFERENCE = 'EQUIPMENT_CATEGORY'

export type Equipment = {
  id: string
  facilityId: string
  inventoryNumber: string
  serialNumber: string | null
  name: string
  category: string
  brand: string | null
  model: string | null
  acquisitionDate: string | null
  supplier: string | null
  warrantyEnd: string | null
  estimatedLifespanMonths: number | null
  spaceId: string | null
  spaceName: string | null
  serviceId: string | null
  serviceName: string | null
  careUnitId: string | null
  careUnitName: string | null
  status: EquipmentStatus
  notes: string | null
  displayOrder: number
  createdOnUtc: string
  updatedOnUtc: string | null
  archivedOnUtc: string | null
}

export type EquipmentFilter = {
  search?: string
  category?: string
  status?: string
  spaceId?: string
  serviceId?: string
  includeArchived?: boolean
  page?: number
  pageSize?: number
}

export type EquipmentInput = {
  inventoryNumber: string
  serialNumber?: string | null
  name: string
  category: string
  brand?: string | null
  model?: string | null
  acquisitionDate?: string | null
  supplier?: string | null
  warrantyEnd?: string | null
  estimatedLifespanMonths?: number | null
  spaceId?: string | null
  serviceId?: string | null
  careUnitId?: string | null
  status?: string | null
  notes?: string | null
  displayOrder: number
}

export const equipmentKeys = {
  all: (facilityId: string) => ['equipment', facilityId] as const,
  list: (facilityId: string, f: EquipmentFilter) => ['equipment', facilityId, 'list', f] as const,
  item: (id: string) => ['equipment', 'item', id] as const,
}

export function getEquipmentList(facilityId: string, filter: EquipmentFilter): Promise<PagedResult<Equipment>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.category) q.set('category', filter.category)
  if (filter.status) q.set('status', filter.status)
  if (filter.spaceId) q.set('spaceId', filter.spaceId)
  if (filter.serviceId) q.set('serviceId', filter.serviceId)
  if (filter.includeArchived) q.set('includeArchived', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<Equipment>>(`/api/facilities/${facilityId}/equipment?${q.toString()}`)
}

export function getEquipmentItem(id: string): Promise<Equipment> {
  return getJson<Equipment>(`/api/equipment/${id}`)
}

export function createEquipment(facilityId: string, input: EquipmentInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/equipment`, input)
}

export function updateEquipment(id: string, input: EquipmentInput): Promise<void> {
  return putJson(`/api/equipment/${id}`, input)
}

export function setEquipmentStatus(id: string, status: EquipmentStatus): Promise<void> {
  return putJson(`/api/equipment/${id}/status`, { status })
}

export function deleteEquipment(id: string): Promise<void> {
  return del(`/api/equipment/${id}`)
}

// --- Phase 2 : déplacements ---

export type EquipmentMovement = {
  id: string
  equipmentId: string
  movedOn: string
  fromSpaceId: string | null
  fromSpaceName: string | null
  toSpaceId: string | null
  toSpaceName: string | null
  reason: string | null
  performedBy: string | null
  notes: string | null
  createdOnUtc: string
}

export type MovementInput = {
  movedOn: string
  fromSpaceId?: string | null
  toSpaceId?: string | null
  reason?: string | null
  performedBy?: string | null
  notes?: string | null
  updateCurrentLocation?: boolean
}

export const movementKeys = {
  all: (equipmentId: string) => ['equipment', equipmentId, 'movements'] as const,
}

export function getMovements(equipmentId: string, page = 1, pageSize = 50): Promise<PagedResult<EquipmentMovement>> {
  return getJson<PagedResult<EquipmentMovement>>(`/api/equipment/${equipmentId}/movements?page=${page}&pageSize=${pageSize}`)
}

export function createMovement(equipmentId: string, input: MovementInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/equipment/${equipmentId}/movements`, input)
}

export function deleteMovement(id: string): Promise<void> {
  return del(`/api/equipment/movements/${id}`)
}

// --- Phase 2 : maintenance ---

export const MAINTENANCE_TYPES = ['Preventive', 'Corrective'] as const
export type MaintenanceType = (typeof MAINTENANCE_TYPES)[number]

export const MAINTENANCE_STATUSES = ['Planned', 'InProgress', 'Completed', 'Cancelled'] as const
export type MaintenanceStatus = (typeof MAINTENANCE_STATUSES)[number]

export type EquipmentMaintenance = {
  id: string
  equipmentId: string
  type: MaintenanceType
  status: MaintenanceStatus
  scheduledOn: string | null
  performedOn: string | null
  provider: string | null
  cost: number | null
  description: string
  notes: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
}

export type MaintenanceFilter = { type?: string; status?: string; page?: number; pageSize?: number }
export type MaintenanceInput = {
  type: string
  status?: string | null
  scheduledOn?: string | null
  performedOn?: string | null
  provider?: string | null
  cost?: number | null
  description: string
  notes?: string | null
}

export const maintenanceKeys = {
  all: (equipmentId: string) => ['equipment', equipmentId, 'maintenance'] as const,
}

export function getMaintenance(equipmentId: string, filter: MaintenanceFilter = {}): Promise<PagedResult<EquipmentMaintenance>> {
  const q = new URLSearchParams()
  if (filter.type) q.set('type', filter.type)
  if (filter.status) q.set('status', filter.status)
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 50))
  return getJson<PagedResult<EquipmentMaintenance>>(`/api/equipment/${equipmentId}/maintenance?${q.toString()}`)
}

export function createMaintenance(equipmentId: string, input: MaintenanceInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/equipment/${equipmentId}/maintenance`, input)
}

export function updateMaintenance(id: string, input: Omit<MaintenanceInput, 'status'>): Promise<void> {
  return putJson(`/api/equipment/maintenance/${id}`, input)
}

export function setMaintenanceStatus(id: string, status: MaintenanceStatus): Promise<void> {
  return putJson(`/api/equipment/maintenance/${id}/status`, { status })
}

export function deleteMaintenance(id: string): Promise<void> {
  return del(`/api/equipment/maintenance/${id}`)
}
