import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { Button, Spinner, makeStyles, tokens } from '@fluentui/react-components'
import { Building20Regular, ChevronRight20Regular } from '@fluentui/react-icons'
import { useThemeMode } from '@app/providers/ThemeModeProvider'
import { selectEstablishment, setToken } from '../api'
import { useCurrentUser } from '../useCurrentUser'
import logoFull from '../../../assets/logos/lafie-full.svg'
import logoFullDark from '../../../assets/logos/lafie-full-dark.svg'

const useStyles = makeStyles({
  root: { minHeight: '100vh', display: 'flex', alignItems: 'flex-start', justifyContent: 'center', backgroundColor: tokens.colorNeutralBackground1, paddingTop: '12vh' },
  card: { width: '100%', maxWidth: '420px', display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL, padding: `0 ${tokens.spacingHorizontalXL}` },
  logo: { height: '48px', width: 'auto', alignSelf: 'flex-start' },
  title: { fontSize: '22px', fontWeight: tokens.fontWeightSemibold, color: tokens.colorNeutralForeground1, margin: 0 },
  subtitle: { fontSize: tokens.fontSizeBase300, color: tokens.colorNeutralForeground3, margin: 0 },
  list: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalS },
  item: {
    display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM,
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalL}`, border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusLarge, background: tokens.colorNeutralBackground1, cursor: 'pointer', textAlign: 'left',
    '&:hover': { border: `1px solid ${tokens.colorBrandStroke1}`, background: tokens.colorNeutralBackground1Hover },
  },
  itemMain: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalM, color: tokens.colorNeutralForeground1 },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
})

export function SelectEstablishmentPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const { isDark } = useThemeMode()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { me, isLoading } = useCurrentUser()

  const mutation = useMutation({
    mutationFn: (facilityId: string) => selectEstablishment(facilityId),
    onSuccess: (res) => {
      setToken(res.token)
      queryClient.setQueryData(['me'], res.user)
      void navigate({ to: '/' })
    },
  })

  return (
    <div className={styles.root}>
      <div className={styles.card}>
        <img className={styles.logo} src={isDark ? logoFullDark : logoFull} alt="Lafie" />
        <div>
          <h1 className={styles.title}>{t('auth.chooseEstablishment')}</h1>
          <p className={styles.subtitle}>{t('auth.chooseEstablishmentHint')}</p>
        </div>

        {isLoading && <div className={styles.state}><Spinner label={t('auth.loading')} /></div>}

        {me && (
          <div className={styles.list}>
            {me.establishments.map((e) => (
              <button key={e.facilityId} type="button" className={styles.item}
                disabled={mutation.isPending}
                onClick={() => mutation.mutate(e.facilityId)}>
                <span className={styles.itemMain}>
                  <Building20Regular />
                  <span>{e.name}</span>
                </span>
                <ChevronRight20Regular />
              </button>
            ))}
          </div>
        )}

        {mutation.isError && <span className={styles.error}>{t('auth.selectError')}</span>}
      </div>
    </div>
  )
}
