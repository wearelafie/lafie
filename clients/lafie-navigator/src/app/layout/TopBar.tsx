import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Avatar,
  Button,
  CounterBadge,
  SearchBox,
  Tooltip,
  makeStyles,
  mergeClasses,
  tokens,
} from '@fluentui/react-components'
import {
  Alert24Regular,
  Apps24Regular,
  Navigation24Regular,
  Settings24Regular,
} from '@fluentui/react-icons'
import { SettingsDialog } from '@features/settings'
import { AppLauncherDialog } from './AppLauncherDialog'

const useStyles = makeStyles({
  root: {
    display: 'flex',
    alignItems: 'center',
    columnGap: tokens.spacingHorizontalS,
    height: '48px',
    padding: `0 ${tokens.spacingHorizontalM}`,
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  logoBtn: {
    padding: 0,
    minWidth: 0,
    border: 'none',
    background: 'transparent',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    borderRadius: tokens.borderRadiusMedium,
    ':hover': { opacity: 0.85 },
  },
  logo: { height: '28px', width: 'auto', display: 'block' },
  searchWrap: { flex: 1, display: 'flex', justifyContent: 'center' },
  search: { width: '100%', maxWidth: '480px' },
  right: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalXS },
  onBrand: { color: tokens.colorNeutralForegroundOnBrand },
  bell: { position: 'relative' },
  badge: { position: 'absolute', top: '4px', right: '4px' },
  hideOnMobile: { '@media (max-width: 768px)': { display: 'none' } },
})

export function TopBar({ onMenuClick }: { onMenuClick: () => void }) {
  const styles = useStyles()
  const { t, i18n } = useTranslation()
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [launcherOpen, setLauncherOpen] = useState(false)
  const toggleLang = () => i18n.changeLanguage(i18n.language.startsWith('fr') ? 'en' : 'fr')

  return (
    <header className={styles.root}>
      <Button
        appearance="transparent"
        className={styles.onBrand}
        icon={<Navigation24Regular />}
        onClick={onMenuClick}
        aria-label="Menu"
      />
      <Button
        appearance="transparent"
        className={mergeClasses(styles.onBrand, styles.hideOnMobile)}
        icon={<Apps24Regular />}
        aria-label={t('launcher.open')}
        onClick={() => setLauncherOpen(true)}
      />
      <button
        type="button"
        className={styles.logoBtn}
        onClick={() => setLauncherOpen(true)}
        aria-label={t('launcher.open')}
      >
        <img src="/logo/lafie-white.svg" alt="Lafie" className={styles.logo} />
      </button>

      <div className={styles.searchWrap}>
        <SearchBox className={styles.search} placeholder={t('search')} />
      </div>

      <div className={styles.right}>
        <Button
          appearance="transparent"
          className={mergeClasses(styles.onBrand, styles.hideOnMobile)}
          onClick={toggleLang}
        >
          {t('switchLang')}
        </Button>
        <Tooltip content="Notifications" relationship="label">
          <span className={styles.bell}>
            <Button
              appearance="transparent"
              className={styles.onBrand}
              icon={<Alert24Regular />}
              aria-label="Notifications"
            />
            <CounterBadge className={styles.badge} count={3} size="small" color="danger" />
          </span>
        </Tooltip>
        <Tooltip content="Réglages" relationship="label">
          <Button
            appearance="transparent"
            className={styles.onBrand}
            icon={<Settings24Regular />}
            aria-label="Réglages"
            onClick={() => setSettingsOpen(true)}
          />
        </Tooltip>
        <Avatar name="Horacio sKrp" size={28} color="colorful" />
      </div>

      <SettingsDialog open={settingsOpen} onOpenChange={setSettingsOpen} />
      <AppLauncherDialog open={launcherOpen} onOpenChange={setLauncherOpen} />
    </header>
  )
}
