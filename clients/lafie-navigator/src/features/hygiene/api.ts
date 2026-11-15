import { del, getJson, postJson, putJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

export const CLEANING_PROTOCOLS = ['Standard', 'Deep', 'Terminal'] as const
export type CleaningProtocol = (typeof CLEANING_PROTOCOLS)[number]

export const CLEANING_TRIGGERS = ['PostDischarge', 'Scheduled', 'OnDemand'] as const
export type CleaningTrigger = (typeof CLEANING_TRIGGERS)[number]

export const CLEANING_STATUSES = ['Pending', 'InProgress', 'Completed', 'Cancelled'] as const
export type CleaningStatus = (typeof CLEANING_STATUSES)[number]

export type CleaningTask = {
  id: string
  facilityId: string
  spaceId: string
  spaceName: string | null
  spaceCode: string | null
  protocol: CleaningProtocol
  trigger: CleaningTrigger
  status: CleaningStatus
  requestedBy: string | null
  assignedTo: string | null
  notes: string | null
  startedOnUtc: string | null
  completedOnUtc: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
  itemsTotal: number
  itemsDone: number
}

export type CleaningTaskItem = {
  id: string
  taskId: string
  label: string
  done: boolean
  doneOnUtc: string | null
  sortOrder: number
}

export type CleaningTaskDetail = {
  task: CleaningTask
  items: CleaningTaskItem[]
}

export type CleaningFilter = {
  search?: string
  status?: string
  protocol?: string
  spaceId?: string
  activeOnly?: boolean
  page?: number
  pageSize?: number
}

export type CreateCleaningInput = {
  spaceId: string
  protocol: CleaningProtocol
  trigger: CleaningTrigger
  notes?: string | null
}

export type CleaningAction = 'start' | 'complete' | 'cancel'

export const cleaningKeys = {
  all: (facilityId: string) => ['cleaning-tasks', facilityId] as const,
  list: (facilityId: string, f: CleaningFilter) => ['cleaning-tasks', facilityId, 'list', f] as const,
  detail: (id: string) => ['cleaning-tasks', 'detail', id] as const,
}

export function getCleaningTasks(facilityId: string, filter: CleaningFilter): Promise<PagedResult<CleaningTask>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.status) q.set('status', filter.status)
  if (filter.protocol) q.set('protocol', filter.protocol)
  if (filter.spaceId) q.set('spaceId', filter.spaceId)
  if (filter.activeOnly) q.set('activeOnly', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<CleaningTask>>(`/api/facilities/${facilityId}/hygiene/cleaning-tasks?${q.toString()}`)
}

export function getCleaningTask(id: string): Promise<CleaningTaskDetail> {
  return getJson<CleaningTaskDetail>(`/api/hygiene/cleaning-tasks/${id}`)
}

export function createCleaningTask(facilityId: string, input: CreateCleaningInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/hygiene/cleaning-tasks`, input)
}

export function assignCleaningTask(id: string, assignedTo: string): Promise<void> {
  return postJson<void>(`/api/hygiene/cleaning-tasks/${id}/assign`, { assignedTo })
}

export function transitionCleaningTask(id: string, action: CleaningAction): Promise<void> {
  return postJson<void>(`/api/hygiene/cleaning-tasks/${id}/transition`, { action })
}

export function addCleaningItem(id: string, label: string): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/hygiene/cleaning-tasks/${id}/items`, { label })
}

export function toggleCleaningItem(itemId: string, done: boolean): Promise<void> {
  return putJson(`/api/hygiene/cleaning-tasks/items/${itemId}`, { done })
}

export function deleteCleaningItem(itemId: string): Promise<void> {
  return del(`/api/hygiene/cleaning-tasks/items/${itemId}`)
}
