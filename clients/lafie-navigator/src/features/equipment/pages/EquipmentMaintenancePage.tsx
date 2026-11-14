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
import {
  MAINTENANCE_TYPES, MAINTENANCE_STATUSES, getFacilityMaintenance, facilityMaintenanceKeys,
  type FacilityMaintenance, type FacilityMaintenanceFilter, type MaintenanceStatus,
} from '../api'

const PAGE_SIZE = 10
const ANY = '__any__'

const statusColor = (s: MaintenanceStatus): 'success' | 'warning' | 'danger' | 'brand' | 'subtle' =>
  s === 'Completed' ? 'success' : s === 'InProgress' ? 'warning' : s === 'Planned' ? 'brand' : 'subtle'

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

export function EquipmentMaintenancePage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { me } = useCurrentUser()
  const setSelectedEquipment = useAppStore((s) => s.setSelectedEquipment)
  const storeFacilityId = useAppStore((s) => s.selectedFacilityId)
  const facilityId = me?.activeEstablishment?.facilityId ?? storeFacilityId

  const [draft, setDraft] = useState<{ search: string; type: string; status: string }>({ search: '', type: ANY, status: ANY })
  const [applied, setApplied] = useState<FacilityMaintenanceFilter>({ page: 1, pageSize: PAGE_SIZE })

  const query = useQuery({
    queryKey: facilityMaintenanceKeys.list(facilityId ?? '', applied),
    queryFn: () => getFacilityMaintenance(facilityId!, applied),
    enabled: !!facilityId,
  })

  const runSearch = () => setApplied({
    page: 1, pageSize: PAGE_SIZE,
    search: draft.search.trim() || undefined,
    type: draft.type === ANY ? undefined : draft.type,
    status: draft.status === ANY ? undefined : draft.status,
  })

  const openDetail = (m: FacilityMaintenance) => { setSelectedEquipment(m.equipmentId); void navigate({ to: '/admin/equipment-item' as never }) }

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))

  if (!facilityId) {
    return <div className={styles.root}><div className={styles.state}>{t('equipment.noFacility')}</div></div>
  }

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <Button className={styles.back} appearance="subtle" size="small" icon={<ArrowLeft20Regular />}
          onClick={() => navigate({ to: '/admin/equipment' as never })}>{t('equipment.backToList')}</Button>
        <Title3>{t('equipment.maintenanceList.title')}</Title3>
        <div className={styles.subtitle}>{t('equipment.maintenanceList.subtitle')}</div>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('equipment.filters.search')}</span>
          <SearchBox value={draft.search} placeholder={t('equipment.filters.searchPlaceholder')}
            onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
            onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('equipment.maintenance.fields.type')}</span>
          <Dropdown value={draft.type === ANY ? t('equipment.maintenanceList.allTypes') : t(`equipment.maintenance.types.${draft.type}`)}
            selectedOptions={[draft.type]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, type: d.optionValue ?? ANY }))}>
            <Option value={ANY}>{t('equipment.maintenanceList.allTypes')}</Option>
            {MAINTENANCE_TYPES.map((s) => <Option key={s} value={s}>{t(`equipment.maintenance.types.${s}`)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('equipment.maintenance.fields.status')}</span>
          <Dropdown value={draft.status === ANY ? t('equipment.maintenanceList.allStatuses') : t(`equipment.maintenance.statuses.${draft.status}`)}
            selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
            <Option value={ANY}>{t('equipment.maintenanceList.allStatuses')}</Option>
            {MAINTENANCE_STATUSES.map((s) => <Option key={s} value={s}>{t(`equipment.maintenance.statuses.${s}`)}</Option>)}
          </Dropdown>
        </div>
        <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('equipment.filters.apply')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('equipment.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('equipment.maintenanceList.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('equipment.maintenanceList.empty')}</div>
      ) : (
        <Table aria-label={t('equipment.maintenanceList.title')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('equipment.maintenanceList.equipmentColumn')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.maintenance.fields.type')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.maintenance.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.maintenanceList.dateColumn')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.maintenance.fields.provider')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((m) => (
              <TableRow key={m.id} className={styles.rowLink} onClick={() => openDetail(m)}>
                <TableCell>
                  <TableCellLayout description={m.inventoryNumber}>{m.equipmentName}</TableCellLayout>
                </TableCell>
                <TableCell><TableCellLayout>{t(`equipment.maintenance.types.${m.type}`)}</TableCellLayout></TableCell>
                <TableCell><Badge appearance="tint" color={statusColor(m.status)}>{t(`equipment.maintenance.statuses.${m.status}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{m.scheduledOn ?? m.performedOn ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{m.provider ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
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
