import { getJson } from '@shared/api/http'

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

export const referenceKeys = {
  values: (category: string) => ['reference', 'values', category] as const,
  geo: (level: string, parentCode?: string | null) => ['reference', 'geo', level, parentCode ?? null] as const,
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
