import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Tab, TabList, makeStyles, tokens } from '@fluentui/react-components'
import { useAppStore } from '@app/store/appStore'
import { DetailHeader, FieldList, type FieldRow } from '@shared/components'
import { getFloor } from '../api'

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
  const q = useQuery({ queryKey: ['floor', id], queryFn: () => getFloor(id!), enabled: !!id })

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
  return (
    <div className={styles.root}>
      <DetailHeader title={f.name} backTo="/admin/floors" backLabel={t('floor.goBuildings')} />
      <TabList selectedValue="overview"><Tab value="overview">{t('common.overview')}</Tab></TabList>
      <div className={styles.tabContent}><FieldList rows={rows} /></div>
    </div>
  )
}
