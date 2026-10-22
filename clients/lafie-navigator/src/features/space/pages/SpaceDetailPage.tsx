import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Tab, TabList, makeStyles, tokens } from '@fluentui/react-components'
import { useAppStore } from '@app/store/appStore'
import { DetailHeader, FieldList, type FieldRow } from '@shared/components'
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
  const b = (v: boolean) => (v ? t('common.yes') : t('common.no'))
  const q = useQuery({ queryKey: ['space', id], queryFn: () => getSpace(id!), enabled: !!id })

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
  return (
    <div className={styles.root}>
      <DetailHeader title={s.name} backTo="/admin/spaces" backLabel={t('space.goFloors')} />
      <TabList selectedValue="overview"><Tab value="overview">{t('common.overview')}</Tab></TabList>
      <div className={styles.tabContent}><FieldList rows={rows} /></div>
    </div>
  )
}
