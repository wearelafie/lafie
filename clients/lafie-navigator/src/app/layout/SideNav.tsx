import { useState, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { Button, Tooltip, makeStyles, mergeClasses, tokens } from '@fluentui/react-components'
// Icônes colorées Fluent (fluent-color) via unplugin-icons — offline, tree-shakées.
import HomeIcon from '~icons/fluent-color/home-24'
import PeopleIcon from '~icons/fluent-color/people-24'
import CalendarIcon from '~icons/fluent-color/calendar-24'
import ClinicalIcon from '~icons/fluent-color/person-heart-24'
import PharmacyIcon from '~icons/fluent-color/molecule-24'
import LabIcon from '~icons/fluent-color/data-scatter-24'
import BillingIcon from '~icons/fluent-color/receipt-24'
import AdminIcon from '~icons/fluent-color/settings-24'

type NavItem = { key: string; icon: ReactElement; labelKey: string }

const items: NavItem[] = [
  { key: 'home', icon: <HomeIcon />, labelKey: 'nav.home' },
  { key: 'patients', icon: <PeopleIcon />, labelKey: 'nav.patients' },
  { key: 'agenda', icon: <CalendarIcon />, labelKey: 'nav.agenda' },
  { key: 'clinical', icon: <ClinicalIcon />, labelKey: 'nav.clinical' },
  { key: 'pharmacy', icon: <PharmacyIcon />, labelKey: 'nav.pharmacy' },
  { key: 'lab', icon: <LabIcon />, labelKey: 'nav.lab' },
  { key: 'billing', icon: <BillingIcon />, labelKey: 'nav.billing' },
  { key: 'admin', icon: <AdminIcon />, labelKey: 'nav.admin' },
]

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    rowGap: '2px',
    padding: tokens.spacingVerticalXS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRight: `1px solid ${tokens.colorNeutralStroke2}`,
    overflowY: 'auto',
    '& svg': { width: '20px', height: '20px' },
  },
  item: { justifyContent: 'flex-start', minWidth: 0 },
  itemCollapsed: { justifyContent: 'center' },
  selected: {
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorBrandForeground1,
  },
})

export function SideNav({
  expanded,
  onItemClick,
}: {
  expanded: boolean
  onItemClick?: () => void
}) {
  const styles = useStyles()
  const { t } = useTranslation()
  const [selected, setSelected] = useState('home')

  return (
    <nav className={styles.root} aria-label="Navigation principale">
      {items.map((it) => {
        const label = t(it.labelKey)
        const btn = (
          <Button
            appearance="subtle"
            icon={it.icon}
            onClick={() => {
              setSelected(it.key)
              onItemClick?.()
            }}
            className={mergeClasses(
              expanded ? styles.item : styles.itemCollapsed,
              selected === it.key && styles.selected,
            )}
            aria-current={selected === it.key ? 'page' : undefined}
          >
            {expanded ? label : undefined}
          </Button>
        )
        return expanded ? (
          <div key={it.key}>{btn}</div>
        ) : (
          <Tooltip key={it.key} content={label} relationship="label" positioning="after">
            {btn}
          </Tooltip>
        )
      })}
    </nav>
  )
}
