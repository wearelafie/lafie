import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from '@tanstack/react-router'
import {
  Badge,
  Button,
  Dropdown,
  Option,
  SearchBox,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableCellLayout,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Title3,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { Add20Regular, ArrowLeft20Regular, Edit20Regular, Wrench20Regular, PlugConnected20Regular, PlugDisconnected20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { getBuilding } from '@features/building/api'
import { FLOOR_STATUSES, floorKeys, getFloors, setFloorStatus, type Floor, type FloorFilter, type FloorStatus } from '../api'
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
  actions: { display: 'flex', columnGap: tokens.spacingHorizontalXS },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
})

export function FloorsPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const buildingId = useAppStore((s) => s.selectedBuildingId)

  const building = useQuery({
    queryKey: ['building', buildingId],
    queryFn: () => getBuilding(buildingId!),
    enabled: !!buildingId,
  })

  const [draft, setDraft] = useState<{ search: string; status: string }>({ search: '', status: ANY })
  const [applied, setApplied] = useState<FloorFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Floor | null>(null)

  const query = useQuery({
    queryKey: floorKeys.list(buildingId ?? '', applied),
    queryFn: () => getFloors(buildingId!, applied),
    enabled: !!buildingId,
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: FloorStatus }) => setFloorStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: floorKeys.all(buildingId ?? '') }),
  })

  if (!buildingId) {
    return (
      <div className={styles.root}>
        <Title3>{t('floor.title')}</Title3>
        <div className={styles.state}>
          {t('floor.noBuilding')} <Link to={'/admin/buildings' as never}>{t('floor.goBuildings')}</Link>
        </div>
      </div>
    )
  }

  const runSearch = () =>
    setApplied({ search: draft.search || undefined, status: draft.status === ANY ? undefined : draft.status, page: 1, pageSize: PAGE_SIZE })
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
          <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setEditing(null); setFormOpen(true) }}>
            {t('floor.new')}
          </Button>
        </div>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('floor.filters.search')}</span>
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
            placeholder={t('floor.filters.searchPlaceholder')} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('floor.fields.status')}</span>
          <Dropdown
            value={draft.status === ANY ? t('floor.filters.all') : t(`floor.status.${draft.status}`)}
            selectedOptions={[draft.status]}
            onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue! }))}
          >
            <Option value={ANY}>{t('floor.filters.all')}</Option>
            {FLOOR_STATUSES.map((st) => <Option key={st} value={st}>{t(`floor.status.${st}`)}</Option>)}
          </Dropdown>
        </div>
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
              {data!.items.map((f) => (
                <TableRow key={f.id}>
                  <TableCell><TableCellLayout>{f.code}</TableCellLayout></TableCell>
                  <TableCell>{f.name}</TableCell>
                  <TableCell>{f.level ?? '—'}</TableCell>
                  <TableCell><Badge appearance="filled" color={statusColor(f.status)}>{t(`floor.status.${f.status}`)}</Badge></TableCell>
                  <TableCell>
                    <div className={styles.actions}>
                      <Button size="small" appearance="subtle" icon={<Edit20Regular />} aria-label={t('floor.edit')}
                        onClick={() => { setEditing(f); setFormOpen(true) }} />
                      <Button size="small" appearance="subtle" icon={<Wrench20Regular />} aria-label={t('floor.status.Maintenance')}
                        onClick={() => statusMutation.mutate({ id: f.id, status: 'Maintenance' })} />
                      {f.status === 'Active' ? (
                        <Button size="small" appearance="subtle" icon={<PlugDisconnected20Regular />} aria-label={t('floor.deactivate')}
                          onClick={() => statusMutation.mutate({ id: f.id, status: 'Inactive' })} />
                      ) : (
                        <Button size="small" appearance="subtle" icon={<PlugConnected20Regular />} aria-label={t('floor.activate')}
                          onClick={() => statusMutation.mutate({ id: f.id, status: 'Active' })} />
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
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
