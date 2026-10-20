import { useTranslation } from 'react-i18next'
import { Body1, Title2, makeStyles, tokens } from '@fluentui/react-components'
import { Wrench24Regular } from '@fluentui/react-icons'

const useStyles = makeStyles({
  root: {
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    rowGap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
  },
  icon: { fontSize: '40px', color: tokens.colorBrandForeground1 },
})

// Page provisoire affichée pour les modules pas encore implémentés.
export function PlaceholderPage({ titleKey }: { titleKey: string }) {
  const styles = useStyles()
  const { t } = useTranslation()
  return (
    <div className={styles.root}>
      <Wrench24Regular className={styles.icon} />
      <Title2>{t(titleKey)}</Title2>
      <Body1>{t('common.comingSoon')}</Body1>
    </div>
  )
}
