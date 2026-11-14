import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import {
  Badge, Button, Dropdown, Option, SearchBox, Spinner,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  Title3, makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import { ArrowLeft20Regular, Search20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { useCurrentUser } from '@features/auth'
import { getReferenceValues, refLabel } from '@features/reference/api'
import {
  EQUIPMENT_CATEGORY_REFERENCE, WARRANTY_STATES, getEquipmentWarranties, warrantyKeys,
  type EquipmentWarranty, type WarrantyFilter, type WarrantyState,
} from '../api'

const PAGE_SIZE = 10
const ANY = '__any__'

const stateColor = (s: WarrantyState): 'danger' | 'warning' | 'success' | 'subtle' =>
  s === 'Expired' ? 'danger' : s === 'Expiring' ? 'warning' : s === 'Valid' ? 'success' : 'subtle'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  header: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXS },
  back: { alignSelf: 'flex-start' },
  subtitle: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  filters: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM, rowGap: tokens.spacingVerticalS, flexWrap: 'wrap' },
  filterField: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  label: { fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1 },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  rowLink: { cursor: 'pointer' },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
})

export function EquipmentWarrantiesPage() {
  const styles = useStyles()
  const { t, i18n } = useTranslation()
  const lang = i18n.language
  const navigate = useNavigate()
  const { me } = useCurrentUser()
  const setSelectedEquipment = useAppStore((s) => s.setSelectedEquipment)
  const storeFacilityId = useAppStore((s) => s.selectedFacilityId)
  const facilityId = me?.activeEstablishment?.facilityId ?? storeFacilityId

  const [draft, setDraft] = useState<{ search: string; stateFilter: string }>({ search: '', stateFilter: ANY })
  const [applied, setApplied] = useState<WarrantyFilter>({ page: 1, pageSize: PAGE_SIZE })

  const categories = useQuery({
    queryKey: ['reference', 'values', EQUIPMENT_CATEGORY_REFERENCE],
    queryFn: () => getReferenceValues(EQUIPMENT_CATEGORY_REFERENCE),
  })
  const catLabel = (code: string): string => {
    const v = categories.data?.find((c) => c.code === code)
    return v ? refLabel(v, lang) : code
  }

  const query = useQuery({
    queryKey: warrantyKeys.list(facilityId ?? '', applied),
    queryFn: () => getEquipmentWarranties(facilityId!, applied),
    enabled: !!facilityId,
  })

  const runSearch = () => setApplied({
    page: 1, pageSize: PAGE_SIZE,
    search: draft.search.trim() || undefined,
    state: draft.stateFilter === ANY ? undefined : draft.stateFilter,
  })

  const openDetail = (w: EquipmentWarranty) => { setSelectedEquipment(w.equipmentId); void navigate({ to: '/admin/equipment-item' as never }) }

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))

  const stateBadge = (w: EquipmentWarranty) => {
    const label = t(`equipment.warranties.states.${w.warrantyState}`)
    if (w.warrantyState === 'Expired' && w.daysRemaining != null) {
      return <Badge appearance="tint" color="danger">{t('equipment.warranties.expiredBadge', { days: Math.abs(w.daysRemaining) })}</Badge>
    }
    if (w.warrantyState === 'Expiring' && w.daysRemaining != null) {
      return <Badge appearance="tint" color="warning">{t('equipment.warranties.remainingBadge', { days: w.daysRemaining })}</Badge>
    }
    return <Badge appearance="tint" color={stateColor(w.warrantyState)}>{label}</Badge>
  }

  if (!facilityId) {
    return <div className={styles.root}><div className={styles.state}>{t('equipment.noFacility')}</div></div>
  }

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <Button className={styles.back} appearance="subtle" size="small" icon={<ArrowLeft20Regular />}
          onClick={() => navigate({ to: '/admin/equipment' as never })}>{t('equipment.backToList')}</Button>
        <Title3>{t('equipment.warranties.title')}</Title3>
        <div className={styles.subtitle}>{t('equipment.warranties.subtitle')}</div>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('equipment.filters.search')}</span>
          <SearchBox value={draft.search} placeholder={t('equipment.filters.searchPlaceholder')}
            onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
            onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('equipment.warranties.stateColumn')}</span>
          <Dropdown value={draft.stateFilter === ANY ? t('equipment.warranties.allStates') : t(`equipment.warranties.states.${draft.stateFilter}`)}
            selectedOptions={[draft.stateFilter]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, stateFilter: d.optionValue ?? ANY }))}>
            <Option value={ANY}>{t('equipment.warranties.allStates')}</Option>
            {WARRANTY_STATES.map((s) => <Option key={s} value={s}>{t(`equipment.warranties.states.${s}`)}</Option>)}
          </Dropdown>
        </div>
        <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('equipment.filters.apply')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('equipment.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('equipment.warranties.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('equipment.warranties.empty')}</div>
      ) : (
        <Table aria-label={t('equipment.warranties.title')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('equipment.fields.inventoryNumber')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.fields.name')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.fields.location')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.fields.warrantyEnd')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.warranties.stateColumn')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((w) => (
              <TableRow key={w.equipmentId} className={styles.rowLink} onClick={() => openDetail(w)}>
                <TableCell><TableCellLayout>{w.inventoryNumber}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout description={catLabel(w.category)}>{w.name}</TableCellLayout></TableCell>
                <TableCell>
                  <TableCellLayout description={w.serviceName ?? undefined}>
                    {w.spaceName ?? <span className={styles.muted}>—</span>}
                  </TableCellLayout>
                </TableCell>
                <TableCell><TableCellLayout>{w.warrantyEnd ?? <span className={styles.muted}>{t('equipment.warranties.noDate')}</span>}</TableCellLayout></TableCell>
                <TableCell>{stateBadge(w)}</TableCell>
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
