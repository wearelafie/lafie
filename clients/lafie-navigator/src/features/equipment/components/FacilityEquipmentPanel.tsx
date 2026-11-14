import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import {
  Badge, Button, Dropdown, Option, SearchBox, Spinner,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import { Search20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { getReferenceValues, refLabel, type ReferenceValue } from '@features/reference/api'
import {
  EQUIPMENT_STATUSES, EQUIPMENT_CATEGORY_REFERENCE, getEquipmentList, equipmentKeys,
  type Equipment, type EquipmentFilter,
} from '../api'

const PAGE_SIZE = 10
const ANY = '__any__'

const statusColor = (s: string): 'success' | 'warning' | 'danger' | 'subtle' =>
  s === 'Operational' ? 'success' : s === 'UnderMaintenance' ? 'warning' : s === 'OutOfService' ? 'danger' : 'subtle'

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  filters: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM, rowGap: tokens.spacingVerticalS, flexWrap: 'wrap' },
  filterField: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  label: { fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1 },
  archived: { opacity: 0.55 },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  rowLink: { cursor: 'pointer' },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
})

export function FacilityEquipmentPanel({ facilityId }: { facilityId: string }) {
  const styles = useStyles()
  const { t, i18n } = useTranslation()
  const lang = i18n.language
  const navigate = useNavigate()
  const setSelectedEquipment = useAppStore((s) => s.setSelectedEquipment)

  const [draft, setDraft] = useState<{ search: string; category: string; status: string }>({ search: '', category: ANY, status: ANY })
  const [applied, setApplied] = useState<EquipmentFilter>({ page: 1, pageSize: PAGE_SIZE })

  const categories = useQuery({
    queryKey: ['reference', 'values', EQUIPMENT_CATEGORY_REFERENCE],
    queryFn: () => getReferenceValues(EQUIPMENT_CATEGORY_REFERENCE),
  })
  const catLabel = (code: string): string => {
    const v = categories.data?.find((c: ReferenceValue) => c.code === code)
    return v ? refLabel(v, lang) : code
  }

  const query = useQuery({
    queryKey: equipmentKeys.list(facilityId, applied),
    queryFn: () => getEquipmentList(facilityId, applied),
    enabled: !!facilityId,
  })

  const runSearch = () => setApplied({
    page: 1, pageSize: PAGE_SIZE,
    search: draft.search.trim() || undefined,
    category: draft.category === ANY ? undefined : draft.category,
    status: draft.status === ANY ? undefined : draft.status,
  })

  const openDetail = (e: Equipment) => { setSelectedEquipment(e.id); void navigate({ to: '/admin/equipment-item' as never }) }

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))

  return (
    <div className={styles.root}>
      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('equipment.filters.search')}</span>
          <SearchBox value={draft.search} placeholder={t('equipment.filters.searchPlaceholder')}
            onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
            onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('equipment.fields.category')}</span>
          <Dropdown value={draft.category === ANY ? t('equipment.filters.allCategories') : catLabel(draft.category)}
            selectedOptions={[draft.category]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, category: d.optionValue ?? ANY }))}>
            <Option value={ANY}>{t('equipment.filters.allCategories')}</Option>
            {(categories.data ?? []).map((c) => <Option key={c.code} value={c.code}>{refLabel(c, lang)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('equipment.fields.status')}</span>
          <Dropdown value={draft.status === ANY ? t('equipment.filters.allStatuses') : t(`equipment.statuses.${draft.status}`)}
            selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
            <Option value={ANY}>{t('equipment.filters.allStatuses')}</Option>
            {EQUIPMENT_STATUSES.map((s) => <Option key={s} value={s}>{t(`equipment.statuses.${s}`)}</Option>)}
          </Dropdown>
        </div>
        <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('equipment.filters.apply')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('equipment.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('equipment.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('equipment.empty')}</div>
      ) : (
        <Table aria-label={t('equipment.title')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('equipment.fields.inventoryNumber')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.fields.name')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.fields.brandModel')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.fields.location')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.fields.status')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((e) => (
              <TableRow key={e.id} className={mergeClasses(styles.rowLink, e.archivedOnUtc ? styles.archived : undefined)} onClick={() => openDetail(e)}>
                <TableCell><TableCellLayout>{e.inventoryNumber}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout description={catLabel(e.category)}>{e.name}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{[e.brand, e.model].filter(Boolean).join(' · ') || <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell>
                  <TableCellLayout description={e.serviceName ?? undefined}>
                    {e.spaceName ?? <span className={styles.muted}>{t('equipment.form.unassigned')}</span>}
                  </TableCellLayout>
                </TableCell>
                <TableCell><Badge appearance="tint" color={statusColor(e.status)}>{t(`equipment.statuses.${e.status}`)}</Badge></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <div className={styles.pager}>
        <span className={styles.muted}>{t('equipment.pager', { page, totalPages, total })}</span>
        <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => go(page - 1)}>{t('equipment.prev')}</Button>
        <Button size="small" appearance="subtle" disabled={page >= totalPages} onClick={() => go(page + 1)}>{t('equipment.next')}</Button>
      </div>
    </div>
  )
}
