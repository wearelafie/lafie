import type { ReactElement } from 'react'
import { createRootRoute, createRoute, createRouter } from '@tanstack/react-router'
import { AppShell } from '@app/layout/AppShell'
import { DashboardPage } from '@features/dashboard'
import { OrganizationsPage } from '@features/organization'
import { PlaceholderPage } from '@shared/components'
import { flatRoutes } from '@app/nav/navModel'

// Routes déjà implémentées (vraie feature) — les autres restent des pages stub.
const implemented: Record<string, () => ReactElement> = {
  '/admin': OrganizationsPage,
}

// Routage code-based. Les routes modules sont générées depuis le modèle de nav
// (pages stub pour l'instant) ; les remplacer par les vraies features au fur et à mesure.
const rootRoute = createRootRoute({ component: AppShell })

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: DashboardPage,
})

// Toutes les routes sauf '/' (déjà le dashboard) => page provisoire.
const moduleRoutes = flatRoutes
  .filter((r) => r.to !== '/')
  .map((r) => {
    const Impl = implemented[r.to]
    return createRoute({
      getParentRoute: () => rootRoute,
      path: r.to,
      component: Impl ?? (() => <PlaceholderPage titleKey={r.labelKey} />),
    })
  })

const routeTree = rootRoute.addChildren([indexRoute, ...moduleRoutes])

export const router = createRouter({ routeTree })

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
