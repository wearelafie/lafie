import { useEffect, useState } from 'react'
import { Outlet, useNavigate, useRouterState } from '@tanstack/react-router'
import { DrawerBody, OverlayDrawer, makeStyles } from '@fluentui/react-components'
import { useIsMobile } from '@shared/hooks'
import { getToken } from '@features/auth'
import { TopBar } from './TopBar'
import { CommandBar } from './CommandBar'
import { SideNav } from './SideNav'

const useStyles = makeStyles({
  shell: {
    display: 'grid',
    gridTemplateRows: 'auto auto 1fr',
    height: '100vh',
  },
  body: { display: 'grid', minHeight: 0 },
  content: { overflowY: 'auto', minWidth: 0 },
  drawerBody: { padding: 0 },
})

// Coquille applicative fidèle au shell Outlook + responsive :
// - desktop : navigation en colonne (rail 48px ↔ drawer 240px)
// - mobile  : navigation en overlay drawer (off-canvas), contenu pleine largeur
export function AppShell() {
  const styles = useStyles()
  const isMobile = useIsMobile()
  const [expanded, setExpanded] = useState(true)
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const pathname = useRouterState({ select: (s) => s.location.pathname })
  const navigate = useNavigate()

  // Écrans d'auth publics (sans jeton) et écran de sélection d'établissement (authentifié) : plein écran.
  const isPublicAuth = pathname === '/login' || pathname === '/forgot-password'
  const isSelect = pathname === '/select-establishment'
  const hasToken = !!getToken()

  // Garde d'accès : sans jeton, rediriger vers la connexion (hors écrans publics).
  useEffect(() => {
    if (!isPublicAuth && !hasToken) void navigate({ to: '/login' as never })
  }, [isPublicAuth, hasToken, navigate])

  if (isPublicAuth) return <Outlet />
  if (!hasToken) return null
  if (isSelect) return <Outlet />

  const onMenuClick = () => (isMobile ? setMobileNavOpen((o) => !o) : setExpanded((e) => !e))

  return (
    <div className={styles.shell}>
      <TopBar onMenuClick={onMenuClick} />
      <CommandBar />

      {isMobile ? (
        <div className={styles.content}>
          <Outlet />
          <OverlayDrawer
            open={mobileNavOpen}
            onOpenChange={(_, d) => setMobileNavOpen(d.open)}
            position="start"
            size="small"
          >
            <DrawerBody className={styles.drawerBody}>
              <SideNav expanded onItemClick={() => setMobileNavOpen(false)} />
            </DrawerBody>
          </OverlayDrawer>
        </div>
      ) : (
        <div
          className={styles.body}
          style={{ gridTemplateColumns: expanded ? '240px 1fr' : '48px 1fr' }}
        >
          <SideNav expanded={expanded} />
          <div className={styles.content}>
            <Outlet />
          </div>
        </div>
      )}
    </div>
  )
}
