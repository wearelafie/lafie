import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
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
  mergeClasses,
  tokens,
} from '@fluentui/react-components'
import { Add20Regular, Edit20Regular, PlugConnected20Regular, PlugDisconnected20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import {
  ORGANIZATION_STATUSES,
  ORGANIZATION_TYPES,
  getOrganizations,
  organizationKeys,
  setOrganizationActive,
  type Organization,
  type OrganizationFilter,
} from '../api'
import { OrganizationFormDialog } from '../components/OrganizationFormDialog'
import { useMutation, useQueryClient } from '@tanstack/react-query'

const PAGE_SIZE = 10

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

const ANY = '__any__'
const statusColor = (s: string) => (s === 'Active' ? 'success' : s === 'Inactive' ? 'danger' : 'warning')

export function OrganizationsPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const selectedId = useAppStore((s) => s.selectedFacilityId)
  const setSelected = useAppStore((s) => s.setSelectedFacility)

  // Saisie des filtres (non appliquée tant qu'on ne clique pas « Rechercher »).
  const [draft, setDraft] = useState<{ search: string; type: string; status: string }>({ search: '', type: ANY, status: ANY })
  const [applied, setApplied] = useState<OrganizationFilter>({ page: 1, pageSize: PAGE_SIZE })

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Organization | null>(null)

  const query = useQuery({
    queryKey: organizationKeys.list(applied),
    queryFn: () => getOrganizations(applied),
  })

  const activeMutation = useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) => setOrganizationActive(id, active),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: organizationKeys.all }),
  })

  const runSearch = () =>
    setApplied({
      search: draft.search || undefined,
      type: draft.type === ANY ? undefined : draft.type,
      status: draft.status === ANY ? undefined : draft.status,
      page: 1,
      pageSize: PAGE_SIZE,
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
          <SearchBox
            value={draft.search}
            onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
            placeholder={t('organization.filters.searchPlaceholder')}
          />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('organization.fields.type')}</span>
          <Dropdown
            value={draft.type === ANY ? t('organization.filters.all') : t(`organization.types.${draft.type}`)}
            selectedOptions={[draft.type]}
            onOptionSelect={(_, d) => setDraft((s) => ({ ...s, type: d.optionValue! }))}
          >
            <Option value={ANY}>{t('organization.filters.all')}</Option>
            {ORGANIZATION_TYPES.map((ty) => <Option key={ty} value={ty}>{t(`organization.types.${ty}`)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('organization.fields.status')}</span>
          <Dropdown
            value={draft.status === ANY ? t('organization.filters.all') : t(`organization.status.${draft.status}`)}
            selectedOptions={[draft.status]}
            onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue! }))}
          >
            <Option value={ANY}>{t('organization.filters.all')}</Option>
            {ORGANIZATION_STATUSES.map((st) => <Option key={st} value={st}>{t(`organization.status.${st}`)}</Option>)}
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
                <TableHeaderCell>{t('organization.fields.name')}</TableHeaderCell>
                <TableHeaderCell>{t('organization.fields.type')}</TableHeaderCell>
                <TableHeaderCell>{t('organization.fields.code')}</TableHeaderCell>
                <TableHeaderCell>{t('organization.fields.status')}</TableHeaderCell>
                <TableHeaderCell>{t('organization.fields.city')}</TableHeaderCell>
                <TableHeaderCell>{t('organization.fields.actions')}</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data!.items.map((org) => (
                <TableRow
                  key={org.id}
                  className={mergeClasses(styles.clickable, selectedId === org.id && styles.rowSel)}
                  onClick={() => setSelected(org.id)}
                >
                  <TableCell><TableCellLayout>{org.name}</TableCellLayout></TableCell>
                  <TableCell>{t(`organization.types.${org.type}`)}</TableCell>
                  <TableCell>{org.code ?? '—'}</TableCell>
                  <TableCell>
                    <Badge appearance="filled" color={statusColor(org.status)}>
                      {t(`organization.status.${org.status}`)}
                    </Badge>
                  </TableCell>
                  <TableCell>{org.city ?? '—'}</TableCell>
                  <TableCell onClick={(e) => e.stopPropagation()}>
                    <div className={styles.actions}>
                      <Button size="small" appearance="subtle" icon={<Edit20Regular />}
                        aria-label={t('organization.edit')}
                        onClick={() => { setEditing(org); setFormOpen(true) }} />
                      {org.status === 'Active' ? (
                        <Button size="small" appearance="subtle" icon={<PlugDisconnected20Regular />}
                          aria-label={t('organization.deactivate')}
                          onClick={() => activeMutation.mutate({ id: org.id, active: false })} />
                      ) : (
                        <Button size="small" appearance="subtle" icon={<PlugConnected20Regular />}
                          aria-label={t('organization.activate')}
                          onClick={() => activeMutation.mutate({ id: org.id, active: true })} />
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          <div className={styles.pager}>
            <span className={styles.label}>
              {t('organization.pager', { page, totalPages, total: data!.total })}
            </span>
            <Button size="small" disabled={page <= 1} onClick={() => goPage(page - 1)}>{t('organization.prev')}</Button>
            <Button size="small" disabled={page >= totalPages} onClick={() => goPage(page + 1)}>{t('organization.next')}</Button>
          </div>
        </>
      )}

      <OrganizationFormDialog open={formOpen} onOpenChange={setFormOpen} organization={editing} />
    </div>
  )
}
