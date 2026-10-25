import { getJson, postJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

export const SPACE_TYPES = [
  'PatientRoom', 'ConsultationRoom', 'TreatmentRoom', 'EmergencyRoom', 'OperatingRoom',
  'RecoveryRoom', 'IntensiveCareRoom', 'Laboratory', 'ImagingRoom', 'Pharmacy',
  'Office', 'WaitingRoom', 'TechnicalRoom', 'Storage', 'Other',
] as const
export type SpaceType = (typeof SPACE_TYPES)[number]

export const SPACE_STATUSES = ['Active', 'Inactive', 'Maintenance'] as const
export type SpaceStatus = (typeof SPACE_STATUSES)[number]

export type Space = {
  id: string
  facilityId: string
  buildingId: string
  floorId: string
  code: string
  name: string
  type: SpaceType
  description: string | null
  area: number | null
  maxCapacity: number | null
  accessible: boolean
  status: SpaceStatus
  createdOnUtc: string
  updatedOnUtc: string | null
  archivedOnUtc: string | null
}

export type SpaceFilter = { search?: string; type?: string; status?: string; includeArchived?: boolean; page?: number; pageSize?: number }
export type SpaceInput = {
  code: string
  name: string
  type: SpaceType
  description?: string | null
  area?: number | null
  maxCapacity?: number | null
  accessible: boolean
}

export const spaceKeys = {
  all: (floorId: string) => ['spaces', floorId] as const,
  list: (floorId: string, f: SpaceFilter) => ['spaces', floorId, 'list', f] as const,
}

export function getSpace(id: string): Promise<Space> {
  return getJson<Space>(`/api/spaces/${id}`)
}

export function getSpaces(floorId: string, filter: SpaceFilter): Promise<PagedResult<Space>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.type) q.set('type', filter.type)
  if (filter.status) q.set('status', filter.status)
  if (filter.includeArchived) q.set('includeArchived', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<Space>>(`/api/floors/${floorId}/spaces?${q.toString()}`)
}

export function createSpace(floorId: string, input: SpaceInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/floors/${floorId}/spaces`, input)
}

export async function updateSpace(id: string, input: SpaceInput): Promise<void> {
  await send(`/api/spaces/${id}`, 'PUT', input)
}

export async function setSpaceStatus(id: string, status: SpaceStatus): Promise<void> {
  await send(`/api/spaces/${id}/status`, 'PUT', { status })
}

async function send(url: string, method: string, body: unknown): Promise<void> {
  const res = await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
  if (!res.ok) {
    let message = `HTTP ${res.status}`
    try {
      const d = (await res.json()) as { error?: string; title?: string }
      message = d.error ?? d.title ?? message
    } catch { /* keep http code */ }
    throw new Error(message)
  }
}

export async function archiveSpace(id: string): Promise<void> { await send(`/api/spaces/${id}/archive`, 'POST', {}) }
export async function restoreSpace(id: string): Promise<void> { await send(`/api/spaces/${id}/restore`, 'POST', {}) }
export async function deleteSpace(id: string): Promise<void> {
  const res = await fetch(`/api/spaces/${id}`, { method: 'DELETE' })
  if (!res.ok) { let m = `HTTP ${res.status}`; try { const d = (await res.json()) as { error?: string }; m = d.error ?? m } catch { /* keep */ } throw new Error(m) }
}
