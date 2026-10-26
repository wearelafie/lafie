import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Tab, TabList, makeStyles, tokens } from '@fluentui/react-components'
import { useAppStore } from '@app/store/appStore'
import { getOrganization } from '@features/organization/api'
import { CareUnitsPage } from '@features/care-unit'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import { getService } from '../api'

const TABS = ['overview', 'careUnits'] as const
type TabKey = (typeof TABS)[number]

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  tabContent: { paddingTop: tokens.spacingVerticalM },
})
const statusColor = (s: string) => (s === 'Active' ? 'success' : 'danger')

export function ServiceDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const id = useAppStore((s) => s.selectedServiceId)
  const setSelectedFacility = useAppStore((s) => s.setSelectedFacility)
  const setInfraTab = useAppStore((s) => s.setInfraTab)
  const pendingTab = useAppStore((s) => s.infraTab)
  const [tab, setTab] = useState<TabKey>(() => (TABS.includes(pendingTab as TabKey) ? (pendingTab as TabKey) : 'overview'))

  const q = useQuery({ queryKey: ['service', id], queryFn: () => getService(id!), enabled: !!id })
  const facilityId = q.data?.facilityId
  const facility = useQuery({ queryKey: ['organization', facilityId], queryFn: () => getOrganization(facilityId!), enabled: !!facilityId })
  const parentId = q.data?.parentServiceId ?? undefined
  const parent = useQuery({ queryKey: ['service', parentId], queryFn: () => getService(parentId!), enabled: !!parentId })

  useEffect(() => { if (facilityId) setSelectedFacility(facilityId) }, [facilityId, setSelectedFacility])

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('service.detail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('service.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('service.loadError')}</div></div>

  const s = q.data
  const catLabel = (c: string) => { const l = t(`service.categories.${c}`); return l.startsWith('service.') ? c : l }
  const rows: FieldRow[] = [
    { label: t('service.fields.code'), value: s.code },
    { label: t('service.fields.name'), value: s.name },
    { label: t('service.fields.category'), value: catLabel(s.category) },
    { label: t('service.fields.parent'), value: s.parentServiceId ? (parent.data?.name ?? '…') : '—' },
    { label: t('service.fields.head'), value: s.head },
    { label: t('service.fields.displayOrder'), value: s.displayOrder },
    { label: t('service.fields.status'), value: <Badge appearance="filled" color={statusColor(s.status)}>{t(`service.status.${s.status}`)}</Badge> },
    { label: t('service.fields.description'), value: s.description },
  ]
  const crumbs: Crumb[] = [
    { label: t('organization.title'), to: '/admin' },
    { label: facility.data?.officialName ?? '…', to: '/admin/facility', onClick: () => setInfraTab('services') },
  ]

  return (
    <div className={styles.root}>
      <DetailHeader title={s.name} crumbs={crumbs} />
      <TabList selectedValue={tab} onTabSelect={(_, dt) => setTab(dt.value as TabKey)}>
        <Tab value="overview">{t('common.overview')}</Tab>
        <Tab value="careUnits">{t('service.tabs.careUnits')}</Tab>
      </TabList>
      <div className={styles.tabContent}>
        {tab === 'overview' ? <FieldList rows={rows} /> : <CareUnitsPage />}
      </div>
    </div>
  )
}
