import type { ReactNode } from 'react'
import { Text, makeStyles, tokens } from '@fluentui/react-components'

export type FieldRow = { label: string; value: ReactNode }

const useStyles = makeStyles({
  grid: {
    display: 'grid',
    gridTemplateColumns: 'minmax(160px, 240px) 1fr',
    columnGap: tokens.spacingHorizontalXL,
    rowGap: tokens.spacingVerticalS,
    maxWidth: '720px',
    '@media (max-width: 520px)': { gridTemplateColumns: '1fr' },
  },
  label: { color: tokens.colorNeutralForeground3 },
  value: { color: tokens.colorNeutralForeground1 },
})

/** Grille label/valeur en lecture seule pour les pages de détail. */
export function FieldList({ rows }: { rows: FieldRow[] }) {
  const styles = useStyles()
  return (
    <div className={styles.grid}>
      {rows.map((r, i) => (
        <div key={i} style={{ display: 'contents' }}>
          <Text className={styles.label} size={200}>{r.label}</Text>
          <Text className={styles.value}>{r.value ?? '—'}</Text>
        </div>
      ))}
    </div>
  )
}
