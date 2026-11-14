import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button, Dropdown, Option, SearchBox, Spinner,
  Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow,
  Title3, makeStyles, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Edit20Regular } from '@fluentui/react-icons'
import { RowActions, type RowAction } from '@shared/components'
import {
  GEO_LEVELS, deleteGeoDivision, geoParentLevel, getGeoDivisions, referenceKeys,
  type GeoDivision, type GeoLevel,
} from '@features/reference/api'
import { GeoDivisionDialog } from '../components/GeoDivisionDialog'

const PAGE_SIZE = 20
const ANY = '__any__'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  header: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM },
  sub: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase300 },
  filters: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM, rowGap: tokens.spacingVerticalS, flexWrap: 'wrap' },
  filterField: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  label: { fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1 },
  mono: { fontFamily: tokens.fontFamilyMonospace },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
})

export function GeoDivisionsPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [draft, setDraft] = useState<{ level: GeoLevel; parentCode: string; search: string }>({ level: 'Region', parentCode: ANY, search: '' })
  const [applied, setApplied] = useState<{ level: GeoLevel; parentCode: string; search: string }>({ level: 'Region', parentCode: ANY, search: '' })
  const [page, setPage] = useState(1)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<GeoDivision | null>(null)

  const parentLevel = geoParentLevel(draft.level)
  // Parents pour le filtre (selon le niveau sélectionné dans le brouillon).
  const parentsQ = useQuery({
    queryKey: referenceKeys.geo(parentLevel ?? 'none', 'filter'),
    queryFn: () => getGeoDivisions(parentLevel!),
    enabled: parentLevel != null,
  })

  const query = useQuery({
    queryKey: referenceKeys.geo(applied.level, applied.parentCode === ANY ? null : applied.parentCode),
    queryFn: () => getGeoDivisions(applied.level, applied.parentCode === ANY ? null : applied.parentCode),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['reference', 'geo'] })
  const runSearch = () => { setApplied({ ...draft }); setPage(1) }

  const filtered = useMemo(() => {
    const s = applied.search.trim().toLowerCase()
    const list = query.data ?? []
    return s ? list.filter((d) => d.name.toLowerCase().includes(s) || d.code.toLowerCase().includes(s)) : list
  }, [query.data, applied.search])

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const paged = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>{t('adminGeo.title')}</Title3>
          <div className={styles.sub}>{t('adminGeo.subtitle')}</div>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setEditing(null); setDialogOpen(true) }}>
          {t('adminGeo.new')}
        </Button>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('adminGeo.fields.level')}</span>
          <Dropdown
            value={t(`adminGeo.levels.${draft.level}`)}
            selectedOptions={[draft.level]}
            onOptionSelect={(_, d) => setDraft((s) => ({ ...s, level: d.optionValue as GeoLevel, parentCode: ANY }))}
            style={{ minWidth: '160px' }}>
            {GEO_LEVELS.map((l) => <Option key={l} value={l}>{t(`adminGeo.levels.${l}`)}</Option>)}
          </Dropdown>
        </div>
        {parentLevel != null && (
          <div className={styles.filterField}>
            <span className={styles.label}>{t(`adminGeo.levels.${parentLevel}`)}</span>
            <Dropdown
              value={draft.parentCode === ANY ? t('adminGeo.allParents') : (parentsQ.data?.find((p) => p.code === draft.parentCode)?.name ?? draft.parentCode)}
              selectedOptions={[draft.parentCode]}
              onOptionSelect={(_, d) => setDraft((s) => ({ ...s, parentCode: d.optionValue ?? ANY }))}
              style={{ minWidth: '200px' }}>
              <Option value={ANY}>{t('adminGeo.allParents')}</Option>
              {(parentsQ.data ?? []).map((p) => <Option key={p.id} value={p.code}>{`${p.name} (${p.code})`}</Option>)}
            </Dropdown>
          </div>
        )}
        <div className={styles.filterField}>
          <span className={styles.label}>{t('adminGeo.filters.search')}</span>
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))} placeholder={t('adminGeo.filters.searchPlaceholder')} />
        </div>
        <Button appearance="primary" onClick={runSearch}>{t('adminGeo.filters.apply')}</Button>
      </div>

      {query.isPending && <div className={styles.state}><Spinner label={t('adminGeo.loading')} /></div>}
      {query.isError && <div className={styles.state}><span className={styles.error}>{t('adminGeo.loadError')}</span></div>}
      {query.isSuccess && filtered.length === 0 && <div className={styles.state}>{t('adminGeo.empty')}</div>}

      {query.isSuccess && filtered.length > 0 && (
        <>
          <Table aria-label={t('adminGeo.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('adminGeo.fields.code')}</TableHeaderCell>
                <TableHeaderCell>{t('adminGeo.fields.name')}</TableHeaderCell>
                <TableHeaderCell>{t('adminGeo.fields.parent')}</TableHeaderCell>
                <TableHeaderCell>{t('adminRef.fields.displayOrder')}</TableHeaderCell>
                <TableHeaderCell>{t('adminRef.fields.actions')}</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {paged.map((d) => {
                const primary: RowAction[] = [
                  { key: 'edit', label: t('actions.edit'), icon: <Edit20Regular />, color: tokens.colorPaletteTealForeground2, onClick: () => { setEditing(d); setDialogOpen(true) } },
                ]
                return (
                  <TableRow key={d.id}>
                    <TableCell className={styles.mono}>{d.code}</TableCell>
                    <TableCell>{d.name}</TableCell>
                    <TableCell>{d.parentCode ?? '—'}</TableCell>
                    <TableCell>{d.displayOrder}</TableCell>
                    <TableCell>
                      <RowActions primary={primary}
                        onDelete={async () => { await deleteGeoDivision(d.id); invalidate() }}
                        confirmTitle={t('actions.confirmDeleteTitle')}
                        confirmMessage={t('adminGeo.confirmDelete', { name: d.name })} />
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
          <div className={styles.pager}>
            <span className={styles.label}>{t('adminGeo.pager', { page, totalPages, total: filtered.length })}</span>
            <Button size="small" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>{t('adminRef.prev')}</Button>
            <Button size="small" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>{t('adminRef.next')}</Button>
          </div>
        </>
      )}

      <GeoDivisionDialog open={dialogOpen} onOpenChange={setDialogOpen} editing={editing} defaultLevel={applied.level} />
    </div>
  )
}
