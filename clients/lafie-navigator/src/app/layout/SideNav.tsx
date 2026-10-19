import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useRouterState } from '@tanstack/react-router'
import {
  Button,
  Menu,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  Tooltip,
  makeStyles,
  mergeClasses,
  tokens,
} from '@fluentui/react-components'
import { ChevronDown20Regular, ChevronRight20Regular } from '@fluentui/react-icons'
import { navItems, type NavItem } from '@app/nav/navModel'

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
  item: { justifyContent: 'flex-start', minWidth: 0, width: '100%' },
  itemCollapsed: { justifyContent: 'center' },
  parentRow: { display: 'flex', alignItems: 'center', columnGap: '2px' },
  chevron: { minWidth: '28px', flexShrink: 0 },
  parentBtn: { flex: 1, justifyContent: 'flex-start', minWidth: 0 },
  children: {
    display: 'flex',
    flexDirection: 'column',
    rowGap: '1px',
    paddingLeft: tokens.spacingHorizontalL,
    marginBottom: tokens.spacingVerticalXS,
  },
  childBtn: {
    justifyContent: 'flex-start',
    minWidth: 0,
    width: '100%',
    fontWeight: tokens.fontWeightRegular,
    color: tokens.colorNeutralForeground2,
  },
  link: { textDecoration: 'none', display: 'block' },
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
  const navigate = useNavigate()
  const pathname = useRouterState({ select: (s) => s.location.pathname })

  const isActive = (to: string) => pathname === to
  const isInSection = (item: NavItem) =>
    isActive(item.to) || (item.children?.some((c) => isActive(c.to)) ?? false) ||
    (item.to !== '/' && pathname.startsWith(`${item.to}/`))

  // Groupes ouverts : au départ, celui qui contient la route active.
  const [open, setOpen] = useState<Record<string, boolean>>(() =>
    Object.fromEntries(navItems.map((it) => [it.key, isInSection(it)])),
  )
  const toggle = (key: string) => setOpen((o) => ({ ...o, [key]: !o[key] }))

  // --- rail réduit (48px) : icône + flyout menu pour les enfants ---
  if (!expanded) {
    return (
      <nav className={styles.root} aria-label="Navigation principale">
        {navItems.map((it) => {
          const label = t(it.labelKey)
          if (!it.children?.length) {
            return (
              <Tooltip key={it.key} content={label} relationship="label" positioning="after">
                <Link to={it.to} className={styles.link} onClick={onItemClick}>
                  <Button
                    appearance="subtle"
                    icon={it.icon}
                    className={mergeClasses(styles.itemCollapsed, isActive(it.to) && styles.selected)}
                    aria-current={isActive(it.to) ? 'page' : undefined}
                    aria-label={label}
                  />
                </Link>
              </Tooltip>
            )
          }
          return (
            <Menu key={it.key} openOnHover hoverDelay={120} positioning="after-top">
              <MenuTrigger disableButtonEnhancement>
                <Button
                  appearance="subtle"
                  icon={it.icon}
                  className={mergeClasses(styles.itemCollapsed, isInSection(it) && styles.selected)}
                  aria-label={label}
                />
              </MenuTrigger>
              <MenuPopover>
                <MenuList>
                  {it.children.map((c) => (
                    <MenuItem
                      key={c.key}
                      icon={c.icon}
                      onClick={() => {
                        void navigate({ to: c.to })
                        onItemClick?.()
                      }}
                    >
                      {t(c.labelKey)}
                    </MenuItem>
                  ))}
                </MenuList>
              </MenuPopover>
            </Menu>
          )
        })}
      </nav>
    )
  }

  // --- rail étendu (240px) : accordéon ---
  return (
    <nav className={styles.root} aria-label="Navigation principale">
      {navItems.map((it) => {
        const label = t(it.labelKey)
        if (!it.children?.length) {
          return (
            <Link key={it.key} to={it.to} className={styles.link} onClick={onItemClick}>
              <Button
                appearance="subtle"
                icon={it.icon}
                className={mergeClasses(styles.item, isActive(it.to) && styles.selected)}
                aria-current={isActive(it.to) ? 'page' : undefined}
              >
                {label}
              </Button>
            </Link>
          )
        }
        const isOpen = open[it.key]
        return (
          <div key={it.key}>
            <div className={styles.parentRow}>
              <Button
                appearance="subtle"
                className={styles.chevron}
                icon={isOpen ? <ChevronDown20Regular /> : <ChevronRight20Regular />}
                onClick={() => toggle(it.key)}
                aria-label={label}
                aria-expanded={isOpen}
              />
              <Link to={it.to} className={styles.link} style={{ flex: 1 }} onClick={onItemClick}>
                <Button
                  appearance="subtle"
                  icon={it.icon}
                  className={mergeClasses(styles.parentBtn, isInSection(it) && styles.selected)}
                >
                  {label}
                </Button>
              </Link>
            </div>
            {isOpen && (
              <div className={styles.children}>
                {it.children.map((c) => (
                  <Link key={c.key} to={c.to} className={styles.link} onClick={onItemClick}>
                    <Button
                      appearance="subtle"
                      icon={c.icon}
                      className={mergeClasses(styles.childBtn, isActive(c.to) && styles.selected)}
                      aria-current={isActive(c.to) ? 'page' : undefined}
                    >
                      {t(c.labelKey)}
                    </Button>
                  </Link>
                ))}
              </div>
            )}
          </div>
        )
      })}
    </nav>
  )
}
