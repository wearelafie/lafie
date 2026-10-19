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
    maxWidth: '1040px',
    height: '82vh',
    maxHeight: '680px',
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
    gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))',
    gap: tokens.spacingHorizontalL,
    padding: tokens.spacingVerticalL,
    alignContent: 'start',
  },
  card: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusLarge,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    rowGap: tokens.spacingVerticalS,
    transition: 'box-shadow .12s ease, border-color .12s ease',
    ':hover': {
      boxShadow: tokens.shadow8,
      border: `1px solid ${tokens.colorBrandStroke1}`,
    },
  },
  cardHead: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalS, textDecoration: 'none' },
  cardIcon: { display: 'flex', '& svg': { width: '28px', height: '28px' } },
  cardTitle: { color: tokens.colorNeutralForeground1 },
  subList: { display: 'flex', flexDirection: 'column', rowGap: '1px' },
  subLink: { textDecoration: 'none', display: 'block' },
  subBtn: {
    justifyContent: 'flex-start',
    width: '100%',
    fontWeight: tokens.fontWeightRegular,
    color: tokens.colorNeutralForeground2,
    '& svg': { width: '18px', height: '18px' },
  },
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
  const groups = useMemo(() => {
    if (!q) return navItems
    return navItems
      .map((it) => {
        const parentMatch = t(it.labelKey).toLowerCase().includes(q)
        const kids = (it.children ?? []).filter((c) => t(c.labelKey).toLowerCase().includes(q))
        if (parentMatch) return it
        if (kids.length) return { ...it, children: kids }
        return null
      })
      .filter((it): it is (typeof navItems)[number] => it !== null)
  }, [q, t])

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
          {groups.map((it) => (
            <section key={it.key} className={styles.card}>
              <Link to={it.to} className={styles.cardHead} onClick={close}>
                <span className={styles.cardIcon}>{it.icon}</span>
                <Text className={styles.cardTitle} weight="semibold" size={400}>
                  {t(it.labelKey)}
                </Text>
              </Link>
              {it.children?.length ? (
                <div className={styles.subList}>
                  {it.children.map((c) => (
                    <Link key={c.key} to={c.to} className={styles.subLink} onClick={close}>
                      <Button appearance="subtle" icon={c.icon} className={styles.subBtn}>
                        {t(c.labelKey)}
                      </Button>
                    </Link>
                  ))}
                </div>
              ) : null}
            </section>
          ))}
          {groups.length === 0 && (
            <div className={styles.empty}>
              <Text>{t('launcher.noResult')}</Text>
            </div>
          )}
        </div>
      </DialogSurface>
    </Dialog>
  )
}
