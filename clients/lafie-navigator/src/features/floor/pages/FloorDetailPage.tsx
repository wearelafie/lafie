import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Tab, TabList, makeStyles, tokens } from '@fluentui/react-components'
import { useAppStore } from '@app/store/appStore'
import { getBuilding } from '@features/building/api'
import { getOrganization } from '@features/organization/api'
import { SpacesPage } from '@features/space'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import { getFloor } from '../api'

const TABS = ['overview', 'spaces'] as const
type TabKey = (typeof TABS)[number]

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  tabContent: { paddingTop: tokens.spacingVerticalM },
})
const statusColor = (s: string) => (s === 'Active' ? 'success' : s === 'Maintenance' ? 'warning' : 'danger')

export function FloorDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const id = useAppStore((s) => s.selectedFloorId)
  const setSelectedFacility = useAppStore((s) => s.setSelectedFacility)
  const setSelectedBuilding = useAppStore((s) => s.setSelectedBuilding)
  const setInfraTab = useAppStore((s) => s.setInfraTab)
  const pendingTab = useAppStore((s) => s.infraTab)
  const [tab, setTab] = useState<TabKey>(() => (TABS.includes(pendingTab as TabKey) ? (pendingTab as TabKey) : 'overview'))

  const q = useQuery({ queryKey: ['floor', id], queryFn: () => getFloor(id!), enabled: !!id })
  const buildingId = q.data?.buildingId
  const building = useQuery({ queryKey: ['building', buildingId], queryFn: () => getBuilding(buildingId!), enabled: !!buildingId })
  const facilityId = building.data?.facilityId
  const facility = useQuery({ queryKey: ['organization', facilityId], queryFn: () => getOrganization(facilityId!), enabled: !!facilityId })

  // Garde le contexte parent cohérent (fil d'Ariane + listes au retour).
  useEffect(() => { if (buildingId) setSelectedBuilding(buildingId) }, [buildingId, setSelectedBuilding])
  useEffect(() => { if (facilityId) setSelectedFacility(facilityId) }, [facilityId, setSelectedFacility])

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('floor.detail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('floor.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('floor.loadError')}</div></div>

  const f = q.data
  const rows: FieldRow[] = [
    { label: t('floor.fields.code'), value: f.code },
    { label: t('floor.fields.name'), value: f.name },
    { label: t('floor.fields.level'), value: f.level ?? '—' },
    { label: t('floor.fields.displayOrder'), value: f.displayOrder },
    { label: t('floor.fields.status'), value: <Badge appearance="filled" color={statusColor(f.status)}>{t(`floor.status.${f.status}`)}</Badge> },
    { label: t('floor.fields.description'), value: f.description },
  ]
  const crumbs: Crumb[] = [
    { label: t('organization.title'), to: '/admin' },
    { label: facility.data?.officialName ?? '…', to: '/admin/facility', onClick: () => setInfraTab('buildings') },
    { label: building.data?.name ?? '…', to: '/admin/building', onClick: () => setInfraTab('floors') },
  ]

  return (
    <div className={styles.root}>
      <DetailHeader title={f.name} crumbs={crumbs} />
      <TabList selectedValue={tab} onTabSelect={(_, dt) => setTab(dt.value as TabKey)}>
        <Tab value="overview">{t('common.overview')}</Tab>
        <Tab value="spaces">{t('floor.tabs.spaces')}</Tab>
      </TabList>
      <div className={styles.tabContent}>
        {tab === 'overview' ? <FieldList rows={rows} /> : <SpacesPage />}
      </div>
    </div>
  )
}
