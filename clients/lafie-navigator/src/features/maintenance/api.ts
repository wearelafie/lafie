import { del, getJson, postJson, putJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

export const TICKET_TARGET_TYPES = ['Equipment', 'Space'] as const
export type TicketTargetType = (typeof TICKET_TARGET_TYPES)[number]

export const TICKET_CRITICALITIES = ['Low', 'Medium', 'High', 'Critical'] as const
export type TicketCriticality = (typeof TICKET_CRITICALITIES)[number]

export const TICKET_STATUSES = ['Open', 'Assigned', 'InProgress', 'Resolved', 'Closed'] as const
export type TicketStatus = (typeof TICKET_STATUSES)[number]

export const TICKET_ASSIGNEE_TYPES = ['Internal', 'External'] as const
export type TicketAssigneeType = (typeof TICKET_ASSIGNEE_TYPES)[number]

export type MaintenanceTicket = {
  id: string
  facilityId: string
  targetType: TicketTargetType
  targetId: string
  targetName: string | null
  targetCode: string | null
  title: string
  description: string | null
  criticality: TicketCriticality
  blocking: boolean
  status: TicketStatus
  assigneeType: TicketAssigneeType | null
  assigneeRef: string | null
  reportedBy: string | null
  resolution: string | null
  downtimeStart: string | null
  downtimeEnd: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
}

export type TicketPart = {
  id: string
  ticketId: string
  label: string
  reference: string | null
  quantity: number
  unitCost: number | null
  createdOnUtc: string
}

export type MaintenanceTicketDetail = {
  ticket: MaintenanceTicket
  parts: TicketPart[]
}

export type TicketFilter = {
  search?: string
  status?: string
  criticality?: string
  targetType?: string
  targetId?: string
  openOnly?: boolean
  page?: number
  pageSize?: number
}

export type CreateTicketInput = {
  targetType: TicketTargetType
  targetId: string
  title: string
  description?: string | null
  criticality: TicketCriticality
  blocking: boolean
}

export type UpdateTicketInput = {
  title: string
  description?: string | null
  criticality: TicketCriticality
  blocking: boolean
}

export type TicketPartInput = {
  label: string
  reference?: string | null
  quantity: number
  unitCost?: number | null
}

export type TicketAction = 'start' | 'resolve' | 'close'

export const ticketKeys = {
  all: (facilityId: string) => ['maintenance-tickets', facilityId] as const,
  list: (facilityId: string, f: TicketFilter) => ['maintenance-tickets', facilityId, 'list', f] as const,
  detail: (id: string) => ['maintenance-tickets', 'detail', id] as const,
}

export function getTickets(facilityId: string, filter: TicketFilter): Promise<PagedResult<MaintenanceTicket>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.status) q.set('status', filter.status)
  if (filter.criticality) q.set('criticality', filter.criticality)
  if (filter.targetType) q.set('targetType', filter.targetType)
  if (filter.targetId) q.set('targetId', filter.targetId)
  if (filter.openOnly) q.set('openOnly', 'true')
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<MaintenanceTicket>>(`/api/facilities/${facilityId}/maintenance/tickets?${q.toString()}`)
}

export function getTicket(id: string): Promise<MaintenanceTicketDetail> {
  return getJson<MaintenanceTicketDetail>(`/api/maintenance/tickets/${id}`)
}

export function createTicket(facilityId: string, input: CreateTicketInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/maintenance/tickets`, input)
}

export function updateTicket(id: string, input: UpdateTicketInput): Promise<void> {
  return putJson(`/api/maintenance/tickets/${id}`, input)
}

export function assignTicket(id: string, assigneeType: TicketAssigneeType, assigneeRef: string): Promise<void> {
  return postJson<void>(`/api/maintenance/tickets/${id}/assign`, { assigneeType, assigneeRef })
}

export function transitionTicket(id: string, action: TicketAction, resolution?: string | null): Promise<void> {
  return postJson<void>(`/api/maintenance/tickets/${id}/transition`, { action, resolution: resolution ?? null })
}

export function addTicketPart(id: string, input: TicketPartInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/maintenance/tickets/${id}/parts`, input)
}

export function deleteTicketPart(partId: string): Promise<void> {
  return del(`/api/maintenance/tickets/parts/${partId}`)
}
