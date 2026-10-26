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
  Add20Regular, Archive20Regular, ArrowReset20Regular, Bed20Regular, Eye20Regular, Edit20Regular,
  PlugConnected20Regular, PlugDisconnected20Regular,
} from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { RowActions, type RowAction } from '@shared/components'
import {
  SERVICE_CATEGORIES, SERVICE_STATUSES, archiveService, deleteService, getServices, restoreService,
  serviceKeys, setServiceStatus, type Service, type ServiceFilter, type ServiceStatus,
} from '../api'
import { ServiceFormDialog } from '../components/ServiceFormDialog'

const PAGE_SIZE = 10
const ANY = '__any__'
const statusColor = (s: string) => (s === 'Active' ? 'success' : 'danger')

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

const catLabel = (t: (k: string) => string, code: string) => {
  const l = t(`service.categories.${code}`)
  return l.startsWith('service.') ? code : l
}

export function ServicesPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const facilityId = useAppStore((s) => s.selectedFacilityId)
  const setSelectedService = useAppStore((s) => s.setSelectedService)
  const setInfraTab = useAppStore((s) => s.setInfraTab)
  const openService = (id: string, tab: string) => { setSelectedService(id); setInfraTab(tab); void navigate({ to: '/admin/service' as never }) }

  const [draft, setDraft] = useState<{ search: string; category: string; status: string; archived: boolean }>({ search: '', category: ANY, status: ANY, archived: false })
  const [applied, setApplied] = useState<ServiceFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Service | null>(null)

  const query = useQuery({ queryKey: serviceKeys.list(facilityId ?? '', applied), queryFn: () => getServices(facilityId!, applied), enabled: !!facilityId })
  const invalidate = () => queryClient.invalidateQueries({ queryKey: serviceKeys.all(facilityId ?? '') })
  const statusMut = useMutation({ mutationFn: ({ id, status }: { id: string; status: ServiceStatus }) => setServiceStatus(id, status), onSuccess: invalidate })
  const archiveMut = useMutation({ mutationFn: archiveService, onSuccess: invalidate })
  const restoreMut = useMutation({ mutationFn: restoreService, onSuccess: invalidate })

  if (!facilityId) {
    return <div className={styles.root}><div className={styles.state}>{t('service.noFacility')}</div></div>
  }

  const runSearch = () => setApplied({ search: draft.search || undefined, category: draft.category === ANY ? undefined : draft.category, status: draft.status === ANY ? undefined : draft.status, includeArchived: draft.archived, page: 1, pageSize: PAGE_SIZE })
  const goPage = (p: number) => setApplied((f) => ({ ...f, page: p }))
  const data = query.data
  const page = data?.page ?? 1
  const totalPages = data?.totalPages ?? 0

  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setEditing(null); setFormOpen(true) }}>{t('service.new')}</Button>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('service.filters.search')}</span>
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))} placeholder={t('service.filters.searchPlaceholder')} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('service.fields.category')}</span>
          <Dropdown value={draft.category === ANY ? t('service.filters.all') : catLabel(t, draft.category)} selectedOptions={[draft.category]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, category: d.optionValue! }))}>
            <Option value={ANY}>{t('service.filters.all')}</Option>
            {SERVICE_CATEGORIES.map((c) => <Option key={c} value={c}>{t(`service.categories.${c}`)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('service.fields.status')}</span>
          <Dropdown value={draft.status === ANY ? t('service.filters.all') : t(`service.status.${draft.status}`)} selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue! }))}>
            <Option value={ANY}>{t('service.filters.all')}</Option>
            {SERVICE_STATUSES.map((st) => <Option key={st} value={st}>{t(`service.status.${st}`)}</Option>)}
          </Dropdown>
        </div>
        <Checkbox label={t('actions.showArchived')} checked={draft.archived} onChange={(_, d) => setDraft((s) => ({ ...s, archived: !!d.checked }))} />
        <Button appearance="primary" onClick={runSearch}>{t('service.filters.apply')}</Button>
      </div>

      {query.isPending && <div className={styles.state}><Spinner label={t('service.loading')} /></div>}
      {query.isError && <div className={styles.state}><span className={styles.error}>{t('service.loadError')}</span></div>}
      {query.isSuccess && data!.items.length === 0 && <div className={styles.state}>{t('service.empty')}</div>}

      {query.isSuccess && data!.items.length > 0 && (
        <>
          <Table aria-label={t('service.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('service.fields.code')}</TableHeaderCell>
                <TableHeaderCell>{t('service.fields.name')}</TableHeaderCell>
                <TableHeaderCell>{t('service.fields.category')}</TableHeaderCell>
                <TableHeaderCell>{t('service.fields.head')}</TableHeaderCell>
                <TableHeaderCell>{t('service.fields.status')}</TableHeaderCell>
                <TableHeaderCell>{t('service.fields.actions')}</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data!.items.map((sv) => {
                const primary: RowAction[] = [
                  { key: 'show', label: t('actions.show'), icon: <Eye20Regular />, color: tokens.colorPaletteBlueForeground2, onClick: () => openService(sv.id, 'overview') },
                  { key: 'units', label: t('service.careUnits'), icon: <Bed20Regular />, color: tokens.colorBrandForeground1, onClick: () => openService(sv.id, 'careUnits') },
                  { key: 'edit', label: t('service.edit'), icon: <Edit20Regular />, color: tokens.colorPaletteTealForeground2, onClick: () => { setEditing(sv); setFormOpen(true) } },
                ]
                const menu: RowAction[] = [
                  sv.status === 'Active'
                    ? { key: 'deact', label: t('service.deactivate'), icon: <PlugDisconnected20Regular />, color: tokens.colorPaletteYellowForeground1, onClick: () => statusMut.mutate({ id: sv.id, status: 'Inactive' }) }
                    : { key: 'act', label: t('service.activate'), icon: <PlugConnected20Regular />, color: tokens.colorPaletteGreenForeground1, onClick: () => statusMut.mutate({ id: sv.id, status: 'Active' }) },
                  sv.archivedOnUtc == null
                    ? { key: 'arch', label: t('actions.archive'), icon: <Archive20Regular />, onClick: () => archiveMut.mutate(sv.id) }
                    : { key: 'rest', label: t('actions.restore'), icon: <ArrowReset20Regular />, onClick: () => restoreMut.mutate(sv.id) },
                ]
                return (
                  <TableRow key={sv.id} className={mergeClasses(sv.archivedOnUtc != null && styles.archived)}>
                    <TableCell><TableCellLayout>{sv.code}</TableCellLayout></TableCell>
                    <TableCell>{sv.name}</TableCell>
                    <TableCell>{catLabel(t, sv.category)}</TableCell>
                    <TableCell>{sv.head ?? '—'}</TableCell>
                    <TableCell><Badge appearance="filled" color={statusColor(sv.status)}>{t(`service.status.${sv.status}`)}</Badge></TableCell>
                    <TableCell>
                      <RowActions primary={primary} menu={menu}
                        onDelete={async () => { await deleteService(sv.id); invalidate() }}
                        confirmMessage={t('service.confirmDelete', { name: sv.name })} />
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
          <div className={styles.pager}>
            <span className={styles.label}>{t('service.pager', { page, totalPages, total: data!.total })}</span>
            <Button size="small" disabled={page <= 1} onClick={() => goPage(page - 1)}>{t('service.prev')}</Button>
            <Button size="small" disabled={page >= totalPages} onClick={() => goPage(page + 1)}>{t('service.next')}</Button>
          </div>
        </>
      )}

      <ServiceFormDialog open={formOpen} onOpenChange={setFormOpen} facilityId={facilityId} service={editing} />
    </div>
  )
}
