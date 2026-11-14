import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DateTime } from 'luxon'
import {
  Body1,
  Caption1,
  Card,
  CardHeader,
  Spinner,
  Subtitle2,
  Title3,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import {
  Alert24Regular,
  Beaker24Regular,
  CalendarLtr24Regular,
  People24Regular,
} from '@fluentui/react-icons'
import { StatCard, StatusDot } from '@shared/ui'
import { getJson } from '@shared/api/http'

const useStyles = makeStyles({
  root: {
    padding: tokens.spacingVerticalXL,
    display: 'flex',
    flexDirection: 'column',
    rowGap: tokens.spacingVerticalL,
  },
  heading: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXXS },
  subtitle: { color: tokens.colorNeutralForeground3 },
  kpis: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  cols: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  panelBody: { display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalS, padding: tokens.spacingVerticalS },
  row: { display: 'flex', justifyContent: 'space-between', alignItems: 'center' },
})

export function DashboardPage() {
  const styles = useStyles()
  const { t } = useTranslation()

  const health = useQuery({
    queryKey: ['health'],
    queryFn: () => getJson<{ status: string; phase: string }>('/api/health'),
  })
  const db = useQuery({
    queryKey: ['health-db'],
    queryFn: () => getJson<{ database: string }>('/api/health/db'),
  })

  return (
    <main className={styles.root}>
      <div className={styles.heading}>
        <Title3>{t('dashboard.welcome')}</Title3>
        <Caption1 className={styles.subtitle}>{t('dashboard.subtitle')}</Caption1>
      </div>

      <section className={styles.kpis}>
        <StatCard label={t('dashboard.kpi.patientsToday')} value={24} icon={<People24Regular />} />
        <StatCard label={t('dashboard.kpi.appointments')} value={8} icon={<CalendarLtr24Regular />} />
        <StatCard label={t('dashboard.kpi.pendingResults')} value={3} icon={<Beaker24Regular />} />
        <StatCard label={t('dashboard.kpi.alerts')} value={1} icon={<Alert24Regular />} />
      </section>

      <section className={styles.cols}>
        <Card>
          <CardHeader header={<Subtitle2>{t('dashboard.today')}</Subtitle2>} />
          <div className={styles.panelBody}>
            <Body1 className={styles.subtitle}>{t('dashboard.empty')}</Body1>
          </div>
        </Card>

        <Card>
          <CardHeader header={<Subtitle2>{t('dashboard.system')}</Subtitle2>} />
          <div className={styles.panelBody}>
            <div className={styles.row}>
              <Body1>{t('dashboard.api')}</Body1>
              {health.isPending ? (
                <Spinner size="tiny" />
              ) : (
                <StatusDot
                  ok={!health.isError}
                  label={
                    health.data
                      ? `${t('dashboard.reachable')} (${health.data.phase})`
                      : t('dashboard.unreachable')
                  }
                />
              )}
            </div>
            <div className={styles.row}>
              <Body1>{t('dashboard.database')}</Body1>
              {db.isPending ? (
                <Spinner size="tiny" />
              ) : (
                <StatusDot ok={!db.isError} label={db.data ? db.data.database : t('dashboard.unreachable')} />
              )}
            </div>
            <Caption1 className={styles.subtitle}>
              {t('dashboard.updatedAt', { time: DateTime.now().toLocaleString(DateTime.TIME_WITH_SECONDS) })}
            </Caption1>
          </div>
        </Card>
      </section>
    </main>
  )
}
