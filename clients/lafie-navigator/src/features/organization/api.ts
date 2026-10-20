import { getJson, postJson } from '@shared/api/http'

export const ORGANIZATION_TYPES = [
  'Hospital',
  'Clinic',
  'HealthCenter',
  'Department',
  'Unit',
  'Pharmacy',
  'Laboratory',
] as const

export type OrganizationType = (typeof ORGANIZATION_TYPES)[number]

export const ORGANIZATION_STATUSES = ['Draft', 'Active', 'Inactive'] as const
export type OrganizationStatus = (typeof ORGANIZATION_STATUSES)[number]

export type Organization = {
  id: string
  name: string
  type: OrganizationType
  code: string | null
  status: OrganizationStatus
  shortName: string | null
  description: string | null
  phone: string | null
  email: string | null
  timezone: string
  city: string | null
  country: string | null
  parentId: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
}

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  total: number
  totalPages: number
}

export type OrganizationFilter = {
  search?: string
  type?: string
  status?: string
  page?: number
  pageSize?: number
}

export type OrganizationInput = {
  name: string
  type: OrganizationType
  code?: string | null
  shortName?: string | null
  phone?: string | null
  email?: string | null
  city?: string | null
  country?: string | null
  description?: string | null
}

export const organizationKeys = {
  all: ['organizations'] as const,
  list: (f: OrganizationFilter) => ['organizations', 'list', f] as const,
}

export function getOrganizations(filter: OrganizationFilter): Promise<PagedResult<Organization>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.type) q.set('type', filter.type)
  if (filter.status) q.set('status', filter.status)
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<Organization>>(`/api/organizations?${q.toString()}`)
}

export function createOrganization(input: OrganizationInput): Promise<{ id: string }> {
  return postJson<{ id: string }>('/api/organizations', input)
}

export async function updateOrganization(id: string, input: OrganizationInput): Promise<void> {
  const res = await fetch(`/api/organizations/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!res.ok) throw new Error(await errorMessage(res))
}

export async function setOrganizationActive(id: string, active: boolean): Promise<void> {
  const res = await fetch(`/api/organizations/${id}/${active ? 'activate' : 'deactivate'}`, { method: 'POST' })
  if (!res.ok) throw new Error(await errorMessage(res))
}

async function errorMessage(res: Response): Promise<string> {
  try {
    const d = (await res.json()) as { error?: string; title?: string }
    return d.error ?? d.title ?? `HTTP ${res.status}`
  } catch {
    return `HTTP ${res.status}`
  }
}
