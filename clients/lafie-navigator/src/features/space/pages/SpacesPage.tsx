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
import { getFloor } from '@features/floor/api'
import { SPACE_STATUSES, SPACE_TYPES, getSpaces, setSpaceStatus, spaceKeys, type Space, type SpaceFilter, type SpaceStatus } from '../api'
import { SpaceFormDialog } from '../components/SpaceFormDialog'

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

export function SpacesPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const floorId = useAppStore((s) => s.selectedFloorId)

  const floor = useQuery({ queryKey: ['floor', floorId], queryFn: () => getFloor(floorId!), enabled: !!floorId })

  const [draft, setDraft] = useState<{ search: string; type: string; status: string }>({ search: '', type: ANY, status: ANY })
  const [applied, setApplied] = useState<SpaceFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Space | null>(null)

  const query = useQuery({
    queryKey: spaceKeys.list(floorId ?? '', applied),
    queryFn: () => getSpaces(floorId!, applied),
    enabled: !!floorId,
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: SpaceStatus }) => setSpaceStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: spaceKeys.all(floorId ?? '') }),
  })

  if (!floorId) {
    return (
      <div className={styles.root}>
        <Title3>{t('space.title')}</Title3>
        <div className={styles.state}>
          {t('space.noFloor')} <Link to={'/admin/floors' as never}>{t('space.goFloors')}</Link>
        </div>
      </div>
    )
  }

  const runSearch = () =>
    setApplied({
      search: draft.search || undefined,
      type: draft.type === ANY ? undefined : draft.type,
      status: draft.status === ANY ? undefined : draft.status,
      page: 1, pageSize: PAGE_SIZE,
    })
  const goPage = (p: number) => setApplied((f) => ({ ...f, page: p }))

  const data = query.data
  const page = data?.page ?? 1
  const totalPages = data?.totalPages ?? 0

  return (
    <div className={styles.root}>
      <div className={styles.top}>
        <Link to={'/admin/floors' as never} className={styles.back}><ArrowLeft20Regular />{t('space.goFloors')}</Link>
        <div className={styles.header}>
          <Title3>{t('space.titleFor', { floor: floor.data?.name ?? '…' })}</Title3>
          <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setEditing(null); setFormOpen(true) }}>
            {t('space.new')}
          </Button>
        </div>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('space.filters.search')}</span>
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
            placeholder={t('space.filters.searchPlaceholder')} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('space.fields.type')}</span>
          <Dropdown
            value={draft.type === ANY ? t('space.filters.all') : t(`space.types.${draft.type}`)}
            selectedOptions={[draft.type]}
            onOptionSelect={(_, d) => setDraft((s) => ({ ...s, type: d.optionValue! }))}
          >
            <Option value={ANY}>{t('space.filters.all')}</Option>
            {SPACE_TYPES.map((ty) => <Option key={ty} value={ty}>{t(`space.types.${ty}`)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('space.fields.status')}</span>
          <Dropdown
            value={draft.status === ANY ? t('space.filters.all') : t(`space.status.${draft.status}`)}
            selectedOptions={[draft.status]}
            onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue! }))}
          >
            <Option value={ANY}>{t('space.filters.all')}</Option>
            {SPACE_STATUSES.map((st) => <Option key={st} value={st}>{t(`space.status.${st}`)}</Option>)}
          </Dropdown>
        </div>
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
              {data!.items.map((sp) => (
                <TableRow key={sp.id}>
                  <TableCell><TableCellLayout>{sp.code}</TableCellLayout></TableCell>
                  <TableCell>{sp.name}</TableCell>
                  <TableCell>{t(`space.types.${sp.type}`)}</TableCell>
                  <TableCell>{sp.maxCapacity ?? '—'}</TableCell>
                  <TableCell><Badge appearance="filled" color={statusColor(sp.status)}>{t(`space.status.${sp.status}`)}</Badge></TableCell>
                  <TableCell>
                    <div className={styles.actions}>
                      <Button size="small" appearance="subtle" icon={<Edit20Regular />} aria-label={t('space.edit')}
                        onClick={() => { setEditing(sp); setFormOpen(true) }} />
                      <Button size="small" appearance="subtle" icon={<Wrench20Regular />} aria-label={t('space.status.Maintenance')}
                        onClick={() => statusMutation.mutate({ id: sp.id, status: 'Maintenance' })} />
                      {sp.status === 'Active' ? (
                        <Button size="small" appearance="subtle" icon={<PlugDisconnected20Regular />} aria-label={t('space.deactivate')}
                          onClick={() => statusMutation.mutate({ id: sp.id, status: 'Inactive' })} />
                      ) : (
                        <Button size="small" appearance="subtle" icon={<PlugConnected20Regular />} aria-label={t('space.activate')}
                          onClick={() => statusMutation.mutate({ id: sp.id, status: 'Active' })} />
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
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
