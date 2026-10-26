import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import {
  Badge, Button, Checkbox, Dropdown, Option, SearchBox, Spinner,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import {
  Add20Regular, Archive20Regular, ArrowReset20Regular, Grid20Regular, Eye20Regular, Edit20Regular,
  Wrench20Regular, PlugConnected20Regular, PlugDisconnected20Regular,
} from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { RowActions, type RowAction } from '@shared/components'
import {
  CARE_UNIT_STATUSES, CARE_UNIT_TYPES, archiveCareUnit, careUnitKeys, deleteCareUnit, getCareUnits,
  restoreCareUnit, setCareUnitStatus, type CareUnit, type CareUnitFilter, type CareUnitStatus,
} from '../api'
import { CareUnitFormDialog } from '../components/CareUnitFormDialog'

const PAGE_SIZE = 10
const ANY = '__any__'
const statusColor = (s: string) => (s === 'Active' ? 'success' : s === 'Maintenance' ? 'warning' : 'danger')

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  toolbar: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
  filters: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM, rowGap: tokens.spacingVerticalS, flexWrap: 'wrap' },
  filterField: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  label: { fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1 },
  archived: { opacity: 0.55 },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
})

const typeLabel = (t: (k: string) => string, code: string) => {
  const l = t(`careUnit.types.${code}`)
  return l.startsWith('careUnit.') ? code : l
}

export function CareUnitsPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const serviceId = useAppStore((s) => s.selectedServiceId)
  const setSelectedCareUnit = useAppStore((s) => s.setSelectedCareUnit)
  const setInfraTab = useAppStore((s) => s.setInfraTab)
  const openCareUnit = (id: string, tab: string) => { setSelectedCareUnit(id); setInfraTab(tab); void navigate({ to: '/admin/care-unit' as never }) }

  const [draft, setDraft] = useState<{ search: string; type: string; status: string; archived: boolean }>({ search: '', type: ANY, status: ANY, archived: false })
  const [applied, setApplied] = useState<CareUnitFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<CareUnit | null>(null)

  const query = useQuery({ queryKey: careUnitKeys.list(serviceId ?? '', applied), queryFn: () => getCareUnits(serviceId!, applied), enabled: !!serviceId })
  const invalidate = () => queryClient.invalidateQueries({ queryKey: careUnitKeys.all(serviceId ?? '') })
  const statusMut = useMutation({ mutationFn: ({ id, status }: { id: string; status: CareUnitStatus }) => setCareUnitStatus(id, status), onSuccess: invalidate })
  const archiveMut = useMutation({ mutationFn: archiveCareUnit, onSuccess: invalidate })
  const restoreMut = useMutation({ mutationFn: restoreCareUnit, onSuccess: invalidate })

  if (!serviceId) {
    return <div className={styles.root}><div className={styles.state}>{t('careUnit.noService')}</div></div>
  }

  const runSearch = () => setApplied({ search: draft.search || undefined, type: draft.type === ANY ? undefined : draft.type, status: draft.status === ANY ? undefined : draft.status, includeArchived: draft.archived, page: 1, pageSize: PAGE_SIZE })
  const goPage = (p: number) => setApplied((f) => ({ ...f, page: p }))
  const data = query.data
  const page = data?.page ?? 1
  const totalPages = data?.totalPages ?? 0

  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setEditing(null); setFormOpen(true) }}>{t('careUnit.new')}</Button>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('careUnit.filters.search')}</span>
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))} placeholder={t('careUnit.filters.searchPlaceholder')} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('careUnit.fields.type')}</span>
          <Dropdown value={draft.type === ANY ? t('careUnit.filters.all') : typeLabel(t, draft.type)} selectedOptions={[draft.type]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, type: d.optionValue! }))}>
            <Option value={ANY}>{t('careUnit.filters.all')}</Option>
            {CARE_UNIT_TYPES.map((ty) => <Option key={ty} value={ty}>{t(`careUnit.types.${ty}`)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('careUnit.fields.status')}</span>
          <Dropdown value={draft.status === ANY ? t('careUnit.filters.all') : t(`careUnit.status.${draft.status}`)} selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue! }))}>
            <Option value={ANY}>{t('careUnit.filters.all')}</Option>
            {CARE_UNIT_STATUSES.map((st) => <Option key={st} value={st}>{t(`careUnit.status.${st}`)}</Option>)}
          </Dropdown>
        </div>
        <Checkbox label={t('actions.showArchived')} checked={draft.archived} onChange={(_, d) => setDraft((s) => ({ ...s, archived: !!d.checked }))} />
        <Button appearance="primary" onClick={runSearch}>{t('careUnit.filters.apply')}</Button>
      </div>

      {query.isPending && <div className={styles.state}><Spinner label={t('careUnit.loading')} /></div>}
      {query.isError && <div className={styles.state}><span className={styles.error}>{t('careUnit.loadError')}</span></div>}
      {query.isSuccess && data!.items.length === 0 && <div className={styles.state}>{t('careUnit.empty')}</div>}

      {query.isSuccess && data!.items.length > 0 && (
        <>
          <Table aria-label={t('careUnit.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('careUnit.fields.code')}</TableHeaderCell>
                <TableHeaderCell>{t('careUnit.fields.name')}</TableHeaderCell>
                <TableHeaderCell>{t('careUnit.fields.type')}</TableHeaderCell>
                <TableHeaderCell>{t('careUnit.fields.bedCapacity')}</TableHeaderCell>
                <TableHeaderCell>{t('careUnit.fields.status')}</TableHeaderCell>
                <TableHeaderCell>{t('careUnit.fields.actions')}</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data!.items.map((u) => {
                const primary: RowAction[] = [
                  { key: 'show', label: t('actions.show'), icon: <Eye20Regular />, color: tokens.colorPaletteBlueForeground2, onClick: () => openCareUnit(u.id, 'overview') },
                  { key: 'spaces', label: t('careUnit.spaces'), icon: <Grid20Regular />, color: tokens.colorBrandForeground1, onClick: () => openCareUnit(u.id, 'spaces') },
                  { key: 'edit', label: t('careUnit.edit'), icon: <Edit20Regular />, color: tokens.colorPaletteTealForeground2, onClick: () => { setEditing(u); setFormOpen(true) } },
                ]
                const menu: RowAction[] = [
                  { key: 'maint', label: t('careUnit.status.Maintenance'), icon: <Wrench20Regular />, color: tokens.colorPaletteYellowForeground1, onClick: () => statusMut.mutate({ id: u.id, status: 'Maintenance' }) },
                  u.status === 'Active'
                    ? { key: 'deact', label: t('careUnit.deactivate'), icon: <PlugDisconnected20Regular />, onClick: () => statusMut.mutate({ id: u.id, status: 'Inactive' }) }
                    : { key: 'act', label: t('careUnit.activate'), icon: <PlugConnected20Regular />, color: tokens.colorPaletteGreenForeground1, onClick: () => statusMut.mutate({ id: u.id, status: 'Active' }) },
                  u.archivedOnUtc == null
                    ? { key: 'arch', label: t('actions.archive'), icon: <Archive20Regular />, onClick: () => archiveMut.mutate(u.id) }
                    : { key: 'rest', label: t('actions.restore'), icon: <ArrowReset20Regular />, onClick: () => restoreMut.mutate(u.id) },
                ]
                return (
                  <TableRow key={u.id} className={mergeClasses(u.archivedOnUtc != null && styles.archived)}>
                    <TableCell><TableCellLayout>{u.code}</TableCellLayout></TableCell>
                    <TableCell>{u.name}</TableCell>
                    <TableCell>{typeLabel(t, u.type)}</TableCell>
                    <TableCell>{u.bedCapacity ?? '—'}</TableCell>
                    <TableCell><Badge appearance="filled" color={statusColor(u.status)}>{t(`careUnit.status.${u.status}`)}</Badge></TableCell>
                    <TableCell>
                      <RowActions primary={primary} menu={menu}
                        onDelete={async () => { await deleteCareUnit(u.id); invalidate() }}
                        confirmMessage={t('careUnit.confirmDelete', { name: u.name })} />
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
          <div className={styles.pager}>
            <span className={styles.label}>{t('careUnit.pager', { page, totalPages, total: data!.total })}</span>
            <Button size="small" disabled={page <= 1} onClick={() => goPage(page - 1)}>{t('careUnit.prev')}</Button>
            <Button size="small" disabled={page >= totalPages} onClick={() => goPage(page + 1)}>{t('careUnit.next')}</Button>
          </div>
        </>
      )}

      <CareUnitFormDialog open={formOpen} onOpenChange={setFormOpen} serviceId={serviceId} careUnit={editing} />
    </div>
  )
}
