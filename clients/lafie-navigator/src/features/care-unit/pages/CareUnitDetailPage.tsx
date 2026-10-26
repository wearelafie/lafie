import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  SearchBox, Spinner, Tab, TabList,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Delete20Regular, Dismiss24Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { getOrganization } from '@features/organization/api'
import { getService } from '@features/service/api'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import {
  assignSpace, careUnitKeys, getCareUnit, getCareUnitSpaces, getFacilitySpaces, unassignSpace,
} from '../api'

const TABS = ['overview', 'spaces'] as const
type TabKey = (typeof TABS)[number]

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  tabContent: { paddingTop: tokens.spacingVerticalM },
  panel: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM },
  toolbar: { display: 'flex', justifyContent: 'flex-end' },
  pickerList: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXS, maxHeight: '360px', overflowY: 'auto', paddingTop: tokens.spacingVerticalS },
  pickerRow: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM, padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`, borderBottom: `1px solid ${tokens.colorNeutralStroke2}` },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
})
const statusColor = (s: string) => (s === 'Active' ? 'success' : s === 'Maintenance' ? 'warning' : 'danger')

function SpacesPanel({ careUnitId, facilityId }: { careUnitId: string; facilityId: string }) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [pickerOpen, setPickerOpen] = useState(false)
  const [search, setSearch] = useState('')

  const assigned = useQuery({ queryKey: careUnitKeys.spaces(careUnitId), queryFn: () => getCareUnitSpaces(careUnitId) })
  const facilitySpaces = useQuery({
    queryKey: ['facilitySpaces', facilityId],
    queryFn: () => getFacilitySpaces(facilityId),
    enabled: pickerOpen,
  })
  const invalidate = () => queryClient.invalidateQueries({ queryKey: careUnitKeys.spaces(careUnitId) })
  const assignMut = useMutation({ mutationFn: (spaceId: string) => assignSpace(careUnitId, spaceId), onSuccess: invalidate })
  const unassignMut = useMutation({ mutationFn: (spaceId: string) => unassignSpace(careUnitId, spaceId), onSuccess: invalidate })

  const assignedIds = new Set((assigned.data ?? []).map((s) => s.id))
  const candidates = (facilitySpaces.data?.items ?? [])
    .filter((s) => !assignedIds.has(s.id))
    .filter((s) => !search || s.name.toLowerCase().includes(search.toLowerCase()) || s.code.toLowerCase().includes(search.toLowerCase()))

  return (
    <div className={styles.panel}>
      <div className={styles.toolbar}>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => setPickerOpen(true)}>{t('careUnit.assignSpace')}</Button>
      </div>

      {assigned.isPending && <div className={styles.state}><Spinner label={t('careUnit.loading')} /></div>}
      {assigned.isSuccess && assigned.data.length === 0 && <div className={styles.state}>{t('careUnit.noSpaces')}</div>}
      {assigned.isSuccess && assigned.data.length > 0 && (
        <Table aria-label={t('careUnit.spaces')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('space.fields.code')}</TableHeaderCell>
              <TableHeaderCell>{t('space.fields.name')}</TableHeaderCell>
              <TableHeaderCell>{t('space.fields.type')}</TableHeaderCell>
              <TableHeaderCell>{t('space.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('space.fields.actions')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {assigned.data.map((s) => (
              <TableRow key={s.id}>
                <TableCell><TableCellLayout>{s.code}</TableCellLayout></TableCell>
                <TableCell>{s.name}</TableCell>
                <TableCell>{t(`space.types.${s.type}`)}</TableCell>
                <TableCell><Badge appearance="filled" color={statusColor(s.status)}>{t(`space.status.${s.status}`)}</Badge></TableCell>
                <TableCell>
                  <Button appearance="subtle" icon={<Delete20Regular />} aria-label={t('careUnit.unassignSpace')}
                    onClick={() => unassignMut.mutate(s.id)} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <Dialog open={pickerOpen} onOpenChange={(_, d) => setPickerOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle action={<Button appearance="subtle" icon={<Dismiss24Regular />} aria-label={t('settings.close')} onClick={() => setPickerOpen(false)} />}>
              {t('careUnit.assignSpace')}
            </DialogTitle>
            <DialogContent>
              <SearchBox value={search} onChange={(_, d) => setSearch(d.value)} placeholder={t('careUnit.spacePickerPlaceholder')} />
              {facilitySpaces.isPending && <div className={styles.state}><Spinner label={t('careUnit.loading')} /></div>}
              {facilitySpaces.isSuccess && candidates.length === 0 && <div className={styles.state}>{t('careUnit.noCandidateSpaces')}</div>}
              <div className={styles.pickerList}>
                {candidates.map((s) => (
                  <div key={s.id} className={styles.pickerRow}>
                    <span>{s.code} — {s.name} <span className={styles.muted}>({t(`space.types.${s.type}`)})</span></span>
                    <Button size="small" appearance="primary" disabled={assignMut.isPending}
                      onClick={() => assignMut.mutate(s.id)}>{t('careUnit.assign')}</Button>
                  </div>
                ))}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setPickerOpen(false)}>{t('careUnit.close')}</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}

export function CareUnitDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const id = useAppStore((s) => s.selectedCareUnitId)
  const setSelectedFacility = useAppStore((s) => s.setSelectedFacility)
  const setSelectedService = useAppStore((s) => s.setSelectedService)
  const setInfraTab = useAppStore((s) => s.setInfraTab)
  const pendingTab = useAppStore((s) => s.infraTab)
  const [tab, setTab] = useState<TabKey>(() => (TABS.includes(pendingTab as TabKey) ? (pendingTab as TabKey) : 'overview'))

  const q = useQuery({ queryKey: ['careUnit', id], queryFn: () => getCareUnit(id!), enabled: !!id })
  const serviceId = q.data?.serviceId
  const service = useQuery({ queryKey: ['service', serviceId], queryFn: () => getService(serviceId!), enabled: !!serviceId })
  const facilityId = q.data?.facilityId
  const facility = useQuery({ queryKey: ['organization', facilityId], queryFn: () => getOrganization(facilityId!), enabled: !!facilityId })

  useEffect(() => { if (serviceId) setSelectedService(serviceId) }, [serviceId, setSelectedService])
  useEffect(() => { if (facilityId) setSelectedFacility(facilityId) }, [facilityId, setSelectedFacility])

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('careUnit.detail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('careUnit.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('careUnit.loadError')}</div></div>

  const u = q.data
  const typeLabel = (c: string) => { const l = t(`careUnit.types.${c}`); return l.startsWith('careUnit.') ? c : l }
  const rows: FieldRow[] = [
    { label: t('careUnit.fields.code'), value: u.code },
    { label: t('careUnit.fields.name'), value: u.name },
    { label: t('careUnit.fields.type'), value: typeLabel(u.type) },
    { label: t('careUnit.fields.bedCapacity'), value: u.bedCapacity ?? '—' },
    { label: t('careUnit.fields.displayOrder'), value: u.displayOrder },
    { label: t('careUnit.fields.status'), value: <Badge appearance="filled" color={statusColor(u.status)}>{t(`careUnit.status.${u.status}`)}</Badge> },
    { label: t('careUnit.fields.description'), value: u.description },
  ]
  const crumbs: Crumb[] = [
    { label: t('organization.title'), to: '/admin' },
    { label: facility.data?.officialName ?? '…', to: '/admin/facility', onClick: () => setInfraTab('services') },
    { label: service.data?.name ?? '…', to: '/admin/service', onClick: () => setInfraTab('careUnits') },
  ]

  return (
    <div className={styles.root}>
      <DetailHeader title={u.name} crumbs={crumbs} />
      <TabList selectedValue={tab} onTabSelect={(_, dt) => setTab(dt.value as TabKey)}>
        <Tab value="overview">{t('common.overview')}</Tab>
        <Tab value="spaces">{t('careUnit.tabs.spaces')}</Tab>
      </TabList>
      <div className={styles.tabContent}>
        {tab === 'overview' ? <FieldList rows={rows} /> : <SpacesPanel careUnitId={u.id} facilityId={u.facilityId} />}
      </div>
    </div>
  )
}
