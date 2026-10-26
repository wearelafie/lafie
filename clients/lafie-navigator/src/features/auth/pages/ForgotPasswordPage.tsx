import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import { makeStyles, tokens } from '@fluentui/react-components'
import { ArrowLeft20Regular } from '@fluentui/react-icons'
import { useThemeMode } from '@app/providers/ThemeModeProvider'
import logoFull from '../../../assets/logos/lafie-full.svg'
import logoFullDark from '../../../assets/logos/lafie-full-dark.svg'

const useStyles = makeStyles({
  root: { display: 'flex', minHeight: '100vh', backgroundColor: tokens.colorNeutralBackground1 },
  form: {
    width: '25%', minWidth: '360px', display: 'flex', flexDirection: 'column', justifyContent: 'flex-start',
    padding: `0 ${tokens.spacingHorizontalXXXL}`, paddingTop: '9vh',
    '@media (max-width: 768px)': { width: '100%', minWidth: 0, padding: tokens.spacingHorizontalXL, paddingTop: '8vh' },
  },
  inner: { width: '100%', maxWidth: '340px', margin: '0 auto' },
  logo: { height: '52px', width: 'auto', display: 'block', marginBottom: tokens.spacingVerticalXXXL },
  title: { fontSize: '24px', fontWeight: tokens.fontWeightSemibold, color: tokens.colorNeutralForeground1, margin: `0 0 ${tokens.spacingVerticalM}` },
  text: { color: tokens.colorNeutralForeground2, fontSize: tokens.fontSizeBase300, lineHeight: tokens.lineHeightBase300, margin: `0 0 ${tokens.spacingVerticalXL}` },
  back: { display: 'inline-flex', alignItems: 'center', columnGap: '6px', color: tokens.colorBrandForeground1, textDecoration: 'none', fontSize: tokens.fontSizeBase300 },
  brand: { flex: 1, backgroundColor: tokens.colorBrandBackground, '@media (max-width: 768px)': { display: 'none' } },
})

export function ForgotPasswordPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const { isDark } = useThemeMode()
  return (
    <div className={styles.root}>
      <div className={styles.form}>
        <div className={styles.inner}>
          <img className={styles.logo} src={isDark ? logoFullDark : logoFull} alt="Lafie" />
          <h1 className={styles.title}>{t('auth.forgotTitle')}</h1>
          <p className={styles.text}>{t('auth.forgotSoon')}</p>
          <Link to={'/login' as never} className={styles.back}><ArrowLeft20Regular />{t('auth.backToLogin')}</Link>
        </div>
      </div>
      <div className={styles.brand} />
    </div>
  )
}
