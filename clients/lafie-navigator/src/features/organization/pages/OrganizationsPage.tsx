import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import {
  Badge, Button, Checkbox, Dropdown, Option, SearchBox, Spinner,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  Title3, makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import {
  Add20Regular, Archive20Regular, ArrowReset20Regular, Building20Regular, Eye20Regular, Edit20Regular,
  PlugConnected20Regular, PlugDisconnected20Regular,
} from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { RowActions, type RowAction } from '@shared/components'
import {
  FACILITY_TYPES, OPERATIONAL_STATUSES, archiveOrganization, deleteOrganization, getOrganizations,
  organizationKeys, restoreOrganization, setOrganizationStatus, type Organization, type OrganizationFilter,
} from '../api'
import { OrganizationFormDialog } from '../components/OrganizationFormDialog'

const PAGE_SIZE = 10
const ANY = '__any__'
const statusColor = (s: string) =>
  s === 'Active' ? 'success' : s === 'Setup' ? 'brand' : s === 'PermanentlyClosed' ? 'danger' : 'warning'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  header: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM },
  filters: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM, rowGap: tokens.spacingVerticalS, flexWrap: 'wrap' },
  filterField: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  label: { fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1 },
  rowSel: { backgroundColor: tokens.colorBrandBackground2 },
  archived: { opacity: 0.55 },
  clickable: { cursor: 'pointer' },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
})

const typeLabel = (t: (k: string) => string, code: string) => {
  const l = t(`organization.facilityTypes.${code}`)
  return l.startsWith('organization.') ? code : l
}

export function OrganizationsPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const selectedId = useAppStore((s) => s.selectedFacilityId)
  const setSelected = useAppStore((s) => s.setSelectedFacility)

  const [draft, setDraft] = useState<{ search: string; type: string; status: string; archived: boolean }>({ search: '', type: ANY, status: ANY, archived: false })
  const [applied, setApplied] = useState<OrganizationFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Organization | null>(null)

  const query = useQuery({ queryKey: organizationKeys.list(applied), queryFn: () => getOrganizations(applied) })
  const invalidate = () => queryClient.invalidateQueries({ queryKey: organizationKeys.all })
  const statusMut = useMutation({ mutationFn: ({ id, status }: { id: string; status: 'Active' | 'TemporarilyClosed' }) => setOrganizationStatus(id, status), onSuccess: invalidate })
  const archiveMut = useMutation({ mutationFn: archiveOrganization, onSuccess: invalidate })
  const restoreMut = useMutation({ mutationFn: restoreOrganization, onSuccess: invalidate })

  const openBuildings = (id: string) => { setSelected(id); void navigate({ to: '/admin/buildings' as never }) }
  const openDetail = (id: string) => { setSelected(id); void navigate({ to: '/admin/facility' }) }

  const runSearch = () => setApplied({
    search: draft.search || undefined,
    facilityTypeCode: draft.type === ANY ? undefined : draft.type,
    operationalStatus: draft.status === ANY ? undefined : draft.status,
    includeArchived: draft.archived,
    page: 1, pageSize: PAGE_SIZE,
  })
  const goPage = (p: number) => setApplied((f) => ({ ...f, page: p }))

  const data = query.data
  const page = data?.page ?? 1
  const totalPages = data?.totalPages ?? 0

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <Title3>{t('organization.title')}</Title3>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setEditing(null); setFormOpen(true) }}>
          {t('organization.new')}
        </Button>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('organization.filters.search')}</span>
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))} placeholder={t('organization.filters.searchPlaceholder')} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('organization.fields.facilityType')}</span>
          <Dropdown value={draft.type === ANY ? t('organization.filters.all') : typeLabel(t, draft.type)} selectedOptions={[draft.type]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, type: d.optionValue! }))}>
            <Option value={ANY}>{t('organization.filters.all')}</Option>
            {FACILITY_TYPES.map((c) => <Option key={c} value={c}>{t(`organization.facilityTypes.${c}`)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('organization.fields.status')}</span>
          <Dropdown value={draft.status === ANY ? t('organization.filters.all') : t(`organization.operationalStatus.${draft.status}`)} selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue! }))}>
            <Option value={ANY}>{t('organization.filters.all')}</Option>
            {OPERATIONAL_STATUSES.map((st) => <Option key={st} value={st}>{t(`organization.operationalStatus.${st}`)}</Option>)}
          </Dropdown>
        </div>
        <Checkbox label={t('actions.showArchived')} checked={draft.archived} onChange={(_, d) => setDraft((s) => ({ ...s, archived: !!d.checked }))} />
        <Button appearance="primary" onClick={runSearch}>{t('organization.filters.apply')}</Button>
      </div>

      {query.isPending && <div className={styles.state}><Spinner label={t('organization.loading')} /></div>}
      {query.isError && <div className={styles.state}><span className={styles.error}>{t('organization.loadError')}</span></div>}
      {query.isSuccess && data!.items.length === 0 && <div className={styles.state}>{t('organization.empty')}</div>}

      {query.isSuccess && data!.items.length > 0 && (
        <>
          <Table aria-label={t('organization.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('organization.fields.code')}</TableHeaderCell>
                <TableHeaderCell>{t('organization.fields.officialName')}</TableHeaderCell>
                <TableHeaderCell>{t('organization.fields.facilityType')}</TableHeaderCell>
                <TableHeaderCell>{t('organization.fields.status')}</TableHeaderCell>
                <TableHeaderCell>{t('organization.fields.locality')}</TableHeaderCell>
                <TableHeaderCell>{t('organization.fields.actions')}</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data!.items.map((o) => {
                const primary: RowAction[] = [
                  { key: 'show', label: t('actions.show'), icon: <Eye20Regular />, color: tokens.colorPaletteBlueForeground2, onClick: () => openDetail(o.id) },
                  { key: 'buildings', label: t('organization.buildings'), icon: <Building20Regular />, color: tokens.colorBrandForeground1, onClick: () => openBuildings(o.id) },
                  { key: 'edit', label: t('organization.edit'), icon: <Edit20Regular />, color: tokens.colorPaletteTealForeground2, onClick: () => { setEditing(o); setFormOpen(true) } },
                ]
                const statusItem: RowAction = o.operationalStatus === 'Active'
                  ? { key: 'close', label: t('organization.close'), icon: <PlugDisconnected20Regular />, color: tokens.colorPaletteYellowForeground1, onClick: () => statusMut.mutate({ id: o.id, status: 'TemporarilyClosed' }) }
                  : { key: 'reopen', label: t('organization.reopen'), icon: <PlugConnected20Regular />, color: tokens.colorPaletteGreenForeground1, onClick: () => statusMut.mutate({ id: o.id, status: 'Active' }) }
                const archiveItem: RowAction = o.archivedOnUtc == null
                  ? { key: 'archive', label: t('actions.archive'), icon: <Archive20Regular />, onClick: () => archiveMut.mutate(o.id) }
                  : { key: 'restore', label: t('actions.restore'), icon: <ArrowReset20Regular />, onClick: () => restoreMut.mutate(o.id) }
                const menuItems: RowAction[] = [statusItem, archiveItem]
                return (
                  <TableRow key={o.id} className={mergeClasses(styles.clickable, selectedId === o.id && styles.rowSel, o.archivedOnUtc != null && styles.archived)} onClick={() => setSelected(o.id)}>
                    <TableCell><TableCellLayout>{o.code}</TableCellLayout></TableCell>
                    <TableCell>{o.officialName}</TableCell>
                    <TableCell>{typeLabel(t, o.facilityTypeCode)}</TableCell>
                    <TableCell><Badge appearance="filled" color={statusColor(o.operationalStatus)}>{t(`organization.operationalStatus.${o.operationalStatus}`)}</Badge></TableCell>
                    <TableCell>{o.locality ?? '—'}</TableCell>
                    <TableCell>
                      <RowActions primary={primary} menu={menuItems}
                        onDelete={async () => { await deleteOrganization(o.id); invalidate() }}
                        confirmTitle={t('actions.confirmDeleteTitle')}
                        confirmMessage={t('organization.confirmDelete', { name: o.officialName })} />
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
          <div className={styles.pager}>
            <span className={styles.label}>{t('organization.pager', { page, totalPages, total: data!.total })}</span>
            <Button size="small" disabled={page <= 1} onClick={() => goPage(page - 1)}>{t('organization.prev')}</Button>
            <Button size="small" disabled={page >= totalPages} onClick={() => goPage(page + 1)}>{t('organization.next')}</Button>
          </div>
        </>
      )}

      <OrganizationFormDialog open={formOpen} onOpenChange={setFormOpen} organization={editing} />
    </div>
  )
}
