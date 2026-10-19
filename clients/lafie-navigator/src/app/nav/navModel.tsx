import type { ReactElement } from 'react'
// Icônes colorées (fluent-color) pour les modules parents — déjà offline/tree-shakées.
import HomeIcon from '~icons/fluent-color/home-24'
import PeopleIcon from '~icons/fluent-color/people-24'
import CalendarIcon from '~icons/fluent-color/calendar-24'
import ClinicalIcon from '~icons/fluent-color/person-heart-24'
import PharmacyIcon from '~icons/fluent-color/molecule-24'
import LabIcon from '~icons/fluent-color/data-scatter-24'
import BillingIcon from '~icons/fluent-color/receipt-24'
import AdminIcon from '~icons/fluent-color/settings-24'
// Icônes mono (fines) pour les sous-items.
import {
  List20Regular,
  PersonAdd20Regular,
  Bed20Regular,
  BranchFork20Regular,
  CalendarLtr20Regular,
  CalendarClock20Regular,
  ConferenceRoom20Regular,
  Stethoscope20Regular,
  ClipboardPulse20Regular,
  Prohibited20Regular,
  History20Regular,
  Document20Regular,
  Pill20Regular,
  Box20Regular,
  Cart20Regular,
  Warning20Regular,
  DocumentBulletList20Regular,
  Beaker20Regular,
  Drop20Regular,
  Money20Regular,
  ShieldTask20Regular,
  Tag20Regular,
  BuildingMultiple20Regular,
  PeopleTeam20Regular,
  BookGlobe20Regular,
  ArrowSync20Regular,
  Settings20Regular,
} from '@fluentui/react-icons'

export type NavSubItem = {
  key: string
  to: string
  labelKey: string
  icon: ReactElement
}

export type NavItem = {
  key: string
  to: string
  labelKey: string
  icon: ReactElement
  children?: NavSubItem[]
}

// Source de vérité unique de la navigation : consommée par la sidebar ET le lanceur.
export const navItems: NavItem[] = [
  { key: 'home', to: '/', labelKey: 'nav.home', icon: <HomeIcon /> },
  {
    key: 'patients',
    to: '/patients',
    labelKey: 'nav.patients',
    icon: <PeopleIcon />,
    children: [
      { key: 'list', to: '/patients', labelKey: 'nav.sub.patients.list', icon: <List20Regular /> },
      { key: 'new', to: '/patients/new', labelKey: 'nav.sub.patients.new', icon: <PersonAdd20Regular /> },
      { key: 'admissions', to: '/patients/admissions', labelKey: 'nav.sub.patients.admissions', icon: <Bed20Regular /> },
      { key: 'merge', to: '/patients/merge', labelKey: 'nav.sub.patients.merge', icon: <BranchFork20Regular /> },
    ],
  },
  {
    key: 'agenda',
    to: '/agenda',
    labelKey: 'nav.agenda',
    icon: <CalendarIcon />,
    children: [
      { key: 'planning', to: '/agenda', labelKey: 'nav.sub.agenda.planning', icon: <CalendarLtr20Regular /> },
      { key: 'appointments', to: '/agenda/appointments', labelKey: 'nav.sub.agenda.appointments', icon: <CalendarClock20Regular /> },
      { key: 'rooms', to: '/agenda/rooms', labelKey: 'nav.sub.agenda.rooms', icon: <ConferenceRoom20Regular /> },
    ],
  },
  {
    key: 'clinical',
    to: '/clinical',
    labelKey: 'nav.clinical',
    icon: <ClinicalIcon />,
    children: [
      { key: 'consultations', to: '/clinical', labelKey: 'nav.sub.clinical.consultations', icon: <Stethoscope20Regular /> },
      { key: 'observations', to: '/clinical/observations', labelKey: 'nav.sub.clinical.observations', icon: <ClipboardPulse20Regular /> },
      { key: 'prescriptions', to: '/clinical/prescriptions', labelKey: 'nav.sub.clinical.prescriptions', icon: <Prohibited20Regular /> },
      { key: 'history', to: '/clinical/history', labelKey: 'nav.sub.clinical.history', icon: <History20Regular /> },
      { key: 'documents', to: '/clinical/documents', labelKey: 'nav.sub.clinical.documents', icon: <Document20Regular /> },
    ],
  },
  {
    key: 'pharmacy',
    to: '/pharmacy',
    labelKey: 'nav.pharmacy',
    icon: <PharmacyIcon />,
    children: [
      { key: 'dispensing', to: '/pharmacy', labelKey: 'nav.sub.pharmacy.dispensing', icon: <Pill20Regular /> },
      { key: 'stock', to: '/pharmacy/stock', labelKey: 'nav.sub.pharmacy.stock', icon: <Box20Regular /> },
      { key: 'orders', to: '/pharmacy/orders', labelKey: 'nav.sub.pharmacy.orders', icon: <Cart20Regular /> },
      { key: 'interactions', to: '/pharmacy/interactions', labelKey: 'nav.sub.pharmacy.interactions', icon: <Warning20Regular /> },
    ],
  },
  {
    key: 'lab',
    to: '/lab',
    labelKey: 'nav.lab',
    icon: <LabIcon />,
    children: [
      { key: 'requests', to: '/lab', labelKey: 'nav.sub.lab.requests', icon: <DocumentBulletList20Regular /> },
      { key: 'results', to: '/lab/results', labelKey: 'nav.sub.lab.results', icon: <Beaker20Regular /> },
      { key: 'samples', to: '/lab/samples', labelKey: 'nav.sub.lab.samples', icon: <Drop20Regular /> },
    ],
  },
  {
    key: 'billing',
    to: '/billing',
    labelKey: 'nav.billing',
    icon: <BillingIcon />,
    children: [
      { key: 'invoices', to: '/billing', labelKey: 'nav.sub.billing.invoices', icon: <DocumentBulletList20Regular /> },
      { key: 'payments', to: '/billing/payments', labelKey: 'nav.sub.billing.payments', icon: <Money20Regular /> },
      { key: 'insurance', to: '/billing/insurance', labelKey: 'nav.sub.billing.insurance', icon: <ShieldTask20Regular /> },
      { key: 'pricing', to: '/billing/pricing', labelKey: 'nav.sub.billing.pricing', icon: <Tag20Regular /> },
    ],
  },
  {
    key: 'admin',
    to: '/admin',
    labelKey: 'nav.admin',
    icon: <AdminIcon />,
    children: [
      { key: 'facilities', to: '/admin', labelKey: 'nav.sub.admin.facilities', icon: <BuildingMultiple20Regular /> },
      { key: 'users', to: '/admin/users', labelKey: 'nav.sub.admin.users', icon: <PeopleTeam20Regular /> },
      { key: 'terminology', to: '/admin/terminology', labelKey: 'nav.sub.admin.terminology', icon: <BookGlobe20Regular /> },
      { key: 'interop', to: '/admin/interop', labelKey: 'nav.sub.admin.interop', icon: <ArrowSync20Regular /> },
      { key: 'audit', to: '/admin/audit', labelKey: 'nav.sub.admin.audit', icon: <History20Regular /> },
      { key: 'settings', to: '/admin/settings', labelKey: 'nav.sub.admin.settings', icon: <Settings20Regular /> },
    ],
  },
]

// Grand module actif selon la route courante (sidebar dynamique).
// Correspondance la plus spécifique ; '/' => Accueil.
export function findActiveModule(pathname: string): NavItem {
  const matches = navItems.filter((it) =>
    it.to === '/' ? pathname === '/' : pathname === it.to || pathname.startsWith(`${it.to}/`),
  )
  const nonRoot = matches.filter((m) => m.to !== '/').sort((a, b) => b.to.length - a.to.length)
  return nonRoot[0] ?? navItems[0]
}

// Liste à plat de toutes les routes (parents + enfants), pour générer le routeur.
export type FlatRoute = { to: string; labelKey: string }

export const flatRoutes: FlatRoute[] = navItems.flatMap((item) => {
  const self: FlatRoute = { to: item.to, labelKey: item.labelKey }
  const kids = (item.children ?? [])
    .filter((c) => c.to !== item.to) // évite de dupliquer la route parent (ex. /patients)
    .map((c) => ({ to: c.to, labelKey: c.labelKey }))
  return [self, ...kids]
})
