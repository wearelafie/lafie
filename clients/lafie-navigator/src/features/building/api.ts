import { getJson, postJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

export const BUILDING_STATUSES = ['Active', 'Inactive', 'Maintenance'] as const
export type BuildingStatus = (typeof BUILDING_STATUSES)[number]

export type Building = {
  id: string
  facilityId: string
  code: string
  name: string
  description: string | null
  location: string | null
  status: BuildingStatus
  createdOnUtc: string
  updatedOnUtc: string | null
}

export type BuildingFilter = { search?: string; status?: string; page?: number; pageSize?: number }
export type BuildingInput = { code: string; name: string; description?: string | null; location?: string | null }

export const buildingKeys = {
  all: (facilityId: string) => ['buildings', facilityId] as const,
  list: (facilityId: string, f: BuildingFilter) => ['buildings', facilityId, 'list', f] as const,
}

export function getBuilding(id: string): Promise<Building> {
  return getJson<Building>(`/api/buildings/${id}`)
}

export function getBuildings(facilityId: string, filter: BuildingFilter): Promise<PagedResult<Building>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.status) q.set('status', filter.status)
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<Building>>(`/api/facilities/${facilityId}/buildings?${q.toString()}`)
}

export function createBuilding(facilityId: string, input: BuildingInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/buildings`, input)
}

export async function updateBuilding(id: string, input: BuildingInput): Promise<void> {
  await send(`/api/buildings/${id}`, 'PUT', input)
}

export async function setBuildingStatus(id: string, status: BuildingStatus): Promise<void> {
  await send(`/api/buildings/${id}/status`, 'PUT', { status })
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
