import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Button, Card, CardHeader, Spinner, Subtitle2, makeStyles, tokens } from '@fluentui/react-components'
import {
  Box24Regular, CheckmarkCircle24Regular, Wrench24Regular, PlugDisconnected24Regular,
  ShieldError20Regular, Clock20Regular, ChevronRight20Regular,
} from '@fluentui/react-icons'
import { StatCard } from '@shared/ui'
import { dashboardKeys, getEquipmentDashboard } from '../api'

const WARRANTY_WINDOW = 60
const MAINTENANCE_WINDOW = 30

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  kpis: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: tokens.spacingHorizontalM },
  panels: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: tokens.spacingHorizontalM },
  panelBody: {
    display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM,
    padding: `0 ${tokens.spacingVerticalM} ${tokens.spacingVerticalM}`,
  },
  stats: { display: 'flex', columnGap: tokens.spacingHorizontalS, rowGap: tokens.spacingVerticalXS, flexWrap: 'wrap' },
  viewAll: { alignSelf: 'flex-start' },
  state: { padding: tokens.spacingVerticalL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1 },
})

export function EquipmentDashboard({
  facilityId, onOpenWarranties, onOpenMaintenance,
}: {
  facilityId: string
  onOpenWarranties: () => void
  onOpenMaintenance: () => void
}) {
  const styles = useStyles()
  const { t } = useTranslation()

  const query = useQuery({
    queryKey: dashboardKeys.all(facilityId),
    queryFn: () => getEquipmentDashboard(facilityId, WARRANTY_WINDOW, MAINTENANCE_WINDOW),
  })

  if (query.isLoading) {
    return <div className={styles.state}><Spinner size="small" label={t('equipment.loading')} /></div>
  }
  if (query.isError || !query.data) {
    return <div className={`${styles.state} ${styles.error}`}>{t('equipment.dashboard.loadError')}</div>
  }
  const d = query.data

  return (
    <div className={styles.root}>
      <div className={styles.kpis}>
        <StatCard label={t('equipment.dashboard.kpis.total')} value={d.total} icon={<Box24Regular />} />
        <StatCard label={t('equipment.dashboard.kpis.operational')} value={d.operational} icon={<CheckmarkCircle24Regular />} />
        <StatCard label={t('equipment.dashboard.kpis.underMaintenance')} value={d.underMaintenance} icon={<Wrench24Regular />} />
        <StatCard label={t('equipment.dashboard.kpis.outOfService')} value={d.outOfService} icon={<PlugDisconnected24Regular />} />
      </div>

      <div className={styles.panels}>
        <Card>
          <CardHeader
            image={<ShieldError20Regular />}
            header={<Subtitle2>{t('equipment.dashboard.warranty.title')}</Subtitle2>}
          />
          <div className={styles.panelBody}>
            <div className={styles.stats}>
              <Badge appearance="tint" color="danger" size="large">
                {t('equipment.dashboard.warranty.expiredStat', { count: d.warrantyExpired.length })}
              </Badge>
              <Badge appearance="tint" color="warning" size="large">
                {t('equipment.dashboard.warranty.expiringStat', { count: d.warrantyExpiring.length })}
              </Badge>
            </div>
            <Button className={styles.viewAll} appearance="transparent" icon={<ChevronRight20Regular />}
              iconPosition="after" onClick={onOpenWarranties}>
              {t('equipment.dashboard.warranty.viewAll')}
            </Button>
          </div>
        </Card>

        <Card>
          <CardHeader
            image={<Clock20Regular />}
            header={<Subtitle2>{t('equipment.dashboard.maintenance.title')}</Subtitle2>}
          />
          <div className={styles.panelBody}>
            <div className={styles.stats}>
              <Badge appearance="tint" color="danger" size="large">
                {t('equipment.dashboard.maintenance.overdueStat', { count: d.maintenanceOverdue.length })}
              </Badge>
              <Badge appearance="tint" color="warning" size="large">
                {t('equipment.dashboard.maintenance.upcomingStat', { count: d.maintenanceUpcoming.length })}
              </Badge>
            </div>
            <Button className={styles.viewAll} appearance="transparent" icon={<ChevronRight20Regular />}
              iconPosition="after" onClick={onOpenMaintenance}>
              {t('equipment.dashboard.maintenance.viewAll')}
            </Button>
          </div>
        </Card>
      </div>
    </div>
  )
}
