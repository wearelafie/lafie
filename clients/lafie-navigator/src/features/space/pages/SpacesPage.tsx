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
  Add20Regular, Archive20Regular, ArrowReset20Regular, Eye20Regular, Edit20Regular,
  Wrench20Regular, PlugConnected20Regular, PlugDisconnected20Regular,
} from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { RowActions, type RowAction } from '@shared/components'
import {
  SPACE_STATUSES, SPACE_TYPES, archiveSpace, deleteSpace, getSpaces, restoreSpace, setSpaceStatus, spaceKeys,
  type Space, type SpaceFilter, type SpaceStatus,
} from '../api'
import { SpaceFormDialog } from '../components/SpaceFormDialog'

const PAGE_SIZE = 10
const ANY = '__any__'
const statusColor = (s: string) => (s === 'Active' ? 'success' : s === 'Maintenance' ? 'warning' : 'danger')

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  toolbar: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
  filters: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM, flexWrap: 'wrap' },
  filterField: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  label: { fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1 },
  archived: { opacity: 0.55 },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
})

export function SpacesPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const floorId = useAppStore((s) => s.selectedFloorId)
  const setSelectedSpace = useAppStore((s) => s.setSelectedSpace)
  const openDetail = (id: string) => { setSelectedSpace(id); void navigate({ to: '/admin/space' }) }

  const [draft, setDraft] = useState<{ search: string; type: string; status: string; archived: boolean }>({ search: '', type: ANY, status: ANY, archived: false })
  const [applied, setApplied] = useState<SpaceFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Space | null>(null)

  const query = useQuery({ queryKey: spaceKeys.list(floorId ?? '', applied), queryFn: () => getSpaces(floorId!, applied), enabled: !!floorId })
  const invalidate = () => queryClient.invalidateQueries({ queryKey: spaceKeys.all(floorId ?? '') })
  const statusMut = useMutation({ mutationFn: ({ id, status }: { id: string; status: SpaceStatus }) => setSpaceStatus(id, status), onSuccess: invalidate })
  const archiveMut = useMutation({ mutationFn: archiveSpace, onSuccess: invalidate })
  const restoreMut = useMutation({ mutationFn: restoreSpace, onSuccess: invalidate })

  if (!floorId) {
    return <div className={styles.root}><div className={styles.state}>{t('space.noFloor')}</div></div>
  }

  const runSearch = () => setApplied({ search: draft.search || undefined, type: draft.type === ANY ? undefined : draft.type, status: draft.status === ANY ? undefined : draft.status, includeArchived: draft.archived, page: 1, pageSize: PAGE_SIZE })
  const goPage = (p: number) => setApplied((f) => ({ ...f, page: p }))
  const data = query.data
  const page = data?.page ?? 1
  const totalPages = data?.totalPages ?? 0

  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setEditing(null); setFormOpen(true) }}>{t('space.new')}</Button>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('space.filters.search')}</span>
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))} placeholder={t('space.filters.searchPlaceholder')} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('space.fields.type')}</span>
          <Dropdown value={draft.type === ANY ? t('space.filters.all') : t(`space.types.${draft.type}`)} selectedOptions={[draft.type]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, type: d.optionValue! }))}>
            <Option value={ANY}>{t('space.filters.all')}</Option>
            {SPACE_TYPES.map((ty) => <Option key={ty} value={ty}>{t(`space.types.${ty}`)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('space.fields.status')}</span>
          <Dropdown value={draft.status === ANY ? t('space.filters.all') : t(`space.status.${draft.status}`)} selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue! }))}>
            <Option value={ANY}>{t('space.filters.all')}</Option>
            {SPACE_STATUSES.map((st) => <Option key={st} value={st}>{t(`space.status.${st}`)}</Option>)}
          </Dropdown>
        </div>
        <Checkbox label={t('actions.showArchived')} checked={draft.archived} onChange={(_, d) => setDraft((s) => ({ ...s, archived: !!d.checked }))} />
        <Button appearance="primary" onClick={runSearch}>{t('space.filters.apply')}</Button>
      </div>

      {query.isPending && <div className={styles.state}><Spinner label={t('space.loading')} /></div>}
      {query.isError && <div className={styles.state}><span className={styles.error}>{t('space.loadError')}</span></div>}
      {query.isSuccess && data!.items.length === 0 && <div className={styles.state}>{t('space.empty')}</div>}

      {query.isSuccess && data!.items.length > 0 && (
        <>
          <Table aria-label={t('space.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('space.fields.code')}</TableHeaderCell>
                <TableHeaderCell>{t('space.fields.name')}</TableHeaderCell>
                <TableHeaderCell>{t('space.fields.type')}</TableHeaderCell>
                <TableHeaderCell>{t('space.fields.maxCapacity')}</TableHeaderCell>
                <TableHeaderCell>{t('space.fields.status')}</TableHeaderCell>
                <TableHeaderCell>{t('space.fields.actions')}</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data!.items.map((sp) => {
                const primary: RowAction[] = [
                  { key: 'show', label: t('actions.show'), icon: <Eye20Regular />, color: tokens.colorPaletteBlueForeground2, onClick: () => openDetail(sp.id) },
                  { key: 'edit', label: t('space.edit'), icon: <Edit20Regular />, color: tokens.colorPaletteTealForeground2, onClick: () => { setEditing(sp); setFormOpen(true) } },
                ]
                const menu: RowAction[] = [
                  { key: 'maint', label: t('space.status.Maintenance'), icon: <Wrench20Regular />, color: tokens.colorPaletteYellowForeground1, onClick: () => statusMut.mutate({ id: sp.id, status: 'Maintenance' }) },
                  sp.status === 'Active'
                    ? { key: 'deact', label: t('space.deactivate'), icon: <PlugDisconnected20Regular />, onClick: () => statusMut.mutate({ id: sp.id, status: 'Inactive' }) }
                    : { key: 'act', label: t('space.activate'), icon: <PlugConnected20Regular />, color: tokens.colorPaletteGreenForeground1, onClick: () => statusMut.mutate({ id: sp.id, status: 'Active' }) },
                  sp.archivedOnUtc == null
                    ? { key: 'arch', label: t('actions.archive'), icon: <Archive20Regular />, onClick: () => archiveMut.mutate(sp.id) }
                    : { key: 'rest', label: t('actions.restore'), icon: <ArrowReset20Regular />, onClick: () => restoreMut.mutate(sp.id) },
                ]
                return (
                  <TableRow key={sp.id} className={mergeClasses(sp.archivedOnUtc != null && styles.archived)}>
                    <TableCell><TableCellLayout>{sp.code}</TableCellLayout></TableCell>
                    <TableCell>{sp.name}</TableCell>
                    <TableCell>{t(`space.types.${sp.type}`)}</TableCell>
                    <TableCell>{sp.maxCapacity ?? '—'}</TableCell>
                    <TableCell><Badge appearance="filled" color={statusColor(sp.status)}>{t(`space.status.${sp.status}`)}</Badge></TableCell>
                    <TableCell>
                      <RowActions primary={primary} menu={menu}
                        onDelete={async () => { await deleteSpace(sp.id); invalidate() }}
                        confirmMessage={t('space.confirmDelete', { name: sp.name })} />
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
          <div className={styles.pager}>
            <span className={styles.label}>{t('space.pager', { page, totalPages, total: data!.total })}</span>
            <Button size="small" disabled={page <= 1} onClick={() => goPage(page - 1)}>{t('space.prev')}</Button>
            <Button size="small" disabled={page >= totalPages} onClick={() => goPage(page + 1)}>{t('space.next')}</Button>
          </div>
        </>
      )}

      <SpaceFormDialog open={formOpen} onOpenChange={setFormOpen} floorId={floorId} space={editing} />
    </div>
  )
}
