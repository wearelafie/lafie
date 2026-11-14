import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Checkbox, Dropdown, Option, SearchBox, Spinner,
  Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow,
  Title3, makeStyles, tokens,
} from '@fluentui/react-components'
import {
  Add20Regular, Edit20Regular, CheckmarkCircle20Regular, DismissCircle20Regular,
} from '@fluentui/react-icons'
import { RowActions, type RowAction } from '@shared/components'
import {
  deleteReferenceValue, getReferenceCategories, getReferenceValuesAdmin, referenceKeys,
  setReferenceValueActive, type ReferenceValueAdmin,
} from '@features/reference/api'
import { ReferenceValueDialog } from '../components/ReferenceValueDialog'

const PAGE_SIZE = 20

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
  inactive: { opacity: 0.55 },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
})

export function ReferenceValuesPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const categoriesQ = useQuery({ queryKey: referenceKeys.categories(), queryFn: getReferenceCategories })

  const [draft, setDraft] = useState({ category: '', search: '', includeInactive: true })
  const [applied, setApplied] = useState({ category: '', search: '', includeInactive: true })
  const [page, setPage] = useState(1)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<ReferenceValueAdmin | null>(null)

  // Catégorie par défaut = première disponible, une fois chargées.
  useEffect(() => {
    if (!applied.category && categoriesQ.data && categoriesQ.data.length > 0) {
      const first = categoriesQ.data[0].category
      setDraft((d) => ({ ...d, category: first }))
      setApplied((a) => ({ ...a, category: first }))
    }
  }, [categoriesQ.data, applied.category])

  const query = useQuery({
    queryKey: referenceKeys.adminValues(applied.category, applied.search, applied.includeInactive),
    queryFn: () => getReferenceValuesAdmin(applied.category, { search: applied.search || undefined, includeInactive: applied.includeInactive }),
    enabled: applied.category !== '',
  })

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['reference', 'admin'] })
    void queryClient.invalidateQueries({ queryKey: referenceKeys.categories() })
  }
  const activeMut = useMutation({
    mutationFn: ({ v, active }: { v: ReferenceValueAdmin; active: boolean }) => setReferenceValueActive(v.category, v.code, active),
    onSuccess: invalidate,
  })

  const runSearch = () => { setApplied({ ...draft }); setPage(1) }

  const rows = query.data ?? []
  const totalPages = Math.max(1, Math.ceil(rows.length / PAGE_SIZE))
  const paged = useMemo(() => rows.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE), [rows, page])

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>{t('adminRef.title')}</Title3>
          <div className={styles.sub}>{t('adminRef.subtitle')}</div>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} disabled={!applied.category} onClick={() => { setEditing(null); setDialogOpen(true) }}>
          {t('adminRef.values.new')}
        </Button>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('adminRef.fields.category')}</span>
          <Dropdown
            value={draft.category}
            selectedOptions={[draft.category]}
            onOptionSelect={(_, d) => setDraft((s) => ({ ...s, category: d.optionValue ?? '' }))}
            style={{ minWidth: '240px' }}>
            {(categoriesQ.data ?? []).map((c) => (
              <Option key={c.category} value={c.category}>{`${c.category} (${c.count})`}</Option>
            ))}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('adminRef.filters.search')}</span>
          <SearchBox value={draft.search} onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))} placeholder={t('adminRef.filters.searchPlaceholder')} />
        </div>
        <Checkbox label={t('adminRef.filters.includeInactive')} checked={draft.includeInactive} onChange={(_, d) => setDraft((s) => ({ ...s, includeInactive: !!d.checked }))} />
        <Button appearance="primary" onClick={runSearch}>{t('adminRef.filters.apply')}</Button>
      </div>

      {(categoriesQ.isPending || (applied.category && query.isPending)) && <div className={styles.state}><Spinner label={t('adminRef.loading')} /></div>}
      {query.isError && <div className={styles.state}><span className={styles.error}>{t('adminRef.loadError')}</span></div>}
      {query.isSuccess && rows.length === 0 && <div className={styles.state}>{t('adminRef.empty')}</div>}

      {query.isSuccess && rows.length > 0 && (
        <>
          <Table aria-label={t('adminRef.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('adminRef.fields.code')}</TableHeaderCell>
                <TableHeaderCell>{t('adminRef.fields.labelFr')}</TableHeaderCell>
                <TableHeaderCell>{t('adminRef.fields.labelEn')}</TableHeaderCell>
                <TableHeaderCell>{t('adminRef.fields.displayOrder')}</TableHeaderCell>
                <TableHeaderCell>{t('adminRef.fields.active')}</TableHeaderCell>
                <TableHeaderCell>{t('adminRef.fields.actions')}</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {paged.map((v) => {
                const primary: RowAction[] = [
                  { key: 'edit', label: t('actions.edit'), icon: <Edit20Regular />, color: tokens.colorPaletteTealForeground2, onClick: () => { setEditing(v); setDialogOpen(true) } },
                ]
                const toggle: RowAction = v.active
                  ? { key: 'deactivate', label: t('adminRef.deactivate'), icon: <DismissCircle20Regular />, color: tokens.colorPaletteYellowForeground1, onClick: () => activeMut.mutate({ v, active: false }) }
                  : { key: 'activate', label: t('adminRef.activate'), icon: <CheckmarkCircle20Regular />, color: tokens.colorPaletteGreenForeground1, onClick: () => activeMut.mutate({ v, active: true }) }
                return (
                  <TableRow key={v.code} className={v.active ? undefined : styles.inactive}>
                    <TableCell className={styles.mono}>{v.code}</TableCell>
                    <TableCell>{v.labelFr}</TableCell>
                    <TableCell>{v.labelEn}</TableCell>
                    <TableCell>{v.displayOrder}</TableCell>
                    <TableCell>
                      <Badge appearance="filled" color={v.active ? 'success' : 'subtle'}>
                        {t(v.active ? 'adminRef.status.active' : 'adminRef.status.inactive')}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <RowActions primary={primary} menu={[toggle]}
                        onDelete={async () => { await deleteReferenceValue(v.category, v.code); invalidate() }}
                        confirmTitle={t('actions.confirmDeleteTitle')}
                        confirmMessage={t('adminRef.confirmDelete', { code: v.code })} />
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
          <div className={styles.pager}>
            <span className={styles.label}>{t('adminRef.pager', { page, totalPages, total: rows.length })}</span>
            <Button size="small" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>{t('adminRef.prev')}</Button>
            <Button size="small" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>{t('adminRef.next')}</Button>
          </div>
        </>
      )}

      <ReferenceValueDialog open={dialogOpen} onOpenChange={setDialogOpen} category={applied.category} editing={editing} />
    </div>
  )
}
