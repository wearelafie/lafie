import { del, getJson, postJson, putJson } from '@shared/api/http'

// Codes "suggérés" (deviendront des référentiels configurables). Le backend stocke du texte.
export const FACILITY_TYPES = [
  'HOSPITAL', 'UNIVERSITY_HOSPITAL', 'REGIONAL_HOSPITAL', 'DISTRICT_HOSPITAL',
  'CLINIC', 'MEDICAL_SOCIAL_CENTER', 'HEALTH_CENTER', 'DISPENSARY', 'SPECIALIZED', 'OTHER',
] as const
export const OWNERSHIP_TYPES = ['PUBLIC', 'PRIVATE_FOR_PROFIT', 'PRIVATE_NON_PROFIT', 'FAITH_BASED', 'OTHER'] as const
export const OPERATIONAL_STATUSES = ['Setup', 'Active', 'TemporarilyClosed', 'Suspended', 'PermanentlyClosed'] as const
export type OperationalStatus = (typeof OPERATIONAL_STATUSES)[number]

export type Organization = {
  id: string
  code: string
  officialName: string
  displayName: string | null
  abbreviation: string | null
  description: string | null
  facilityTypeCode: string
  ownershipTypeCode: string
  logoUrl: string | null
  websiteUrl: string | null
  establishedOn: string | null
  parentOrganizationId: string | null
  legalName: string | null
  legalEntityTypeCode: string | null
  registrationNumber: string | null
  nationalFacilityCode: string | null
  licenseNumber: string | null
  licenseIssuedOn: string | null
  licenseExpiresOn: string | null
  accreditationStatusCode: string | null
  taxIdentificationNumber: string | null
  countryCode: string
  regionCode: string | null
  prefectureCode: string | null
  communeCode: string | null
  locality: string | null
  districtOrNeighborhood: string | null
  streetAddress: string | null
  postalCode: string | null
  latitude: number | null
  longitude: number | null
  altitudeMeters: number | null
  locationAccuracy: string | null
  timeZoneId: string
  openingDate: string | null
  defaultCurrencyCode: string
  defaultLanguageCode: string
  emergencyServicesAvailable: boolean
  publicFacing: boolean
  operationalStatus: OperationalStatus
  configurationStatus: string
  phone: string | null
  email: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
  archivedOnUtc: string | null
}

export type PagedResult<T> = { items: T[]; page: number; pageSize: number; total: number; totalPages: number }
export type OrganizationFilter = { search?: string; facilityTypeCode?: string; operationalStatus?: string; includeArchived?: boolean; page?: number; pageSize?: number }

// Modèle d'écriture (aligné sur l'API). Champs optionnels à null/omission.
export type OrganizationInput = {
  code: string
  officialName: string
  facilityTypeCode: string
  ownershipTypeCode: string
  countryCode: string
  displayName?: string | null
  abbreviation?: string | null
  description?: string | null
  websiteUrl?: string | null
  establishedOn?: string | null
  legalName?: string | null
  legalEntityTypeCode?: string | null
  registrationNumber?: string | null
  nationalFacilityCode?: string | null
  licenseNumber?: string | null
  licenseIssuedOn?: string | null
  licenseExpiresOn?: string | null
  accreditationStatusCode?: string | null
  taxIdentificationNumber?: string | null
  regionCode?: string | null
  prefectureCode?: string | null
  communeCode?: string | null
  locality?: string | null
  districtOrNeighborhood?: string | null
  streetAddress?: string | null
  postalCode?: string | null
  latitude?: number | null
  longitude?: number | null
  timeZoneId?: string | null
  openingDate?: string | null
  defaultCurrencyCode?: string | null
  defaultLanguageCode?: string | null
  emergencyServicesAvailable: boolean
  publicFacing: boolean
  phone?: string | null
  email?: string | null
}

export const organizationKeys = {
  all: ['organizations'] as const,
  list: (f: OrganizationFilter) => ['organizations', 'list', f] as const,
}

export function getOrganizations(filter: OrganizationFilter): Promise<PagedResult<Organization>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.facilityTypeCode) q.set('type', filter.facilityTypeCode)
  if (filter.operationalStatus) q.set('status', filter.operationalStatus)
  if (filter.includeArchived) q.set('includeArchived', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<Organization>>(`/api/organizations?${q.toString()}`)
}

export function getOrganization(id: string): Promise<Organization> {
  return getJson<Organization>(`/api/organizations/${id}`)
}

export function createOrganization(input: OrganizationInput): Promise<{ id: string }> {
  return postJson<{ id: string }>('/api/organizations', input)
}

export async function updateOrganization(id: string, input: OrganizationInput): Promise<void> {
  await putJson(`/api/organizations/${id}`, input)
}

export async function setOrganizationStatus(id: string, operationalStatus: OperationalStatus): Promise<void> {
  await putJson(`/api/organizations/${id}/status`, { operationalStatus })
}

export async function archiveOrganization(id: string): Promise<void> { await postJson<void>(`/api/organizations/${id}/archive`, {}) }
export async function restoreOrganization(id: string): Promise<void> { await postJson<void>(`/api/organizations/${id}/restore`, {}) }
export async function deleteOrganization(id: string): Promise<void> { await del(`/api/organizations/${id}`) }
