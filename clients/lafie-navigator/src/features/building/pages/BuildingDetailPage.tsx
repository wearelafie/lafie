import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Tab, TabList, makeStyles, tokens } from '@fluentui/react-components'
import { useAppStore } from '@app/store/appStore'
import { getOrganization } from '@features/organization/api'
import { FloorsPage } from '@features/floor'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import { getBuilding } from '../api'

const TABS = ['overview', 'floors'] as const
type TabKey = (typeof TABS)[number]

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  tabContent: { paddingTop: tokens.spacingVerticalM },
})
const statusColor = (s: string) => (s === 'Active' ? 'success' : s === 'Maintenance' ? 'warning' : 'danger')

export function BuildingDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const id = useAppStore((s) => s.selectedBuildingId)
  const setSelectedFacility = useAppStore((s) => s.setSelectedFacility)
  const setInfraTab = useAppStore((s) => s.setInfraTab)
  const pendingTab = useAppStore((s) => s.infraTab)
  const [tab, setTab] = useState<TabKey>(() => (TABS.includes(pendingTab as TabKey) ? (pendingTab as TabKey) : 'overview'))

  const q = useQuery({ queryKey: ['building', id], queryFn: () => getBuilding(id!), enabled: !!id })
  const facilityId = q.data?.facilityId
  const facility = useQuery({ queryKey: ['organization', facilityId], queryFn: () => getOrganization(facilityId!), enabled: !!facilityId })

  // Garde le contexte établissement cohérent (fil d'Ariane + liste des bâtiments au retour).
  useEffect(() => { if (facilityId) setSelectedFacility(facilityId) }, [facilityId, setSelectedFacility])

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('building.detail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('building.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('building.loadError')}</div></div>

  const b = q.data
  const rows: FieldRow[] = [
    { label: t('building.fields.code'), value: b.code },
    { label: t('building.fields.name'), value: b.name },
    { label: t('building.fields.location'), value: b.location },
    { label: t('building.fields.status'), value: <Badge appearance="filled" color={statusColor(b.status)}>{t(`building.status.${b.status}`)}</Badge> },
    { label: t('building.fields.description'), value: b.description },
  ]
  const crumbs: Crumb[] = [
    { label: t('organization.title'), to: '/admin' },
    { label: facility.data?.officialName ?? '…', to: '/admin/facility', onClick: () => setInfraTab('buildings') },
  ]

  return (
    <div className={styles.root}>
      <DetailHeader title={b.name} crumbs={crumbs} />
      <TabList selectedValue={tab} onTabSelect={(_, dt) => setTab(dt.value as TabKey)}>
        <Tab value="overview">{t('common.overview')}</Tab>
        <Tab value="floors">{t('building.tabs.floors')}</Tab>
      </TabList>
      <div className={styles.tabContent}>
        {tab === 'overview' ? <FieldList rows={rows} /> : <FloorsPage />}
      </div>
    </div>
  )
}
