import { getJson, postJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

export const SERVICE_CATEGORIES = ['Clinical', 'MedicoTechnical', 'Administrative', 'Hosting'] as const
export type ServiceCategory = (typeof SERVICE_CATEGORIES)[number]

export const SERVICE_STATUSES = ['Active', 'Inactive'] as const
export type ServiceStatus = (typeof SERVICE_STATUSES)[number]

export type Service = {
  id: string
  facilityId: string
  parentServiceId: string | null
  code: string
  name: string
  category: ServiceCategory
  description: string | null
  head: string | null
  displayOrder: number
  status: ServiceStatus
  createdOnUtc: string
  updatedOnUtc: string | null
  archivedOnUtc: string | null
}

export type ServiceFilter = { search?: string; category?: string; status?: string; includeArchived?: boolean; page?: number; pageSize?: number }
export type ServiceInput = {
  parentServiceId?: string | null
  code: string
  name: string
  category: string
  description?: string | null
  head?: string | null
  displayOrder: number
}

export const serviceKeys = {
  all: (facilityId: string) => ['services', facilityId] as const,
  list: (facilityId: string, f: ServiceFilter) => ['services', facilityId, 'list', f] as const,
}

export function getService(id: string): Promise<Service> {
  return getJson<Service>(`/api/services/${id}`)
}

export function getServices(facilityId: string, filter: ServiceFilter): Promise<PagedResult<Service>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.category) q.set('category', filter.category)
  if (filter.status) q.set('status', filter.status)
  if (filter.includeArchived) q.set('includeArchived', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<Service>>(`/api/facilities/${facilityId}/services?${q.toString()}`)
}

export function createService(facilityId: string, input: ServiceInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/services`, input)
}

export async function updateService(id: string, input: ServiceInput): Promise<void> {
  await send(`/api/services/${id}`, 'PUT', input)
}

export async function setServiceStatus(id: string, status: ServiceStatus): Promise<void> {
  await send(`/api/services/${id}/status`, 'PUT', { status })
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

export async function archiveService(id: string): Promise<void> { await send(`/api/services/${id}/archive`, 'POST', {}) }
export async function restoreService(id: string): Promise<void> { await send(`/api/services/${id}/restore`, 'POST', {}) }
export async function deleteService(id: string): Promise<void> {
  const res = await fetch(`/api/services/${id}`, { method: 'DELETE' })
  if (!res.ok) { let m = `HTTP ${res.status}`; try { const d = (await res.json()) as { error?: string }; m = d.error ?? m } catch { /* keep */ } throw new Error(m) }
}
