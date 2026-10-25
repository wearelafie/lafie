import { Link } from '@tanstack/react-router'
import { Title3, makeStyles, tokens } from '@fluentui/react-components'
import { ChevronRight16Regular } from '@fluentui/react-icons'

export type Crumb = { label: string; to: string; onClick?: () => void }

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXS, marginBottom: tokens.spacingVerticalM },
  trail: { display: 'flex', alignItems: 'center', flexWrap: 'wrap', columnGap: '4px', rowGap: '2px', fontSize: tokens.fontSizeBase200 },
  crumb: { textDecoration: 'none', color: tokens.colorBrandForeground1, '&:hover': { textDecoration: 'underline' } },
  sep: { color: tokens.colorNeutralForeground4, display: 'inline-flex', '& svg': { width: '14px', height: '14px' } },
  head: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM },
})

/** En-tête de fiche détail : fil d'Ariane (ancêtres cliquables) + titre + actions. */
export function DetailHeader({ title, crumbs, actions }: {
  title: string
  crumbs: Crumb[]
  actions?: React.ReactNode
}) {
  const styles = useStyles()
  return (
    <div className={styles.root}>
      <div className={styles.trail}>
        {crumbs.map((c, i) => (
          <span key={i} className={styles.trail}>
            <Link to={c.to as never} className={styles.crumb} onClick={c.onClick}>{c.label}</Link>
            <span className={styles.sep}><ChevronRight16Regular /></span>
          </span>
        ))}
      </div>
      <div className={styles.head}>
        <Title3>{title}</Title3>
        {actions}
      </div>
    </div>
  )
}
