import { useTranslation } from 'react-i18next'
import { Link, useRouterState } from '@tanstack/react-router'
import { Button, Text, Tooltip, makeStyles, mergeClasses, tokens } from '@fluentui/react-components'
import { findActiveModule } from '@app/nav/navModel'

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
  // En-tête = grand module actif.
  header: {
    display: 'flex',
    alignItems: 'center',
    columnGap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalS}`,
    marginBottom: tokens.spacingVerticalXS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  headerCollapsed: { justifyContent: 'center', padding: tokens.spacingVerticalS },
  headerIcon: { display: 'flex', '& svg': { width: '24px', height: '24px' } },
  headerText: { color: tokens.colorNeutralForeground1 },
  link: { textDecoration: 'none', display: 'block' },
  item: { justifyContent: 'flex-start', minWidth: 0, width: '100%' },
  itemCollapsed: { justifyContent: 'center' },
  selected: {
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorBrandForeground1,
  },
  empty: {
    padding: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
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
  const pathname = useRouterState({ select: (s) => s.location.pathname })
  const active = findActiveModule(pathname)
  const children = active.children ?? []
  const isActive = (to: string) => pathname === to

  const label = t(active.labelKey)

  return (
    <nav className={styles.root} aria-label={label}>
      {/* En-tête : grand module courant (lien vers sa racine) */}
      {expanded ? (
        <Link to={active.to} className={styles.link} onClick={onItemClick}>
          <div className={styles.header}>
            <span className={styles.headerIcon}>{active.icon}</span>
            <Text className={styles.headerText} weight="semibold" size={400}>
              {label}
            </Text>
          </div>
        </Link>
      ) : (
        <Tooltip content={label} relationship="label" positioning="after">
          <Link to={active.to} className={styles.link} onClick={onItemClick}>
            <div className={mergeClasses(styles.header, styles.headerCollapsed)}>
              <span className={styles.headerIcon}>{active.icon}</span>
            </div>
          </Link>
        </Tooltip>
      )}

      {/* Sous-modules du grand module actif */}
      {children.map((c) => {
        const childLabel = t(c.labelKey)
        const btn = (
          <Link to={c.to} className={styles.link} onClick={onItemClick}>
            <Button
              appearance="subtle"
              icon={c.icon}
              className={mergeClasses(
                expanded ? styles.item : styles.itemCollapsed,
                isActive(c.to) && styles.selected,
              )}
              aria-current={isActive(c.to) ? 'page' : undefined}
            >
              {expanded ? childLabel : undefined}
            </Button>
          </Link>
        )
        return expanded ? (
          <div key={c.key}>{btn}</div>
        ) : (
          <Tooltip key={c.key} content={childLabel} relationship="label" positioning="after">
            {btn}
          </Tooltip>
        )
      })}

      {expanded && children.length === 0 && active.to !== '/' && (
        <Text className={styles.empty} size={200}>
          {t('common.comingSoon')}
        </Text>
      )}
    </nav>
  )
}
