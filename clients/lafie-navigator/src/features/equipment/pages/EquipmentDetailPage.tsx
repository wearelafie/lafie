import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Tab, TabList, makeStyles, tokens } from '@fluentui/react-components'
import { useAppStore } from '@app/store/appStore'
import { getReferenceValues, refLabel, type ReferenceValue } from '@features/reference/api'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import { EQUIPMENT_CATEGORY_REFERENCE, equipmentKeys, getEquipmentItem } from '../api'
import { MovementsTab } from '../components/MovementsTab'
import { MaintenanceTab } from '../components/MaintenanceTab'

const TABS = ['overview', 'movements', 'maintenance'] as const
type TabKey = (typeof TABS)[number]

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  tabContent: { paddingTop: tokens.spacingVerticalM },
})

const statusColor = (s: string): 'success' | 'warning' | 'danger' | 'subtle' =>
  s === 'Operational' ? 'success' : s === 'UnderMaintenance' ? 'warning' : s === 'OutOfService' ? 'danger' : 'subtle'

export function EquipmentDetailPage() {
  const styles = useStyles()
  const { t, i18n } = useTranslation()
  const lang = i18n.language
  const id = useAppStore((s) => s.selectedEquipmentId)
  const [tab, setTab] = useState<TabKey>('overview')

  const q = useQuery({ queryKey: equipmentKeys.item(id ?? ''), queryFn: () => getEquipmentItem(id!), enabled: !!id })
  const categories = useQuery({ queryKey: ['reference', 'values', EQUIPMENT_CATEGORY_REFERENCE], queryFn: () => getReferenceValues(EQUIPMENT_CATEGORY_REFERENCE) })
  const catLabel = (code: string) => { const v = categories.data?.find((c: ReferenceValue) => c.code === code); return v ? refLabel(v, lang) : code }

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('equipment.detail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('equipment.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('equipment.loadError')}</div></div>

  const e = q.data
  const rows: FieldRow[] = [
    { label: t('equipment.fields.inventoryNumber'), value: e.inventoryNumber },
    { label: t('equipment.fields.serialNumber'), value: e.serialNumber },
    { label: t('equipment.fields.category'), value: catLabel(e.category) },
    { label: t('equipment.fields.status'), value: <Badge appearance="tint" color={statusColor(e.status)}>{t(`equipment.statuses.${e.status}`)}</Badge> },
    { label: t('equipment.fields.brand'), value: e.brand },
    { label: t('equipment.fields.model'), value: e.model },
    { label: t('equipment.fields.acquisitionDate'), value: e.acquisitionDate },
    { label: t('equipment.fields.warrantyEnd'), value: e.warrantyEnd },
    { label: t('equipment.fields.supplier'), value: e.supplier },
    { label: t('equipment.fields.estimatedLifespanMonths'), value: e.estimatedLifespanMonths },
    { label: t('equipment.fields.space'), value: e.spaceName },
    { label: t('equipment.fields.service'), value: e.serviceName },
    { label: t('equipment.fields.careUnit'), value: e.careUnitName },
    { label: t('equipment.fields.notes'), value: e.notes },
  ]
  const crumbs: Crumb[] = [
    { label: t('nav.infrastructure'), to: '/admin' },
    { label: t('equipment.title'), to: '/admin/equipment' },
  ]

  return (
    <div className={styles.root}>
      <DetailHeader title={e.name} crumbs={crumbs} />
      <TabList selectedValue={tab} onTabSelect={(_, dt) => setTab(dt.value as TabKey)}>
        <Tab value="overview">{t('common.overview')}</Tab>
        <Tab value="movements">{t('equipment.tabs.movements')}</Tab>
        <Tab value="maintenance">{t('equipment.tabs.maintenance')}</Tab>
      </TabList>
      <div className={styles.tabContent}>
        {tab === 'overview' && <FieldList rows={rows} />}
        {tab === 'movements' && <MovementsTab equipmentId={e.id} facilityId={e.facilityId} currentSpaceId={e.spaceId} />}
        {tab === 'maintenance' && <MaintenanceTab equipmentId={e.id} />}
      </div>
    </div>
  )
}
