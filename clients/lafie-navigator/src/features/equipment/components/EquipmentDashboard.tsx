import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Card, CardHeader, Spinner, Subtitle2, Text, makeStyles, tokens } from '@fluentui/react-components'
import {
  Box24Regular, CheckmarkCircle24Regular, Wrench24Regular, PlugDisconnected24Regular,
  ShieldError20Regular, Clock20Regular,
} from '@fluentui/react-icons'
import { StatCard } from '@shared/ui'
import {
  dashboardKeys, getEquipmentDashboard,
  type MaintenanceAlert, type WarrantyAlert,
} from '../api'

const WARRANTY_WINDOW = 60
const MAINTENANCE_WINDOW = 30

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  kpis: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: tokens.spacingHorizontalM },
  panels: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: tokens.spacingHorizontalM },
  panelBody: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, padding: tokens.spacingVerticalM },
  group: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXS },
  groupTitle: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200, fontWeight: tokens.fontWeightSemibold },
  row: {
    display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`, borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    ':hover': { backgroundColor: tokens.colorNeutralBackground1Hover },
  },
  rowMain: { display: 'flex', flexDirection: 'column', minWidth: 0 },
  rowName: { fontWeight: tokens.fontWeightSemibold, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
  rowSub: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
  empty: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200, padding: tokens.spacingVerticalS },
  state: { padding: tokens.spacingVerticalL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1 },
})

export function EquipmentDashboard({ facilityId, onOpenEquipment }: { facilityId: string; onOpenEquipment: (id: string) => void }) {
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

  const warrantyRow = (a: WarrantyAlert, overdue: boolean) => (
    <div key={a.equipmentId} className={styles.row} role="button" tabIndex={0}
      onClick={() => onOpenEquipment(a.equipmentId)}
      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') onOpenEquipment(a.equipmentId) }}>
      <div className={styles.rowMain}>
        <Text className={styles.rowName}>{a.name}</Text>
        <Text className={styles.rowSub}>{a.inventoryNumber} · {a.warrantyEnd}</Text>
      </div>
      <Badge appearance="tint" color={overdue ? 'danger' : 'warning'}>
        {overdue
          ? t('equipment.dashboard.warranty.expiredBadge', { days: Math.abs(a.daysRemaining) })
          : t('equipment.dashboard.warranty.remainingBadge', { days: a.daysRemaining })}
      </Badge>
    </div>
  )

  const maintenanceRow = (a: MaintenanceAlert, overdue: boolean) => (
    <div key={a.maintenanceId} className={styles.row} role="button" tabIndex={0}
      onClick={() => onOpenEquipment(a.equipmentId)}
      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') onOpenEquipment(a.equipmentId) }}>
      <div className={styles.rowMain}>
        <Text className={styles.rowName}>{a.equipmentName}</Text>
        <Text className={styles.rowSub}>
          {a.inventoryNumber} · {t(`equipment.maintenance.types.${a.type}`)} · {a.scheduledOn}
        </Text>
      </div>
      <Badge appearance="tint" color={overdue ? 'danger' : 'warning'}>
        {overdue
          ? t('equipment.dashboard.maintenance.overdueBadge', { days: Math.abs(a.daysRemaining) })
          : a.daysRemaining === 0
            ? t('equipment.dashboard.maintenance.dueToday')
            : t('equipment.dashboard.maintenance.upcomingBadge', { days: a.daysRemaining })}
      </Badge>
    </div>
  )

  const warrantyEmpty = d.warrantyExpired.length === 0 && d.warrantyExpiring.length === 0
  const maintenanceEmpty = d.maintenanceOverdue.length === 0 && d.maintenanceUpcoming.length === 0

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
            {warrantyEmpty && <div className={styles.empty}>{t('equipment.dashboard.warranty.none')}</div>}
            {d.warrantyExpired.length > 0 && (
              <div className={styles.group}>
                <span className={styles.groupTitle}>{t('equipment.dashboard.warranty.expired')} ({d.warrantyExpired.length})</span>
                {d.warrantyExpired.map((a) => warrantyRow(a, true))}
              </div>
            )}
            {d.warrantyExpiring.length > 0 && (
              <div className={styles.group}>
                <span className={styles.groupTitle}>{t('equipment.dashboard.warranty.expiring', { days: WARRANTY_WINDOW })} ({d.warrantyExpiring.length})</span>
                {d.warrantyExpiring.map((a) => warrantyRow(a, false))}
              </div>
            )}
          </div>
        </Card>

        <Card>
          <CardHeader
            image={<Clock20Regular />}
            header={<Subtitle2>{t('equipment.dashboard.maintenance.title')}</Subtitle2>}
          />
          <div className={styles.panelBody}>
            {maintenanceEmpty && <div className={styles.empty}>{t('equipment.dashboard.maintenance.none')}</div>}
            {d.maintenanceOverdue.length > 0 && (
              <div className={styles.group}>
                <span className={styles.groupTitle}>{t('equipment.dashboard.maintenance.overdue')} ({d.maintenanceOverdue.length})</span>
                {d.maintenanceOverdue.map((a) => maintenanceRow(a, true))}
              </div>
            )}
            {d.maintenanceUpcoming.length > 0 && (
              <div className={styles.group}>
                <span className={styles.groupTitle}>{t('equipment.dashboard.maintenance.upcoming', { days: MAINTENANCE_WINDOW })} ({d.maintenanceUpcoming.length})</span>
                {d.maintenanceUpcoming.map((a) => maintenanceRow(a, false))}
              </div>
            )}
          </div>
        </Card>
      </div>
    </div>
  )
}
