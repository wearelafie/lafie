import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from '@tanstack/react-router'
import {
  Badge, Button, Checkbox, Dropdown, Option, SearchBox, Spinner,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  Title3, makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import {
  Add20Regular, Archive20Regular, ArrowLeft20Regular, ArrowReset20Regular, Eye20Regular, Edit20Regular,
  Grid20Regular, Wrench20Regular, PlugConnected20Regular, PlugDisconnected20Regular,
} from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { getBuilding } from '@features/building/api'
import { RowActions, type RowAction } from '@shared/components'
import {
  FLOOR_STATUSES, archiveFloor, deleteFloor, floorKeys, getFloors, restoreFloor, setFloorStatus,
  type Floor, type FloorFilter, type FloorStatus,
} from '../api'
import { FloorFormDialog } from '../components/FloorFormDialog'

const PAGE_SIZE = 10
const ANY = '__any__'
const statusColor = (s: string) => (s === 'Active' ? 'success' : s === 'Maintenance' ? 'warning' : 'danger')

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  top: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  back: { display: 'inline-flex', alignItems: 'center', columnGap: '4px', textDecoration: 'none', color: tokens.colorBrandForeground1, fontSize: tokens.fontSizeBase200 },
  header: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM },
  filters: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM, flexWrap: 'wrap' },
  filterField: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  label: { fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1 },
  archived: { opacity: 0.55 },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
})

export function FloorsPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const buildingId = useAppStore((s) => s.selectedBuildingId)
  const setSelectedFloor = useAppStore((s) => s.setSelectedFloor)
  const openSpaces = (id: string) => { setSelectedFloor(id); void navigate({ to: '/admin/spaces' }) }
  const openDetail = (id: string) => { setSelectedFloor(id); void navigate({ to: '/admin/floor' }) }

  const building = useQuery({ queryKey: ['building', buildingId], queryFn: () => getBuilding(buildingId!), enabled: !!buildingId })

  const [draft, setDraft] = useState<{ search: string; status: string; archived: boolean }>({ search: '', status: ANY, archived: false })
  const [applied, setApplied] = useState<FloorFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Floor | null>(null)

  const query = useQuery({ queryKey: floorKeys.list(buildingId ?? '', applied), queryFn: () => getFloors(buildingId!, applied), enabled: !!buildingId })
  const invalidate = () => queryClient.invalidateQueries({ queryKey: floorKeys.all(buildingId ?? '') })
  const statusMut = useMutation({ mutationFn: ({ id, status }: { id: string; status: FloorStatus }) => setFloorStatus(id, status), onSuccess: invalidate })
  const archiveMut = useMutation({ mutationFn: archiveFloor, onSuccess: invalidate })
  const restoreMut = useMutation({ mutationFn: restoreFloor, onSuccess: invalidate })

  if (!buildingId) {
    return (
      <div className={styles.root}>
        <Title3>{t('floor.title')}</Title3>
        <div className={styles.state}>{t('floor.noBuilding')} <Link to={'/admin/buildings' as never}>{t('floor.goBuildings')}</Link></div>
      </div>
    )
  }

  const runSearch = () => setApplied({ search: draft.search || undefined, status: draft.status === ANY ? undefined : draft.status, includeArchived: draft.archived, page: 1, pageSize: PAGE_SIZE })
  const goPage = (p: number) => setApplied((f) => ({ ...f, page: p }))
  const data = query.data
  const page = data?.page ?? 1
  const totalPages = data?.totalPages ?? 0

  return (
    <div className={styles.root}>
      <div className={styles.top}>
        <Link to={'/admin/buildings' as never} className={styles.back}><ArrowLeft20Regular />{t('floor.goBuildings')}</Link>
        <div className={styles.header}>
          <Title3>{t('floor.titleFor', { building: building.data?.name ?? '…' })}</Title3>
          <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setEditing(null); setFormOpen(true) }}>{t('floor.new')}</Button>
        </div>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('floor.filters.search')}</span>
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))} placeholder={t('floor.filters.searchPlaceholder')} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('floor.fields.status')}</span>
          <Dropdown value={draft.status === ANY ? t('floor.filters.all') : t(`floor.status.${draft.status}`)} selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue! }))}>
            <Option value={ANY}>{t('floor.filters.all')}</Option>
            {FLOOR_STATUSES.map((st) => <Option key={st} value={st}>{t(`floor.status.${st}`)}</Option>)}
          </Dropdown>
        </div>
        <Checkbox label={t('actions.showArchived')} checked={draft.archived} onChange={(_, d) => setDraft((s) => ({ ...s, archived: !!d.checked }))} />
        <Button appearance="primary" onClick={runSearch}>{t('floor.filters.apply')}</Button>
      </div>

      {query.isPending && <div className={styles.state}><Spinner label={t('floor.loading')} /></div>}
      {query.isError && <div className={styles.state}><span className={styles.error}>{t('floor.loadError')}</span></div>}
      {query.isSuccess && data!.items.length === 0 && <div className={styles.state}>{t('floor.empty')}</div>}

      {query.isSuccess && data!.items.length > 0 && (
        <>
          <Table aria-label={t('floor.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('floor.fields.code')}</TableHeaderCell>
                <TableHeaderCell>{t('floor.fields.name')}</TableHeaderCell>
                <TableHeaderCell>{t('floor.fields.level')}</TableHeaderCell>
                <TableHeaderCell>{t('floor.fields.status')}</TableHeaderCell>
                <TableHeaderCell>{t('floor.fields.actions')}</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data!.items.map((f) => {
                const primary: RowAction[] = [
                  { key: 'show', label: t('actions.show'), icon: <Eye20Regular />, color: tokens.colorPaletteBlueForeground2, onClick: () => openDetail(f.id) },
                  { key: 'spaces', label: t('floor.spaces'), icon: <Grid20Regular />, color: tokens.colorBrandForeground1, onClick: () => openSpaces(f.id) },
                  { key: 'edit', label: t('floor.edit'), icon: <Edit20Regular />, color: tokens.colorPaletteTealForeground2, onClick: () => { setEditing(f); setFormOpen(true) } },
                ]
                const menu: RowAction[] = [
                  { key: 'maint', label: t('floor.status.Maintenance'), icon: <Wrench20Regular />, color: tokens.colorPaletteYellowForeground1, onClick: () => statusMut.mutate({ id: f.id, status: 'Maintenance' }) },
                  f.status === 'Active'
                    ? { key: 'deact', label: t('floor.deactivate'), icon: <PlugDisconnected20Regular />, onClick: () => statusMut.mutate({ id: f.id, status: 'Inactive' }) }
                    : { key: 'act', label: t('floor.activate'), icon: <PlugConnected20Regular />, color: tokens.colorPaletteGreenForeground1, onClick: () => statusMut.mutate({ id: f.id, status: 'Active' }) },
                  f.archivedOnUtc == null
                    ? { key: 'arch', label: t('actions.archive'), icon: <Archive20Regular />, onClick: () => archiveMut.mutate(f.id) }
                    : { key: 'rest', label: t('actions.restore'), icon: <ArrowReset20Regular />, onClick: () => restoreMut.mutate(f.id) },
                ]
                return (
                  <TableRow key={f.id} className={mergeClasses(f.archivedOnUtc != null && styles.archived)}>
                    <TableCell><TableCellLayout>{f.code}</TableCellLayout></TableCell>
                    <TableCell>{f.name}</TableCell>
                    <TableCell>{f.level ?? '—'}</TableCell>
                    <TableCell><Badge appearance="filled" color={statusColor(f.status)}>{t(`floor.status.${f.status}`)}</Badge></TableCell>
                    <TableCell>
                      <RowActions primary={primary} menu={menu}
                        onDelete={async () => { await deleteFloor(f.id); invalidate() }}
                        confirmMessage={t('floor.confirmDelete', { name: f.name })} />
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
          <div className={styles.pager}>
            <span className={styles.label}>{t('floor.pager', { page, totalPages, total: data!.total })}</span>
            <Button size="small" disabled={page <= 1} onClick={() => goPage(page - 1)}>{t('floor.prev')}</Button>
            <Button size="small" disabled={page >= totalPages} onClick={() => goPage(page + 1)}>{t('floor.next')}</Button>
          </div>
        </>
      )}

      <FloorFormDialog open={formOpen} onOpenChange={setFormOpen} buildingId={buildingId} floor={editing} />
    </div>
  )
}
