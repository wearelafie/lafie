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
