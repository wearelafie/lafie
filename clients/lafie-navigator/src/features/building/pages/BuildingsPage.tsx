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
import { Add20Regular, ArrowLeft20Regular, Edit20Regular, Layer20Regular, Wrench20Regular, PlugConnected20Regular, PlugDisconnected20Regular } from '@fluentui/react-icons'
import { useNavigate } from '@tanstack/react-router'
import { useAppStore } from '@app/store/appStore'
import { getOrganization } from '@features/organization/api'
import {
  BUILDING_STATUSES,
  buildingKeys,
  getBuildings,
  setBuildingStatus,
  type Building,
  type BuildingFilter,
  type BuildingStatus,
} from '../api'
import { BuildingFormDialog } from '../components/BuildingFormDialog'

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

export function BuildingsPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const facilityId = useAppStore((s) => s.selectedFacilityId)
  const setSelectedBuilding = useAppStore((s) => s.setSelectedBuilding)
  const openFloors = (id: string) => { setSelectedBuilding(id); void navigate({ to: '/admin/floors' }) }

  const facility = useQuery({
    queryKey: ['organization', facilityId],
    queryFn: () => getOrganization(facilityId!),
    enabled: !!facilityId,
  })

  const [draft, setDraft] = useState<{ search: string; status: string }>({ search: '', status: ANY })
  const [applied, setApplied] = useState<BuildingFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Building | null>(null)

  const query = useQuery({
    queryKey: buildingKeys.list(facilityId ?? '', applied),
    queryFn: () => getBuildings(facilityId!, applied),
    enabled: !!facilityId,
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: BuildingStatus }) => setBuildingStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: buildingKeys.all(facilityId ?? '') }),
  })

  if (!facilityId) {
    return (
      <div className={styles.root}>
        <Title3>{t('building.title')}</Title3>
        <div className={styles.state}>
          {t('building.noFacility')}{' '}
          <Link to={"/admin" as never}>{t('building.goFacilities')}</Link>
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
        <Link to={"/admin" as never} className={styles.back}><ArrowLeft20Regular />{t('building.goFacilities')}</Link>
        <div className={styles.header}>
          <Title3>{t('building.titleFor', { facility: facility.data?.officialName ?? '…' })}</Title3>
          <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setEditing(null); setFormOpen(true) }}>
            {t('building.new')}
          </Button>
        </div>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('building.filters.search')}</span>
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
            placeholder={t('building.filters.searchPlaceholder')} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('building.fields.status')}</span>
          <Dropdown
            value={draft.status === ANY ? t('building.filters.all') : t(`building.status.${draft.status}`)}
            selectedOptions={[draft.status]}
            onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue! }))}
          >
            <Option value={ANY}>{t('building.filters.all')}</Option>
            {BUILDING_STATUSES.map((st) => <Option key={st} value={st}>{t(`building.status.${st}`)}</Option>)}
          </Dropdown>
        </div>
        <Button appearance="primary" onClick={runSearch}>{t('building.filters.apply')}</Button>
      </div>

      {query.isPending && <div className={styles.state}><Spinner label={t('building.loading')} /></div>}
      {query.isError && <div className={styles.state}><span className={styles.error}>{t('building.loadError')}</span></div>}
      {query.isSuccess && data!.items.length === 0 && <div className={styles.state}>{t('building.empty')}</div>}

      {query.isSuccess && data!.items.length > 0 && (
        <>
          <Table aria-label={t('building.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('building.fields.code')}</TableHeaderCell>
                <TableHeaderCell>{t('building.fields.name')}</TableHeaderCell>
                <TableHeaderCell>{t('building.fields.location')}</TableHeaderCell>
                <TableHeaderCell>{t('building.fields.status')}</TableHeaderCell>
                <TableHeaderCell>{t('building.fields.actions')}</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data!.items.map((b) => (
                <TableRow key={b.id}>
                  <TableCell><TableCellLayout>{b.code}</TableCellLayout></TableCell>
                  <TableCell>{b.name}</TableCell>
                  <TableCell>{b.location ?? '—'}</TableCell>
                  <TableCell><Badge appearance="filled" color={statusColor(b.status)}>{t(`building.status.${b.status}`)}</Badge></TableCell>
                  <TableCell>
                    <div className={styles.actions}>
                      <Button size="small" appearance="subtle" icon={<Layer20Regular />} aria-label={t('building.floors')}
                        onClick={() => openFloors(b.id)} />
                      <Button size="small" appearance="subtle" icon={<Edit20Regular />} aria-label={t('building.edit')}
                        onClick={() => { setEditing(b); setFormOpen(true) }} />
                      <Button size="small" appearance="subtle" icon={<Wrench20Regular />} aria-label={t('building.status.Maintenance')}
                        onClick={() => statusMutation.mutate({ id: b.id, status: 'Maintenance' })} />
                      {b.status === 'Active' ? (
                        <Button size="small" appearance="subtle" icon={<PlugDisconnected20Regular />} aria-label={t('building.deactivate')}
                          onClick={() => statusMutation.mutate({ id: b.id, status: 'Inactive' })} />
                      ) : (
                        <Button size="small" appearance="subtle" icon={<PlugConnected20Regular />} aria-label={t('building.activate')}
                          onClick={() => statusMutation.mutate({ id: b.id, status: 'Active' })} />
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <div className={styles.pager}>
            <span className={styles.label}>{t('building.pager', { page, totalPages, total: data!.total })}</span>
            <Button size="small" disabled={page <= 1} onClick={() => goPage(page - 1)}>{t('building.prev')}</Button>
            <Button size="small" disabled={page >= totalPages} onClick={() => goPage(page + 1)}>{t('building.next')}</Button>
          </div>
        </>
      )}

      <BuildingFormDialog open={formOpen} onOpenChange={setFormOpen} facilityId={facilityId} building={editing} />
    </div>
  )
}
