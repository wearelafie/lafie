import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Tab, TabList, makeStyles, tokens } from '@fluentui/react-components'
import { useAppStore } from '@app/store/appStore'
import { DetailHeader, FieldList, type FieldRow } from '@shared/components'
import { getBuilding } from '../api'

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
  const q = useQuery({ queryKey: ['building', id], queryFn: () => getBuilding(id!), enabled: !!id })

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
  return (
    <div className={styles.root}>
      <DetailHeader title={b.name} backTo="/admin/buildings" backLabel={t('building.goFacilities')} />
      <TabList selectedValue="overview"><Tab value="overview">{t('common.overview')}</Tab></TabList>
      <div className={styles.tabContent}><FieldList rows={rows} /></div>
    </div>
  )
}
