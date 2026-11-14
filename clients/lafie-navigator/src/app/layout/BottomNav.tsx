import { useState, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useRouterState } from '@tanstack/react-router'
import { makeStyles, mergeClasses, tokens } from '@fluentui/react-components'
import {
  Home24Regular,
  Home24Filled,
  People24Regular,
  People24Filled,
  CalendarLtr24Regular,
  CalendarLtr24Filled,
  Stethoscope24Regular,
  Stethoscope24Filled,
  GridDots24Regular,
  GridDots24Filled,
} from '@fluentui/react-icons'
import { findActiveModule } from '@app/nav/navModel'
import { AppLauncherDialog } from './AppLauncherDialog'

const useStyles = makeStyles({
  root: {
    display: 'flex',
    alignItems: 'stretch',
    justifyContent: 'space-around',
    backgroundColor: tokens.colorNeutralBackground1,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    // Respecte la zone sûre iOS (encoche / barre home).
    paddingBottom: 'env(safe-area-inset-bottom, 0px)',
  },
  tab: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    rowGap: '2px',
    minWidth: 0,
    height: '56px',
    textDecoration: 'none',
    color: tokens.colorNeutralForeground3,
    cursor: 'pointer',
    border: 'none',
    background: 'transparent',
    padding: 0,
    ':hover': { color: tokens.colorNeutralForeground2 },
  },
  tabActive: { color: tokens.colorBrandForeground1 },
  icon: { display: 'flex', '& svg': { width: '24px', height: '24px' } },
  label: {
    fontSize: '11px',
    lineHeight: '12px',
    maxWidth: '100%',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
})

type Tab = {
  key: string
  to: string
  labelKey: string
  icon: ReactElement
  iconActive: ReactElement
}

// Onglets principaux (les plus utilisés en première ligne). Les autres modules
// restent accessibles via « Plus » (drawer complet).
const TABS: Tab[] = [
  { key: 'home', to: '/', labelKey: 'nav.home', icon: <Home24Regular />, iconActive: <Home24Filled /> },
  { key: 'patients', to: '/patients', labelKey: 'nav.patients', icon: <People24Regular />, iconActive: <People24Filled /> },
  { key: 'agenda', to: '/agenda', labelKey: 'nav.agenda', icon: <CalendarLtr24Regular />, iconActive: <CalendarLtr24Filled /> },
  { key: 'clinical', to: '/clinical', labelKey: 'nav.clinical', icon: <Stethoscope24Regular />, iconActive: <Stethoscope24Filled /> },
]

const MAIN_ROUTES = new Set(TABS.map((tTab) => tTab.to))

/**
 * Barre d'onglets fixe en bas, propre au mobile (feel « app native »).
 * 4 sections clés + « Plus » qui ouvre le lanceur (grille de tous les modules).
 */
export function BottomNav() {
  const styles = useStyles()
  const { t } = useTranslation()
  const pathname = useRouterState({ select: (s) => s.location.pathname })
  const activeModuleTo = findActiveModule(pathname).to
  const [launcherOpen, setLauncherOpen] = useState(false)
  // « Plus » est actif quand le module courant n'est pas un onglet principal.
  const moreActive = !MAIN_ROUTES.has(activeModuleTo)

  return (
    <>
      <nav className={styles.root} aria-label={t('nav.primary', 'Navigation principale')}>
        {TABS.map((tab) => {
          const active = activeModuleTo === tab.to
          return (
            <Link
              key={tab.key}
              to={tab.to}
              className={mergeClasses(styles.tab, active && styles.tabActive)}
              aria-current={active ? 'page' : undefined}
            >
              <span className={styles.icon}>{active ? tab.iconActive : tab.icon}</span>
              <span className={styles.label}>{t(tab.labelKey)}</span>
            </Link>
          )
        })}
        <button
          type="button"
          className={mergeClasses(styles.tab, moreActive && styles.tabActive)}
          onClick={() => setLauncherOpen(true)}
          aria-current={moreActive ? 'page' : undefined}
        >
          <span className={styles.icon}>{moreActive ? <GridDots24Filled /> : <GridDots24Regular />}</span>
          <span className={styles.label}>{t('actions.more')}</span>
        </button>
      </nav>
      <AppLauncherDialog open={launcherOpen} onOpenChange={setLauncherOpen} />
    </>
  )
}
