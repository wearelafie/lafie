import type { ReactElement } from 'react'
import { createRootRoute, createRoute, createRouter } from '@tanstack/react-router'
import { AppShell } from '@app/layout/AppShell'
import { DashboardPage } from '@features/dashboard'
import { OrganizationsPage, FacilityDetailPage, OrganizationFormPage } from '@features/organization'
import { BuildingDetailPage } from '@features/building'
import { FloorDetailPage } from '@features/floor'
import { SpaceDetailPage } from '@features/space'
import { ServiceDetailPage } from '@features/service'
import { CareUnitDetailPage } from '@features/care-unit'
import { LoginPage, SelectEstablishmentPage } from '@features/auth'
import { ForgotPasswordPage } from '@features/auth/pages/ForgotPasswordPage'
import { ReferenceValuesPage, GeoDivisionsPage } from '@features/admin'
import { EquipmentPage, EquipmentDetailPage, EquipmentWarrantiesPage, EquipmentMaintenancePage, EquipmentFormPage } from '@features/equipment'
import { MaintenancePage, MaintenanceTicketDetailPage } from '@features/maintenance'
import { HygienePage, CleaningTaskDetailPage } from '@features/hygiene'
import { SterilizationPage, InstrumentSetDetailPage, SterilizationCycleDetailPage } from '@features/sterilization'
import { PlaceholderPage } from '@shared/components'
import { flatRoutes } from '@app/nav/navModel'

// Routes déjà implémentées (vraie feature) — les autres restent des pages stub.
// Bâtiments/étages/espaces ne sont plus des routes à plat : ils vivent dans les
// onglets des fiches détail (voir extraRoutes), scopés à leur établissement parent.
const implemented: Record<string, () => ReactElement> = {
  '/admin': OrganizationsPage,
  '/admin/equipment': EquipmentPage,
  '/admin/maintenance': MaintenancePage,
  '/admin/hygiene': HygienePage,
  '/admin/sterilization': SterilizationPage,
  '/admin/terminology': ReferenceValuesPage,
  '/admin/geography': GeoDivisionsPage,
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

// Routes additionnelles (hors navModel) : écrans de détail/sous-ressources.
const extraRoutes = [
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/facility', component: FacilityDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/facility-form', component: OrganizationFormPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/building', component: BuildingDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/floor', component: FloorDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/space', component: SpaceDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/service', component: ServiceDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/care-unit', component: CareUnitDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/equipment-item', component: EquipmentDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/equipment-warranties', component: EquipmentWarrantiesPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/equipment-maintenance', component: EquipmentMaintenancePage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/equipment-form', component: EquipmentFormPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/maintenance-ticket', component: MaintenanceTicketDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/cleaning-task', component: CleaningTaskDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/instrument-set', component: InstrumentSetDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/admin/sterilization-cycle', component: SterilizationCycleDetailPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/login', component: LoginPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/forgot-password', component: ForgotPasswordPage }),
  createRoute({ getParentRoute: () => rootRoute, path: '/select-establishment', component: SelectEstablishmentPage }),
]

const routeTree = rootRoute.addChildren([indexRoute, ...moduleRoutes, ...extraRoutes])

export const router = createRouter({ routeTree })

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
