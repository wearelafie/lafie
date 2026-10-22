import { Link } from '@tanstack/react-router'
import { Title3, makeStyles, tokens } from '@fluentui/react-components'
import { ArrowLeft20Regular } from '@fluentui/react-icons'

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: '2px', marginBottom: tokens.spacingVerticalM },
  back: { display: 'inline-flex', alignItems: 'center', columnGap: '4px', textDecoration: 'none', color: tokens.colorBrandForeground1, fontSize: tokens.fontSizeBase200 },
  head: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM },
})

export function DetailHeader({ title, backTo, backLabel, actions }: {
  title: string
  backTo: string
  backLabel: string
  actions?: React.ReactNode
}) {
  const styles = useStyles()
  return (
    <div className={styles.root}>
      <Link to={backTo as never} className={styles.back}><ArrowLeft20Regular />{backLabel}</Link>
      <div className={styles.head}>
        <Title3>{title}</Title3>
        {actions}
      </div>
    </div>
  )
}
