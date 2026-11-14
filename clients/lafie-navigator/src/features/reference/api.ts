import { del, getJson, postJson, putJson } from '@shared/api/http'

export type ReferenceValue = {
  category: string
  code: string
  labelFr: string
  labelEn: string
  displayOrder: number
}

export type GeoDivision = {
  id: string
  level: string
  code: string
  name: string
  parentCode: string | null
  displayOrder: number
}

// --- Administration ---
export type ReferenceValueAdmin = ReferenceValue & { active: boolean }
export type ReferenceCategory = { category: string; count: number }
export const GEO_LEVELS = ['Country', 'Region', 'Prefecture', 'Commune'] as const
export type GeoLevel = (typeof GEO_LEVELS)[number]
export const geoParentLevel = (level: GeoLevel): GeoLevel | null =>
  level === 'Region' ? 'Country' : level === 'Prefecture' ? 'Region' : level === 'Commune' ? 'Prefecture' : null

export const referenceKeys = {
  values: (category: string) => ['reference', 'values', category] as const,
  geo: (level: string, parentCode?: string | null) => ['reference', 'geo', level, parentCode ?? null] as const,
  categories: () => ['reference', 'admin', 'categories'] as const,
  adminValues: (category: string, search: string, includeInactive: boolean) =>
    ['reference', 'admin', 'values', category, search, includeInactive] as const,
}

export function getReferenceValues(category: string): Promise<ReferenceValue[]> {
  return getJson<ReferenceValue[]>(`/api/reference-values/${category}`)
}

export function getGeoDivisions(level: string, parentCode?: string | null): Promise<GeoDivision[]> {
  const q = new URLSearchParams({ level })
  if (parentCode) q.set('parentCode', parentCode)
  return getJson<GeoDivision[]>(`/api/geo-divisions?${q.toString()}`)
}

/** Libellé d'une valeur de référence selon la langue courante (repli FR). */
export function refLabel(v: ReferenceValue, lang: string): string {
  return lang.startsWith('en') ? v.labelEn : v.labelFr
}

// ---------------------------------------------------------------- Administration : valeurs de référence
export function getReferenceCategories(): Promise<ReferenceCategory[]> {
  return getJson<ReferenceCategory[]>('/api/reference-admin/categories')
}

export function getReferenceValuesAdmin(category: string, opts: { search?: string; includeInactive?: boolean } = {}): Promise<ReferenceValueAdmin[]> {
  const q = new URLSearchParams()
  if (opts.search) q.set('search', opts.search)
  if (opts.includeInactive) q.set('includeInactive', 'true')
  const qs = q.toString()
  return getJson<ReferenceValueAdmin[]>(`/api/reference-admin/values/${encodeURIComponent(category)}${qs ? `?${qs}` : ''}`)
}

export type ReferenceValueInput = { category: string; code: string; labelFr: string; labelEn: string; displayOrder: number; active: boolean }
export function createReferenceValue(input: ReferenceValueInput): Promise<void> {
  return postJson<void>('/api/reference-admin/values', input)
}
export function updateReferenceValue(category: string, code: string, input: { labelFr: string; labelEn: string; displayOrder: number; active: boolean }): Promise<void> {
  return putJson(`/api/reference-admin/values/${encodeURIComponent(category)}/${encodeURIComponent(code)}`, input)
}
export function setReferenceValueActive(category: string, code: string, active: boolean): Promise<void> {
  return putJson(`/api/reference-admin/values/${encodeURIComponent(category)}/${encodeURIComponent(code)}/active`, { active })
}
export function deleteReferenceValue(category: string, code: string): Promise<void> {
  return del(`/api/reference-admin/values/${encodeURIComponent(category)}/${encodeURIComponent(code)}`)
}

// ---------------------------------------------------------------- Administration : divisions géographiques
export type GeoDivisionInput = { level: GeoLevel; code: string; name: string; parentCode?: string | null; displayOrder: number }
export function createGeoDivision(input: GeoDivisionInput): Promise<{ id: string }> {
  return postJson<{ id: string }>('/api/reference-admin/geo', input)
}
export function updateGeoDivision(id: string, input: { name: string; parentCode?: string | null; displayOrder: number }): Promise<void> {
  return putJson(`/api/reference-admin/geo/${id}`, input)
}
export function deleteGeoDivision(id: string): Promise<void> {
  return del(`/api/reference-admin/geo/${id}`)
}
