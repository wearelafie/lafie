import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Tab, TabList, makeStyles, tokens } from '@fluentui/react-components'
import { useAppStore } from '@app/store/appStore'
import { getBuilding } from '@features/building/api'
import { getFloor } from '@features/floor/api'
import { getOrganization } from '@features/organization/api'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import { getSpace } from '../api'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  tabContent: { paddingTop: tokens.spacingVerticalM },
})
const statusColor = (s: string) => (s === 'Active' ? 'success' : s === 'Maintenance' ? 'warning' : 'danger')

export function SpaceDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const id = useAppStore((s) => s.selectedSpaceId)
  const setSelectedFacility = useAppStore((s) => s.setSelectedFacility)
  const setSelectedBuilding = useAppStore((s) => s.setSelectedBuilding)
  const setSelectedFloor = useAppStore((s) => s.setSelectedFloor)
  const setInfraTab = useAppStore((s) => s.setInfraTab)
  const b = (v: boolean) => (v ? t('common.yes') : t('common.no'))

  const q = useQuery({ queryKey: ['space', id], queryFn: () => getSpace(id!), enabled: !!id })
  const floorId = q.data?.floorId
  const floor = useQuery({ queryKey: ['floor', floorId], queryFn: () => getFloor(floorId!), enabled: !!floorId })
  const buildingId = floor.data?.buildingId
  const building = useQuery({ queryKey: ['building', buildingId], queryFn: () => getBuilding(buildingId!), enabled: !!buildingId })
  const facilityId = building.data?.facilityId
  const facility = useQuery({ queryKey: ['organization', facilityId], queryFn: () => getOrganization(facilityId!), enabled: !!facilityId })

  // Garde le contexte parent cohérent (fil d'Ariane + listes au retour).
  useEffect(() => { if (floorId) setSelectedFloor(floorId) }, [floorId, setSelectedFloor])
  useEffect(() => { if (buildingId) setSelectedBuilding(buildingId) }, [buildingId, setSelectedBuilding])
  useEffect(() => { if (facilityId) setSelectedFacility(facilityId) }, [facilityId, setSelectedFacility])

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('space.detail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('space.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('space.loadError')}</div></div>

  const s = q.data
  const rows: FieldRow[] = [
    { label: t('space.fields.code'), value: s.code },
    { label: t('space.fields.name'), value: s.name },
    { label: t('space.fields.type'), value: t(`space.types.${s.type}`) },
    { label: t('space.fields.area'), value: s.area ?? '—' },
    { label: t('space.fields.maxCapacity'), value: s.maxCapacity ?? '—' },
    { label: t('space.fields.accessible'), value: b(s.accessible) },
    { label: t('space.fields.status'), value: <Badge appearance="filled" color={statusColor(s.status)}>{t(`space.status.${s.status}`)}</Badge> },
    { label: t('space.fields.description'), value: s.description },
  ]
  const crumbs: Crumb[] = [
    { label: t('organization.title'), to: '/admin' },
    { label: facility.data?.officialName ?? '…', to: '/admin/facility', onClick: () => setInfraTab('buildings') },
    { label: building.data?.name ?? '…', to: '/admin/building', onClick: () => setInfraTab('floors') },
    { label: floor.data?.name ?? '…', to: '/admin/floor', onClick: () => setInfraTab('spaces') },
  ]

  return (
    <div className={styles.root}>
      <DetailHeader title={s.name} crumbs={crumbs} />
      <TabList selectedValue="overview"><Tab value="overview">{t('common.overview')}</Tab></TabList>
      <div className={styles.tabContent}><FieldList rows={rows} /></div>
    </div>
  )
}
