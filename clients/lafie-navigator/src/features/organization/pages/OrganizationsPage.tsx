import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import {
  Badge, Button, Dropdown, Option, SearchBox, Spinner,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  Title3, makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Building20Regular, Edit20Regular, PlugConnected20Regular, PlugDisconnected20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import {
  FACILITY_TYPES, OPERATIONAL_STATUSES, getOrganizations, organizationKeys, setOrganizationStatus,
  type Organization, type OrganizationFilter, type OperationalStatus,
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
  clickable: { cursor: 'pointer' },
  actions: { display: 'flex', columnGap: tokens.spacingHorizontalXS },
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
  const openBuildings = (id: string) => { setSelected(id); void navigate({ to: '/admin/buildings' }) }

  const [draft, setDraft] = useState<{ search: string; type: string; status: string }>({ search: '', type: ANY, status: ANY })
  const [applied, setApplied] = useState<OrganizationFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Organization | null>(null)

  const query = useQuery({ queryKey: organizationKeys.list(applied), queryFn: () => getOrganizations(applied) })

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: OperationalStatus }) => setOrganizationStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: organizationKeys.all }),
  })

  const runSearch = () => setApplied({
    search: draft.search || undefined,
    facilityTypeCode: draft.type === ANY ? undefined : draft.type,
    operationalStatus: draft.status === ANY ? undefined : draft.status,
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
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
            placeholder={t('organization.filters.searchPlaceholder')} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('organization.fields.facilityType')}</span>
          <Dropdown value={draft.type === ANY ? t('organization.filters.all') : typeLabel(t, draft.type)}
            selectedOptions={[draft.type]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, type: d.optionValue! }))}>
            <Option value={ANY}>{t('organization.filters.all')}</Option>
            {FACILITY_TYPES.map((c) => <Option key={c} value={c}>{t(`organization.facilityTypes.${c}`)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('organization.fields.status')}</span>
          <Dropdown value={draft.status === ANY ? t('organization.filters.all') : t(`organization.operationalStatus.${draft.status}`)}
            selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue! }))}>
            <Option value={ANY}>{t('organization.filters.all')}</Option>
            {OPERATIONAL_STATUSES.map((st) => <Option key={st} value={st}>{t(`organization.operationalStatus.${st}`)}</Option>)}
          </Dropdown>
        </div>
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
              {data!.items.map((o) => (
                <TableRow key={o.id} className={mergeClasses(styles.clickable, selectedId === o.id && styles.rowSel)}
                  onClick={() => setSelected(o.id)}>
                  <TableCell><TableCellLayout>{o.code}</TableCellLayout></TableCell>
                  <TableCell>{o.officialName}</TableCell>
                  <TableCell>{typeLabel(t, o.facilityTypeCode)}</TableCell>
                  <TableCell><Badge appearance="filled" color={statusColor(o.operationalStatus)}>{t(`organization.operationalStatus.${o.operationalStatus}`)}</Badge></TableCell>
                  <TableCell>{o.locality ?? '—'}</TableCell>
                  <TableCell onClick={(e) => e.stopPropagation()}>
                    <div className={styles.actions}>
                      <Button size="small" appearance="subtle" icon={<Building20Regular />} aria-label={t('organization.buildings')}
                        onClick={() => openBuildings(o.id)} />
                      <Button size="small" appearance="subtle" icon={<Edit20Regular />} aria-label={t('organization.edit')}
                        onClick={() => { setEditing(o); setFormOpen(true) }} />
                      {o.operationalStatus === 'Active' ? (
                        <Button size="small" appearance="subtle" icon={<PlugDisconnected20Regular />} aria-label={t('organization.close')}
                          onClick={() => statusMutation.mutate({ id: o.id, status: 'TemporarilyClosed' })} />
                      ) : (
                        <Button size="small" appearance="subtle" icon={<PlugConnected20Regular />} aria-label={t('organization.reopen')}
                          onClick={() => statusMutation.mutate({ id: o.id, status: 'Active' })} />
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
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
