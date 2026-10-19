import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import {
  Button,
  Dialog,
  DialogSurface,
  DialogTrigger,
  SearchBox,
  Text,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { Dismiss24Regular } from '@fluentui/react-icons'
import { navItems } from '@app/nav/navModel'

const useStyles = makeStyles({
  surface: {
    width: '92vw',
    maxWidth: '920px',
    height: '78vh',
    maxHeight: '600px',
    padding: 0,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
    backgroundColor: '#F4F8FA',
    '@media (max-width: 768px)': {
      width: '100vw',
      height: '100vh',
      maxWidth: 'none',
      maxHeight: 'none',
      borderRadius: 0,
    },
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    columnGap: tokens.spacingHorizontalM,
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalL}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  title: { flexShrink: 0 },
  searchWrap: { flex: 1, display: 'flex', justifyContent: 'center' },
  search: { width: '100%', maxWidth: '420px' },
  grid: {
    flex: 1,
    overflowY: 'auto',
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))',
    gap: tokens.spacingHorizontalL,
    padding: tokens.spacingVerticalXL,
    alignContent: 'start',
  },
  tile: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusLarge,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    padding: tokens.spacingVerticalL,
    minHeight: '120px',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    rowGap: tokens.spacingVerticalS,
    textAlign: 'center',
    textDecoration: 'none',
    color: tokens.colorNeutralForeground1,
    transition: 'box-shadow .12s ease, border-color .12s ease, transform .12s ease',
    ':hover': {
      boxShadow: tokens.shadow8,
      border: `1px solid ${tokens.colorBrandStroke1}`,
      transform: 'translateY(-2px)',
    },
  },
  tileIcon: { display: 'flex', '& svg': { width: '40px', height: '40px' } },
  empty: {
    gridColumn: '1 / -1',
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
    padding: tokens.spacingVerticalXXL,
  },
})

export function AppLauncherDialog({
  open,
  onOpenChange,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const styles = useStyles()
  const { t } = useTranslation()
  const [query, setQuery] = useState('')
  const close = () => onOpenChange(false)

  // Réinitialise la recherche à chaque ouverture.
  useEffect(() => {
    if (open) setQuery('')
  }, [open])

  const q = query.trim().toLowerCase()
  const modules = useMemo(
    () => (q ? navItems.filter((it) => t(it.labelKey).toLowerCase().includes(q)) : navItems),
    [q, t],
  )

  return (
    <Dialog open={open} onOpenChange={(_, d) => onOpenChange(d.open)} modalType="modal">
      <DialogSurface className={styles.surface}>
        <div className={styles.header}>
          <Text className={styles.title} weight="semibold" size={500}>
            {t('launcher.title')}
          </Text>
          <div className={styles.searchWrap}>
            <SearchBox
              className={styles.search}
              placeholder={t('launcher.search')}
              value={query}
              onChange={(_, d) => setQuery(d.value)}
            />
          </div>
          <DialogTrigger disableButtonEnhancement>
            <Button appearance="subtle" icon={<Dismiss24Regular />} aria-label={t('settings.close')} />
          </DialogTrigger>
        </div>

        <div className={styles.grid}>
          {modules.map((it) => (
            <Link key={it.key} to={it.to} className={styles.tile} onClick={close}>
              <span className={styles.tileIcon}>{it.icon}</span>
              <Text weight="semibold" size={400}>
                {t(it.labelKey)}
              </Text>
            </Link>
          ))}
          {modules.length === 0 && (
            <div className={styles.empty}>
              <Text>{t('launcher.noResult')}</Text>
            </div>
          )}
        </div>
      </DialogSurface>
    </Dialog>
  )
}
