import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import { makeStyles, tokens } from '@fluentui/react-components'
import { ArrowLeft20Regular } from '@fluentui/react-icons'

const useStyles = makeStyles({
  root: { display: 'flex', minHeight: '100vh', backgroundColor: tokens.colorNeutralBackground1 },
  form: {
    width: '25%', minWidth: '360px', display: 'flex', flexDirection: 'column', justifyContent: 'center',
    padding: `${tokens.spacingVerticalXXL} ${tokens.spacingHorizontalXXXL}`,
    '@media (max-width: 768px)': { width: '100%', minWidth: 0, padding: tokens.spacingHorizontalXL },
  },
  inner: { width: '100%', maxWidth: '340px', margin: '0 auto' },
  logo: { display: 'flex', alignItems: 'center', columnGap: '9px', marginBottom: tokens.spacingVerticalXXXL },
  wordmark: { fontSize: '23px', fontWeight: tokens.fontWeightSemibold, color: tokens.colorBrandForeground1 },
  title: { fontSize: '24px', fontWeight: tokens.fontWeightSemibold, color: tokens.colorNeutralForeground1, margin: `0 0 ${tokens.spacingVerticalM}` },
  text: { color: tokens.colorNeutralForeground2, fontSize: tokens.fontSizeBase300, lineHeight: tokens.lineHeightBase300, margin: `0 0 ${tokens.spacingVerticalXL}` },
  back: { display: 'inline-flex', alignItems: 'center', columnGap: '6px', color: tokens.colorBrandForeground1, textDecoration: 'none', fontSize: tokens.fontSizeBase300 },
  brand: { flex: 1, backgroundColor: tokens.colorBrandBackground, '@media (max-width: 768px)': { display: 'none' } },
})

export function ForgotPasswordPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  return (
    <div className={styles.root}>
      <div className={styles.form}>
        <div className={styles.inner}>
          <div className={styles.logo}>
            <svg width="26" height="26" viewBox="0 0 26 26" aria-hidden="true">
              <rect x="10.5" y="2" width="5" height="22" rx="2.5" fill="#00728b" />
              <rect x="2" y="10.5" width="22" height="5" rx="2.5" fill="#009664" />
            </svg>
            <span className={styles.wordmark}>lafie</span>
          </div>
          <h1 className={styles.title}>{t('auth.forgotTitle')}</h1>
          <p className={styles.text}>{t('auth.forgotSoon')}</p>
          <Link to={'/login' as never} className={styles.back}><ArrowLeft20Regular />{t('auth.backToLogin')}</Link>
        </div>
      </div>
      <div className={styles.brand} />
    </div>
  )
}
