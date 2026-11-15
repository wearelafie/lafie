import { del, getJson, postJson, putJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

// --- Filières & statuts ---

export const WASTE_STREAMS = ['Dasri', 'Sharps', 'Cytotoxic', 'Anatomical', 'Daom', 'Chemical', 'Pharmaceutical'] as const
export type WasteStream = (typeof WASTE_STREAMS)[number]

export const COLLECTION_STATUSES = ['Collected', 'Assigned', 'Eliminated', 'Cancelled'] as const
export type WasteCollectionStatus = (typeof COLLECTION_STATUSES)[number]

export const SLIP_STATUSES = ['Draft', 'Shipped', 'Treated', 'Cancelled'] as const
export type WasteSlipStatus = (typeof SLIP_STATUSES)[number]

export const WASTE_TREATMENTS = ['Incineration', 'Autoclaving', 'Landfill', 'Other'] as const
export type WasteTreatment = (typeof WASTE_TREATMENTS)[number]

// --- Collectes ---

export type WasteCollection = {
  id: string
  facilityId: string
  stream: WasteStream
  weightKg: number
  containerCount: number
  status: WasteCollectionStatus
  spaceId: string | null
  spaceName: string | null
  spaceCode: string | null
  slipId: string | null
  slipReference: string | null
  collectedOn: string | null
  collectedBy: string | null
  notes: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
}

export type WasteCollectionFilter = {
  search?: string
  stream?: string
  status?: string
  unassignedOnly?: boolean
  page?: number
  pageSize?: number
}

export type WasteCollectionInput = {
  stream: WasteStream
  weightKg: number
  containerCount: number
  spaceId?: string | null
  collectedOn?: string | null
  collectedBy?: string | null
  notes?: string | null
}

// --- Bordereaux ---

export type WasteSlip = {
  id: string
  facilityId: string
  reference: string
  stream: WasteStream
  status: WasteSlipStatus
  carrier: string | null
  destination: string | null
  treatment: WasteTreatment | null
  totalWeightKg: number
  shippedOnUtc: string | null
  treatedOnUtc: string | null
  notes: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
  collectionCount: number
  liveWeightKg: number
}

export type WasteSlipDetail = {
  slip: WasteSlip
  collections: WasteCollection[]
}

export type WasteSlipFilter = {
  search?: string
  stream?: string
  status?: string
  openOnly?: boolean
  page?: number
  pageSize?: number
}

export type CreateWasteSlipInput = {
  reference: string
  stream: WasteStream
  carrier?: string | null
  destination?: string | null
  treatment?: WasteTreatment | null
}

export type UpdateWasteSlipInput = {
  carrier?: string | null
  destination?: string | null
  treatment?: WasteTreatment | null
  notes?: string | null
}

// --- Query keys ---

export const collectionKeys = {
  all: (facilityId: string) => ['waste-collections', facilityId] as const,
  list: (facilityId: string, f: WasteCollectionFilter) => ['waste-collections', facilityId, 'list', f] as const,
}

export const slipKeys = {
  all: (facilityId: string) => ['waste-slips', facilityId] as const,
  list: (facilityId: string, f: WasteSlipFilter) => ['waste-slips', facilityId, 'list', f] as const,
  detail: (id: string) => ['waste-slips', 'detail', id] as const,
}

// --- Collections API ---

export function getWasteCollections(facilityId: string, filter: WasteCollectionFilter): Promise<PagedResult<WasteCollection>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.stream) q.set('stream', filter.stream)
  if (filter.status) q.set('status', filter.status)
  if (filter.unassignedOnly) q.set('unassignedOnly', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<WasteCollection>>(`/api/facilities/${facilityId}/waste/collections?${q.toString()}`)
}

export function createWasteCollection(facilityId: string, input: WasteCollectionInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/waste/collections`, input)
}

export function updateWasteCollection(id: string, input: WasteCollectionInput): Promise<void> {
  return putJson(`/api/waste/collections/${id}`, input)
}

export function cancelWasteCollection(id: string): Promise<void> {
  return postJson<void>(`/api/waste/collections/${id}/cancel`, {})
}

// --- Slips API ---

export function getWasteSlips(facilityId: string, filter: WasteSlipFilter): Promise<PagedResult<WasteSlip>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.stream) q.set('stream', filter.stream)
  if (filter.status) q.set('status', filter.status)
  if (filter.openOnly) q.set('openOnly', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<WasteSlip>>(`/api/facilities/${facilityId}/waste/slips?${q.toString()}`)
}

export function getWasteSlip(id: string): Promise<WasteSlipDetail> {
  return getJson<WasteSlipDetail>(`/api/waste/slips/${id}`)
}

export function createWasteSlip(facilityId: string, input: CreateWasteSlipInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/waste/slips`, input)
}

export function updateWasteSlip(id: string, input: UpdateWasteSlipInput): Promise<void> {
  return putJson(`/api/waste/slips/${id}`, input)
}

export function addCollectionToSlip(slipId: string, collectionId: string): Promise<void> {
  return postJson<void>(`/api/waste/slips/${slipId}/collections`, { collectionId })
}

export function removeCollectionFromSlip(slipId: string, collectionId: string): Promise<void> {
  return del(`/api/waste/slips/${slipId}/collections/${collectionId}`)
}

export function shipWasteSlip(id: string): Promise<void> {
  return postJson<void>(`/api/waste/slips/${id}/ship`, {})
}

export function treatWasteSlip(id: string, treatment: WasteTreatment | null): Promise<void> {
  return postJson<void>(`/api/waste/slips/${id}/treat`, { treatment })
}

export function cancelWasteSlip(id: string): Promise<void> {
  return postJson<void>(`/api/waste/slips/${id}/cancel`, {})
}
