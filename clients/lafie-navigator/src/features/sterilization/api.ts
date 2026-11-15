import { del, getJson, postJson, putJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

// --- Boîtes d'instruments ---

export const SET_STATUSES = ['Sterile', 'InUse', 'Soiled', 'InSterilization', 'Quarantine', 'Retired'] as const
export type InstrumentSetStatus = (typeof SET_STATUSES)[number]

export type InstrumentSet = {
  id: string
  facilityId: string
  code: string
  name: string
  description: string | null
  category: string | null
  itemCount: number
  status: InstrumentSetStatus
  lastCycleId: string | null
  sterilizedOnUtc: string | null
  expiryOn: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
}

export const USAGE_STATUSES = ['Allocated', 'Used', 'Returned', 'Cancelled'] as const
export type SetUsageStatus = (typeof USAGE_STATUSES)[number]

export type InstrumentSetUsage = {
  id: string
  setId: string
  interventionLabel: string
  patientRef: string | null
  scheduledOn: string | null
  performedOnUtc: string | null
  performedBy: string | null
  status: SetUsageStatus
  notes: string | null
  createdOnUtc: string
}

export type InstrumentSetDetail = {
  set: InstrumentSet
  usages: InstrumentSetUsage[]
}

export type InstrumentSetFilter = {
  search?: string
  status?: string
  category?: string
  page?: number
  pageSize?: number
}

export type CreateInstrumentSetInput = {
  code: string
  name: string
  description?: string | null
  category?: string | null
  itemCount: number
}

export type UpdateInstrumentSetInput = {
  name: string
  description?: string | null
  category?: string | null
  itemCount: number
}

export type AllocateSetInput = {
  interventionLabel: string
  patientRef?: string | null
  scheduledOn?: string | null
  performedBy?: string | null
  notes?: string | null
}

export type UsageAction = 'use' | 'return' | 'cancel'

// --- Cycles d'autoclave ---

export const CYCLE_STATUSES = ['Planned', 'Running', 'Passed', 'Failed'] as const
export type SterilizationCycleStatus = (typeof CYCLE_STATUSES)[number]

export type SterilizationCycle = {
  id: string
  facilityId: string
  autoclave: string
  cycleNumber: string | null
  program: string
  status: SterilizationCycleStatus
  operator: string | null
  temperatureC: number | null
  pressureBar: number | null
  durationMin: number | null
  startedOnUtc: string | null
  endedOnUtc: string | null
  resultNotes: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
  loadCount: number
}

export type CycleSet = {
  setId: string
  code: string
  name: string
  status: InstrumentSetStatus
}

export type SterilizationCycleDetail = {
  cycle: SterilizationCycle
  load: CycleSet[]
}

export type SterilizationCycleFilter = {
  search?: string
  status?: string
  openOnly?: boolean
  page?: number
  pageSize?: number
}

export type CreateCycleInput = {
  autoclave: string
  cycleNumber?: string | null
  program: string
  operator?: string | null
}

export type CloseCycleInput = {
  passed: boolean
  temperatureC?: number | null
  pressureBar?: number | null
  durationMin?: number | null
  resultNotes?: string | null
}

// --- Query keys ---

export const setKeys = {
  all: (facilityId: string) => ['instrument-sets', facilityId] as const,
  list: (facilityId: string, f: InstrumentSetFilter) => ['instrument-sets', facilityId, 'list', f] as const,
  detail: (id: string) => ['instrument-sets', 'detail', id] as const,
}

export const cycleKeys = {
  all: (facilityId: string) => ['sterilization-cycles', facilityId] as const,
  list: (facilityId: string, f: SterilizationCycleFilter) => ['sterilization-cycles', facilityId, 'list', f] as const,
  detail: (id: string) => ['sterilization-cycles', 'detail', id] as const,
}

// --- Instrument sets API ---

export function getInstrumentSets(facilityId: string, filter: InstrumentSetFilter): Promise<PagedResult<InstrumentSet>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.status) q.set('status', filter.status)
  if (filter.category) q.set('category', filter.category)
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<InstrumentSet>>(`/api/facilities/${facilityId}/sterilization/instrument-sets?${q.toString()}`)
}

export function getInstrumentSet(id: string): Promise<InstrumentSetDetail> {
  return getJson<InstrumentSetDetail>(`/api/sterilization/instrument-sets/${id}`)
}

export function createInstrumentSet(facilityId: string, input: CreateInstrumentSetInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/sterilization/instrument-sets`, input)
}

export function updateInstrumentSet(id: string, input: UpdateInstrumentSetInput): Promise<void> {
  return putJson(`/api/sterilization/instrument-sets/${id}`, input)
}

export function retireInstrumentSet(id: string): Promise<void> {
  return postJson<void>(`/api/sterilization/instrument-sets/${id}/retire`, {})
}

export function allocateSet(id: string, input: AllocateSetInput): Promise<{ usageId: string }> {
  return postJson<{ usageId: string }>(`/api/sterilization/instrument-sets/${id}/allocate`, input)
}

export function transitionUsage(usageId: string, action: UsageAction): Promise<void> {
  return postJson<void>(`/api/sterilization/instrument-sets/usages/${usageId}/transition`, { action })
}

// --- Cycles API ---

export function getCycles(facilityId: string, filter: SterilizationCycleFilter): Promise<PagedResult<SterilizationCycle>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.status) q.set('status', filter.status)
  if (filter.openOnly) q.set('openOnly', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<SterilizationCycle>>(`/api/facilities/${facilityId}/sterilization/cycles?${q.toString()}`)
}

export function getCycle(id: string): Promise<SterilizationCycleDetail> {
  return getJson<SterilizationCycleDetail>(`/api/sterilization/cycles/${id}`)
}

export function createCycle(facilityId: string, input: CreateCycleInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/sterilization/cycles`, input)
}

export function addSetToCycle(cycleId: string, setId: string): Promise<void> {
  return postJson<void>(`/api/sterilization/cycles/${cycleId}/load`, { setId })
}

export function removeSetFromCycle(cycleId: string, setId: string): Promise<void> {
  return del(`/api/sterilization/cycles/${cycleId}/load/${setId}`)
}

export function startCycle(id: string): Promise<void> {
  return postJson<void>(`/api/sterilization/cycles/${id}/start`, {})
}

export function closeCycle(id: string, input: CloseCycleInput): Promise<void> {
  return postJson<void>(`/api/sterilization/cycles/${id}/close`, input)
}
