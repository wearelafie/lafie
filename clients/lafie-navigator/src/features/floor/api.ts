import { getJson, postJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

export const FLOOR_STATUSES = ['Active', 'Inactive', 'Maintenance'] as const
export type FloorStatus = (typeof FLOOR_STATUSES)[number]

export type Floor = {
  id: string
  buildingId: string
  code: string
  name: string
  level: number | null
  displayOrder: number
  description: string | null
  status: FloorStatus
  createdOnUtc: string
  updatedOnUtc: string | null
}

export type FloorFilter = { search?: string; status?: string; page?: number; pageSize?: number }
export type FloorInput = {
  code: string
  name: string
  level?: number | null
  displayOrder: number
  description?: string | null
}

export const floorKeys = {
  all: (buildingId: string) => ['floors', buildingId] as const,
  list: (buildingId: string, f: FloorFilter) => ['floors', buildingId, 'list', f] as const,
}

export function getFloors(buildingId: string, filter: FloorFilter): Promise<PagedResult<Floor>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.status) q.set('status', filter.status)
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<Floor>>(`/api/buildings/${buildingId}/floors?${q.toString()}`)
}

export function createFloor(buildingId: string, input: FloorInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/buildings/${buildingId}/floors`, input)
}

export async function updateFloor(id: string, input: FloorInput): Promise<void> {
  await send(`/api/floors/${id}`, 'PUT', input)
}

export async function setFloorStatus(id: string, status: FloorStatus): Promise<void> {
  await send(`/api/floors/${id}/status`, 'PUT', { status })
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
