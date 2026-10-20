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

export type Organization = {
  id: string
  name: string
  type: OrganizationType
  code: string | null
  active: boolean
  parentId: string | null
  createdOnUtc: string
}

export type CreateOrganizationInput = {
  name: string
  type: OrganizationType
  code?: string | null
  parentId?: string | null
}

export const organizationKeys = {
  all: ['organizations'] as const,
}

export function getOrganizations(): Promise<Organization[]> {
  return getJson<Organization[]>('/api/organizations')
}

export function createOrganization(input: CreateOrganizationInput): Promise<{ id: string }> {
  return postJson<{ id: string }>('/api/organizations', input)
}
