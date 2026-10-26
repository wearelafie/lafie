import { getJson, postJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'
import type { Space } from '@features/space/api'

export const CARE_UNIT_TYPES = [
  'Hospitalization', 'IntensiveCare', 'Resuscitation', 'Emergency', 'Maternity',
  'Ambulatory', 'OperatingTheatre', 'Consultation', 'DayHospital', 'Other',
] as const
export type CareUnitType = (typeof CARE_UNIT_TYPES)[number]

export const CARE_UNIT_STATUSES = ['Active', 'Inactive', 'Maintenance'] as const
export type CareUnitStatus = (typeof CARE_UNIT_STATUSES)[number]

export type CareUnit = {
  id: string
  facilityId: string
  serviceId: string
  code: string
  name: string
  type: CareUnitType
  bedCapacity: number | null
  description: string | null
  displayOrder: number
  status: CareUnitStatus
  createdOnUtc: string
  updatedOnUtc: string | null
  archivedOnUtc: string | null
}

export type CareUnitFilter = { search?: string; type?: string; status?: string; includeArchived?: boolean; page?: number; pageSize?: number }
export type CareUnitInput = {
  code: string
  name: string
  type: string
  bedCapacity?: number | null
  description?: string | null
  displayOrder: number
}

export const careUnitKeys = {
  all: (serviceId: string) => ['careUnits', serviceId] as const,
  list: (serviceId: string, f: CareUnitFilter) => ['careUnits', serviceId, 'list', f] as const,
  spaces: (id: string) => ['careUnits', 'spaces', id] as const,
}

export function getCareUnit(id: string): Promise<CareUnit> {
  return getJson<CareUnit>(`/api/care-units/${id}`)
}

export function getCareUnits(serviceId: string, filter: CareUnitFilter): Promise<PagedResult<CareUnit>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.type) q.set('type', filter.type)
  if (filter.status) q.set('status', filter.status)
  if (filter.includeArchived) q.set('includeArchived', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<CareUnit>>(`/api/services/${serviceId}/care-units?${q.toString()}`)
}

export function createCareUnit(serviceId: string, input: CareUnitInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/services/${serviceId}/care-units`, input)
}

export async function updateCareUnit(id: string, input: CareUnitInput): Promise<void> {
  await send(`/api/care-units/${id}`, 'PUT', input)
}

export async function setCareUnitStatus(id: string, status: CareUnitStatus): Promise<void> {
  await send(`/api/care-units/${id}/status`, 'PUT', { status })
}

// --- Association aux espaces (M:N) ---
export function getCareUnitSpaces(id: string): Promise<Space[]> {
  return getJson<Space[]>(`/api/care-units/${id}/spaces`)
}

export async function assignSpace(careUnitId: string, spaceId: string): Promise<void> {
  await send(`/api/care-units/${careUnitId}/spaces`, 'POST', { spaceId })
}

export async function unassignSpace(careUnitId: string, spaceId: string): Promise<void> {
  await send(`/api/care-units/${careUnitId}/spaces/${spaceId}`, 'DELETE', undefined)
}

/** Espaces de l'établissement (tous bâtiments/étages), pour le sélecteur d'association. */
export function getFacilitySpaces(facilityId: string, search?: string): Promise<PagedResult<Space>> {
  const q = new URLSearchParams()
  if (search) q.set('search', search)
  q.set('pageSize', '100')
  return getJson<PagedResult<Space>>(`/api/facilities/${facilityId}/spaces?${q.toString()}`)
}

async function send(url: string, method: string, body: unknown): Promise<void> {
  const res = await fetch(url, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!res.ok) {
    let message = `HTTP ${res.status}`
    try {
      const d = (await res.json()) as { error?: string; title?: string }
      message = d.error ?? d.title ?? message
    } catch { /* keep http code */ }
    throw new Error(message)
  }
}

export async function archiveCareUnit(id: string): Promise<void> { await send(`/api/care-units/${id}/archive`, 'POST', {}) }
export async function restoreCareUnit(id: string): Promise<void> { await send(`/api/care-units/${id}/restore`, 'POST', {}) }
export async function deleteCareUnit(id: string): Promise<void> {
  const res = await fetch(`/api/care-units/${id}`, { method: 'DELETE' })
  if (!res.ok) { let m = `HTTP ${res.status}`; try { const d = (await res.json()) as { error?: string }; m = d.error ?? m } catch { /* keep */ } throw new Error(m) }
}
