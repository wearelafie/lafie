import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Checkbox, Dropdown, Field, Input, Option, SearchBox, Spinner, Textarea, Title3,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import {
  Add20Regular, ArrowDownload20Regular, ArrowUpload20Regular, Edit20Regular,
  Open20Regular, Search20Regular, Warning20Regular,
} from '@fluentui/react-icons'
import { useActiveFacility } from '@features/auth'
import { FormDrawer, RowActions, type RowAction } from '@shared/components'
import { medicationKeys, getMedications, type Medication } from '../api'
import {
  stockKeys, getStockItems, getMedicationStock, receiveStock, issueStock, setReorderThreshold, adjustLot,
  type StockFilter, type StockItem, type StockLot,
} from '../api'

const PAGE_SIZE = 10

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  header: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM },
  subtitle: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  filters: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM, rowGap: tokens.spacingVerticalS, flexWrap: 'wrap' },
  filterField: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  label: { fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 },
  actions: { display: 'flex', columnGap: tokens.spacingHorizontalS },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  low: { color: tokens.colorPaletteRedForeground1, fontWeight: tokens.fontWeightSemibold },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '480px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1, minWidth: 0 },
  section: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalS, marginTop: tokens.spacingVerticalM },
  sectionTitle: { fontWeight: tokens.fontWeightSemibold, fontSize: tokens.fontSizeBase300 },
  thresholdRow: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM },
})

type ReceiveForm = {
  medicationId: string; lotNumber: string; expiryOn: string; quantity: string
  unitCost: string; supplierName: string; receivedOn: string; notes: string
}
const emptyReceive: ReceiveForm = {
  medicationId: '', lotNumber: '', expiryOn: '', quantity: '', unitCost: '', supplierName: '', receivedOn: '', notes: '',
}
type IssueForm = { medicationId: string; quantity: string; reason: string; notes: string }
const emptyIssue: IssueForm = { medicationId: '', quantity: '', reason: '', notes: '' }

const fmtDate = (iso: string | null, locale: string): string =>
  iso ? new Date(iso).toLocaleDateString(locale) : '—'

export function PharmacyStockPage() {
  const styles = useStyles()
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const facilityId = useActiveFacility()

  const [draft, setDraft] = useState<{ search: string; low: boolean }>({ search: '', low: false })
  const [applied, setApplied] = useState<StockFilter>({ page: 1, pageSize: PAGE_SIZE })

  const [receiveOpen, setReceiveOpen] = useState(false)
  const [issueOpen, setIssueOpen] = useState(false)
  const [rf, setRf] = useState<ReceiveForm>(emptyReceive)
  const [isf, setIsf] = useState<IssueForm>(emptyIssue)
  const setR = <K extends keyof ReceiveForm>(k: K, v: ReceiveForm[K]) => setRf((s) => ({ ...s, [k]: v }))
  const setI = <K extends keyof IssueForm>(k: K, v: IssueForm[K]) => setIsf((s) => ({ ...s, [k]: v }))

  const [detail, setDetail] = useState<StockItem | null>(null)
  const [adjust, setAdjust] = useState<StockLot | null>(null)
  const [adjustQty, setAdjustQty] = useState('')
  const [adjustReason, setAdjustReason] = useState('')
  const [thresholdDraft, setThresholdDraft] = useState('')

  const query = useQuery({
    queryKey: stockKeys.list(facilityId ?? '', applied),
    queryFn: () => getStockItems(facilityId!, applied),
    enabled: !!facilityId,
  })

  // Catalogue actif de l'établissement : alimente les sélecteurs de médicament des drawers.
  const medsQuery = useQuery({
    queryKey: medicationKeys.list(facilityId ?? '', { status: 'Active', pageSize: 200 }),
    queryFn: () => getMedications(facilityId!, { status: 'Active', pageSize: 200 }),
    enabled: !!facilityId && (receiveOpen || issueOpen),
  })
  const meds: Medication[] = medsQuery.data?.items ?? []
  const medLabel = (id: string) => { const m = meds.find((x) => x.id === id); return m ? `${m.name} (${m.code})` : '' }

  const detailQuery = useQuery({
    queryKey: stockKeys.detail(facilityId ?? '', detail?.medicationId ?? ''),
    queryFn: () => getMedicationStock(facilityId!, detail!.medicationId),
    enabled: !!facilityId && !!detail,
  })

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: stockKeys.all(facilityId ?? '') })
  }

  const receiveMut = useMutation({
    mutationFn: async () => {
      await receiveStock(facilityId!, {
        medicationId: rf.medicationId,
        lotNumber: rf.lotNumber.trim() || null,
        expiryOn: rf.expiryOn || null,
        quantity: Number(rf.quantity),
        unitCost: rf.unitCost.trim() ? Number(rf.unitCost) : null,
        supplierName: rf.supplierName.trim() || null,
        receivedOn: rf.receivedOn || null,
        notes: rf.notes.trim() || null,
      })
    },
    onSuccess: () => { invalidate(); setReceiveOpen(false); setRf(emptyReceive) },
  })

  const issueMut = useMutation({
    mutationFn: async () => {
      await issueStock(facilityId!, {
        medicationId: isf.medicationId,
        quantity: Number(isf.quantity),
        reason: isf.reason.trim() || null,
        notes: isf.notes.trim() || null,
      })
    },
    onSuccess: () => { invalidate(); setIssueOpen(false); setIsf(emptyIssue) },
  })

  const thresholdMut = useMutation({
    mutationFn: async () => { await setReorderThreshold(facilityId!, detail!.medicationId, Number(thresholdDraft || '0')) },
    onSuccess: invalidate,
  })

  const adjustMut = useMutation({
    mutationFn: async () => { await adjustLot(adjust!.id, Number(adjustQty), adjustReason.trim() || null) },
    onSuccess: () => {
      invalidate()
      void queryClient.invalidateQueries({ queryKey: stockKeys.detail(facilityId ?? '', detail?.medicationId ?? '') })
      setAdjust(null); setAdjustQty(''); setAdjustReason('')
    },
  })

  const runSearch = () => setApplied({ page: 1, pageSize: PAGE_SIZE, search: draft.search.trim() || undefined, low: draft.low || undefined })

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))

  const openDetail = (s: StockItem) => { setDetail(s); setThresholdDraft(String(s.reorderThreshold)) }

  const receiveValid = rf.medicationId !== '' && Number(rf.quantity) > 0 && !receiveMut.isPending
  const issueValid = isf.medicationId !== '' && Number(isf.quantity) > 0 && !issueMut.isPending
  const adjustValid = adjustQty.trim() !== '' && Number(adjustQty) >= 0 && !adjustMut.isPending

  const lotActions = (l: StockLot): RowAction[] => [
    { key: 'adjust', label: t('pharmacy.stock.adjust'), icon: <Edit20Regular />, onClick: () => { setAdjust(l); setAdjustQty(String(l.quantityOnHand)); setAdjustReason('') } },
  ]

  if (!facilityId) return <div className={styles.root}><div className={styles.state}>{t('pharmacy.noFacility')}</div></div>

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>{t('pharmacy.stock.title')}</Title3>
          <div className={styles.subtitle}>{t('pharmacy.stock.subtitle')}</div>
        </div>
      </div>

      <div className={styles.header}>
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('pharmacy.filters.search')}</span>
            <SearchBox value={draft.search} placeholder={t('pharmacy.stock.searchPlaceholder')}
              onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
              onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>&nbsp;</span>
            <Checkbox label={t('pharmacy.stock.lowOnly')} checked={draft.low} onChange={(_, d) => setDraft((s) => ({ ...s, low: !!d.checked }))} />
          </div>
          <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('pharmacy.filters.apply')}</Button>
        </div>
        <div className={styles.actions}>
          <Button appearance="secondary" icon={<ArrowUpload20Regular />} onClick={() => { setIsf(emptyIssue); setIssueOpen(true) }}>{t('pharmacy.stock.issue')}</Button>
          <Button appearance="primary" icon={<ArrowDownload20Regular />} onClick={() => { setRf(emptyReceive); setReceiveOpen(true) }}>{t('pharmacy.stock.receive')}</Button>
        </div>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('pharmacy.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('pharmacy.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('pharmacy.stock.empty')}</div>
      ) : (
        <Table aria-label={t('pharmacy.stock.title')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('pharmacy.fields.code')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.name')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.stock.onHand')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.stock.threshold')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.stock.lots')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.stock.earliestExpiry')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.actions')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((s) => (
              <TableRow key={s.medicationId}>
                <TableCell><TableCellLayout>{s.code}</TableCellLayout></TableCell>
                <TableCell>
                  <TableCellLayout>
                    {s.name}{s.controlled && <> <Badge appearance="tint" color="danger" size="small">{t('pharmacy.fields.controlled')}</Badge></>}
                  </TableCellLayout>
                </TableCell>
                <TableCell>
                  <TableCellLayout>
                    <span className={s.lowStock ? styles.low : undefined}>{s.onHand} {s.unit}</span>
                    {s.lowStock && <> <Badge appearance="tint" color="danger" size="small" icon={<Warning20Regular />}>{t('pharmacy.stock.low')}</Badge></>}
                  </TableCellLayout>
                </TableCell>
                <TableCell><TableCellLayout>{s.reorderThreshold > 0 ? s.reorderThreshold : <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{s.lotCount}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{fmtDate(s.earliestExpiry, i18n.language)}</TableCellLayout></TableCell>
                <TableCell>
                  <RowActions primary={[{ key: 'detail', label: t('pharmacy.stock.detail'), icon: <Open20Regular />, onClick: () => openDetail(s) }]} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <div className={styles.pager}>
        <span className={styles.muted}>{t('pharmacy.pager', { page, totalPages, total })}</span>
        <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => go(page - 1)}>{t('pharmacy.prev')}</Button>
        <Button size="small" appearance="subtle" disabled={page >= totalPages} onClick={() => go(page + 1)}>{t('pharmacy.next')}</Button>
      </div>

      {/* Entrée de stock (réception d'un lot) */}
      <FormDrawer
        open={receiveOpen}
        onOpenChange={setReceiveOpen}
        title={t('pharmacy.stock.receive')}
        onSubmit={() => { if (receiveValid) receiveMut.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setReceiveOpen(false)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={!receiveValid} icon={receiveMut.isPending ? <Spinner size="tiny" /> : <Add20Regular />}>
            {t('pharmacy.stock.receiveSubmit')}
          </Button>
        </>}>
        <div className={styles.form}>
          <Field label={t('pharmacy.stock.medication')} required>
            <Dropdown value={medLabel(rf.medicationId)} selectedOptions={[rf.medicationId]} placeholder={t('pharmacy.stock.medicationPlaceholder')}
              onOptionSelect={(_, d) => setR('medicationId', d.optionValue ?? '')}>
              {meds.map((m) => <Option key={m.id} value={m.id}>{`${m.name} (${m.code})`}</Option>)}
            </Dropdown>
          </Field>
          <div className={styles.row}>
            <Field className={styles.grow} label={t('pharmacy.stock.lotNumber')}>
              <Input value={rf.lotNumber} onChange={(_, d) => setR('lotNumber', d.value)} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.stock.expiryOn')}>
              <Input type="date" value={rf.expiryOn} onChange={(_, d) => setR('expiryOn', d.value)} />
            </Field>
          </div>
          <div className={styles.row}>
            <Field className={styles.grow} label={t('pharmacy.stock.quantity')} required>
              <Input type="number" min={1} value={rf.quantity} onChange={(_, d) => setR('quantity', d.value)} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.stock.unitCost')}>
              <Input type="number" min={0} step="0.01" value={rf.unitCost} onChange={(_, d) => setR('unitCost', d.value)} />
            </Field>
          </div>
          <div className={styles.row}>
            <Field className={styles.grow} label={t('pharmacy.stock.supplier')}>
              <Input value={rf.supplierName} onChange={(_, d) => setR('supplierName', d.value)} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.stock.receivedOn')}>
              <Input type="date" value={rf.receivedOn} onChange={(_, d) => setR('receivedOn', d.value)} />
            </Field>
          </div>
          <Field label={t('pharmacy.fields.notes')}>
            <Textarea value={rf.notes} onChange={(_, d) => setR('notes', d.value)} resize="vertical" />
          </Field>
          {receiveMut.isError && <span className={styles.error}>{(receiveMut.error as Error).message}</span>}
        </div>
      </FormDrawer>

      {/* Sortie de stock (consommation FEFO) */}
      <FormDrawer
        open={issueOpen}
        onOpenChange={setIssueOpen}
        title={t('pharmacy.stock.issue')}
        onSubmit={() => { if (issueValid) issueMut.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setIssueOpen(false)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={!issueValid} icon={issueMut.isPending ? <Spinner size="tiny" /> : <ArrowUpload20Regular />}>
            {t('pharmacy.stock.issueSubmit')}
          </Button>
        </>}>
        <div className={styles.form}>
          <Field label={t('pharmacy.stock.medication')} required>
            <Dropdown value={medLabel(isf.medicationId)} selectedOptions={[isf.medicationId]} placeholder={t('pharmacy.stock.medicationPlaceholder')}
              onOptionSelect={(_, d) => setI('medicationId', d.optionValue ?? '')}>
              {meds.map((m) => <Option key={m.id} value={m.id}>{`${m.name} (${m.code})`}</Option>)}
            </Dropdown>
          </Field>
          <Field label={t('pharmacy.stock.quantity')} required>
            <Input type="number" min={1} value={isf.quantity} onChange={(_, d) => setI('quantity', d.value)} />
          </Field>
          <span className={styles.muted}>{t('pharmacy.stock.fefoHint')}</span>
          <Field label={t('pharmacy.stock.reason')}>
            <Input value={isf.reason} onChange={(_, d) => setI('reason', d.value)} />
          </Field>
          <Field label={t('pharmacy.fields.notes')}>
            <Textarea value={isf.notes} onChange={(_, d) => setI('notes', d.value)} resize="vertical" />
          </Field>
          {issueMut.isError && <span className={styles.error}>{(issueMut.error as Error).message}</span>}
        </div>
      </FormDrawer>

      {/* Détail d'un médicament : seuil, lots, mouvements */}
      <FormDrawer
        open={!!detail}
        onOpenChange={(v) => { if (!v) setDetail(null) }}
        title={detail ? `${detail.name} — ${detail.code}` : ''}
        footer={<Button appearance="secondary" type="button" onClick={() => setDetail(null)}>{t('actions.close')}</Button>}>
        <div className={styles.form}>
          {detailQuery.isLoading ? (
            <div className={styles.state}><Spinner size="small" label={t('pharmacy.loading')} /></div>
          ) : detailQuery.isError || !detailQuery.data ? (
            <div className={mergeClasses(styles.state, styles.error)}>{t('pharmacy.loadError')}</div>
          ) : (
            <>
              <div className={styles.thresholdRow}>
                <Field label={t('pharmacy.stock.threshold')}>
                  <Input type="number" min={0} value={thresholdDraft} onChange={(_, d) => setThresholdDraft(d.value)} style={{ maxWidth: '140px' }} />
                </Field>
                <Button appearance="secondary" type="button" disabled={thresholdMut.isPending} onClick={() => thresholdMut.mutate()}>
                  {t('pharmacy.stock.saveThreshold')}
                </Button>
                <span className={styles.muted}>{t('pharmacy.stock.onHandValue', { value: detailQuery.data.onHand })}</span>
              </div>

              <div className={styles.section}>
                <span className={styles.sectionTitle}>{t('pharmacy.stock.lots')}</span>
                {detailQuery.data.lots.length === 0 ? (
                  <span className={styles.muted}>{t('pharmacy.stock.noLots')}</span>
                ) : (
                  <Table size="small" aria-label={t('pharmacy.stock.lots')}>
                    <TableHeader>
                      <TableRow>
                        <TableHeaderCell>{t('pharmacy.stock.lotNumber')}</TableHeaderCell>
                        <TableHeaderCell>{t('pharmacy.stock.expiryOn')}</TableHeaderCell>
                        <TableHeaderCell>{t('pharmacy.stock.onHand')}</TableHeaderCell>
                        <TableHeaderCell>{t('pharmacy.stock.supplier')}</TableHeaderCell>
                        <TableHeaderCell>{t('pharmacy.fields.actions')}</TableHeaderCell>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {detailQuery.data.lots.map((l) => (
                        <TableRow key={l.id}>
                          <TableCell><TableCellLayout>{l.lotNumber ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                          <TableCell><TableCellLayout>{fmtDate(l.expiryOn, i18n.language)}</TableCellLayout></TableCell>
                          <TableCell><TableCellLayout>{l.quantityOnHand} / {l.quantityInitial}</TableCellLayout></TableCell>
                          <TableCell><TableCellLayout>{l.supplierName ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                          <TableCell><RowActions primary={lotActions(l)} /></TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </div>

              <div className={styles.section}>
                <span className={styles.sectionTitle}>{t('pharmacy.stock.movements')}</span>
                {detailQuery.data.movements.length === 0 ? (
                  <span className={styles.muted}>{t('pharmacy.stock.noMovements')}</span>
                ) : (
                  <Table size="small" aria-label={t('pharmacy.stock.movements')}>
                    <TableHeader>
                      <TableRow>
                        <TableHeaderCell>{t('pharmacy.stock.date')}</TableHeaderCell>
                        <TableHeaderCell>{t('pharmacy.stock.type')}</TableHeaderCell>
                        <TableHeaderCell>{t('pharmacy.stock.quantity')}</TableHeaderCell>
                        <TableHeaderCell>{t('pharmacy.stock.lotNumber')}</TableHeaderCell>
                        <TableHeaderCell>{t('pharmacy.stock.reason')}</TableHeaderCell>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {detailQuery.data.movements.map((mv) => (
                        <TableRow key={mv.id}>
                          <TableCell><TableCellLayout>{fmtDate(mv.occurredOnUtc, i18n.language)}</TableCellLayout></TableCell>
                          <TableCell><TableCellLayout>{t(`pharmacy.stock.types.${mv.type}`)}</TableCellLayout></TableCell>
                          <TableCell><TableCellLayout><span className={mv.quantity < 0 ? styles.low : undefined}>{mv.quantity > 0 ? `+${mv.quantity}` : mv.quantity}</span></TableCellLayout></TableCell>
                          <TableCell><TableCellLayout>{mv.lotNumber ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                          <TableCell><TableCellLayout>{mv.reason ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </div>
            </>
          )}
        </div>
      </FormDrawer>

      {/* Ajustement d'inventaire d'un lot */}
      <FormDrawer
        open={!!adjust}
        onOpenChange={(v) => { if (!v) setAdjust(null) }}
        title={t('pharmacy.stock.adjustTitle')}
        onSubmit={() => { if (adjustValid) adjustMut.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setAdjust(null)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={!adjustValid} icon={adjustMut.isPending ? <Spinner size="tiny" /> : undefined}>
            {t('actions.save')}
          </Button>
        </>}>
        <div className={styles.form}>
          <span className={styles.muted}>{t('pharmacy.stock.adjustHint', { lot: adjust?.lotNumber ?? '—', current: adjust?.quantityOnHand ?? 0 })}</span>
          <Field label={t('pharmacy.stock.newQuantity')} required>
            <Input type="number" min={0} value={adjustQty} onChange={(_, d) => setAdjustQty(d.value)} />
          </Field>
          <Field label={t('pharmacy.stock.reason')}>
            <Input value={adjustReason} onChange={(_, d) => setAdjustReason(d.value)} />
          </Field>
          {adjustMut.isError && <span className={styles.error}>{(adjustMut.error as Error).message}</span>}
        </div>
      </FormDrawer>
    </div>
  )
}
