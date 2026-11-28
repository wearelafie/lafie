import { del, getJson, postJson, putJson } from '@shared/api/http'
import type { PagedResult } from '@features/organization/api'

// --- Catalogue médicaments ---

export const MEDICATION_FORMS = [
  'Tablet', 'Capsule', 'Syrup', 'Solution', 'Injectable', 'Infusion', 'Cream', 'Ointment',
  'Drops', 'Suppository', 'Inhaler', 'Patch', 'Powder', 'Other',
] as const
export type MedicationForm = (typeof MEDICATION_FORMS)[number]

export const MEDICATION_STATUSES = ['Active', 'Inactive'] as const
export type MedicationStatus = (typeof MEDICATION_STATUSES)[number]

export type Medication = {
  id: string
  facilityId: string
  code: string
  name: string
  inn: string | null
  form: MedicationForm
  strength: string | null
  unit: string
  atcCode: string | null
  therapeuticClass: string | null
  controlled: boolean
  status: MedicationStatus
  notes: string | null
  createdOnUtc: string
  updatedOnUtc: string | null
}

export type MedicationFilter = {
  search?: string
  form?: string
  status?: string
  controlled?: boolean
  page?: number
  pageSize?: number
}

export type CreateMedicationInput = {
  code: string
  name: string
  inn?: string | null
  form: MedicationForm
  strength?: string | null
  unit: string
  atcCode?: string | null
  therapeuticClass?: string | null
  controlled: boolean
  notes?: string | null
}

export type UpdateMedicationInput = Omit<CreateMedicationInput, 'code'>

// --- Query keys ---

export const medicationKeys = {
  all: (facilityId: string) => ['medications', facilityId] as const,
  list: (facilityId: string, f: MedicationFilter) => ['medications', facilityId, 'list', f] as const,
  detail: (id: string) => ['medications', 'detail', id] as const,
}

// --- API ---

export function getMedications(facilityId: string, filter: MedicationFilter): Promise<PagedResult<Medication>> {
  const q = new URLSearchParams()
  if (filter.search) q.set('search', filter.search)
  if (filter.form) q.set('form', filter.form)
  if (filter.status) q.set('status', filter.status)
  if (filter.controlled != null) q.set('controlled', String(filter.controlled))
  q.set('page', String(filter.page ?? 1))
  q.set('pageSize', String(filter.pageSize ?? 20))
  return getJson<PagedResult<Medication>>(`/api/facilities/${facilityId}/pharmacy/medications?${q.toString()}`)
}

export function getMedication(id: string): Promise<Medication> {
  return getJson<Medication>(`/api/pharmacy/medications/${id}`)
}

export function createMedication(facilityId: string, input: CreateMedicationInput): Promise<{ id: string }> {
  return postJson<{ id: string }>(`/api/facilities/${facilityId}/pharmacy/medications`, input)
}

export function updateMedication(id: string, input: UpdateMedicationInput): Promise<void> {
  return putJson(`/api/pharmacy/medications/${id}`, input)
}

export function setMedicationStatus(id: string, active: boolean): Promise<void> {
  return postJson<void>(`/api/pharmacy/medications/${id}/status`, { active })
}

// Réservé à une évolution ultérieure (le catalogue ne supprime pas, il déréférence).
export function deleteMedication(id: string): Promise<void> {
  return del(`/api/pharmacy/medications/${id}`)
}
