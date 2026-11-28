import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Dropdown, Field, Input, Option, SearchBox, Spinner, Textarea, Title3,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Delete20Regular, Open20Regular, Pill20Regular, ProhibitedNote20Regular, Search20Regular } from '@fluentui/react-icons'
import { useActiveFacility } from '@features/auth'
import { FormDrawer, RowActions, type RowAction } from '@shared/components'
import { medicationKeys, getMedications, type Medication } from '../api'
import {
  PRESCRIPTION_STATUSES,
  prescriptionKeys, getPrescriptions, getPrescription, createPrescription, cancelPrescription, dispensePrescriptionLine,
  type Prescription, type PrescriptionFilter, type PrescriptionLine, type PrescriptionLineInput,
} from '../api'

const PAGE_SIZE = 10
const ANY = '__any__'

const statusColor = (s: string): 'success' | 'informative' | 'danger' | 'subtle' =>
  s === 'Completed' ? 'success' : s === 'Active' ? 'informative' : s === 'Cancelled' ? 'danger' : 'subtle'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  header: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM },
  subtitle: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  filters: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM, rowGap: tokens.spacingVerticalS, flexWrap: 'wrap' },
  filterField: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  label: { fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '520px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1, minWidth: 0 },
  lineRow: { display: 'flex', columnGap: tokens.spacingHorizontalS, alignItems: 'flex-end' },
  section: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalS, marginTop: tokens.spacingVerticalM },
  sectionTitle: { fontWeight: tokens.fontWeightSemibold, fontSize: tokens.fontSizeBase300 },
  detailActions: { display: 'flex', columnGap: tokens.spacingHorizontalS, alignItems: 'center', flexWrap: 'wrap' },
})

const fmtDate = (iso: string | null, locale: string) => (iso ? new Date(iso).toLocaleDateString(locale) : '—')

type LineDraft = { medicationId: string; dosage: string; quantityPrescribed: string }
const emptyLine: LineDraft = { medicationId: '', dosage: '', quantityPrescribed: '' }

export function PharmacyDispensingPage() {
  const styles = useStyles()
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const facilityId = useActiveFacility()

  const [draft, setDraft] = useState<{ search: string; status: string }>({ search: '', status: ANY })
  const [applied, setApplied] = useState<PrescriptionFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [createOpen, setCreateOpen] = useState(false)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [dispensing, setDispensing] = useState<PrescriptionLine | null>(null)
  const [dispenseQty, setDispenseQty] = useState('')

  const [hdr, setHdr] = useState({ patientName: '', patientReference: '', prescriberName: '', reference: '', prescribedOn: '', notes: '' })
  const [lines, setLines] = useState<LineDraft[]>([{ ...emptyLine }])
  const setHdrField = (k: keyof typeof hdr, v: string) => setHdr((s) => ({ ...s, [k]: v }))
  const setLine = (i: number, k: keyof LineDraft, v: string) => setLines((ls) => ls.map((l, j) => (j === i ? { ...l, [k]: v } : l)))

  const query = useQuery({
    queryKey: prescriptionKeys.list(facilityId ?? '', applied),
    queryFn: () => getPrescriptions(facilityId!, applied),
    enabled: !!facilityId,
  })
  const medsQuery = useQuery({
    queryKey: medicationKeys.list(facilityId ?? '', { status: 'Active', pageSize: 200 }),
    queryFn: () => getMedications(facilityId!, { status: 'Active', pageSize: 200 }),
    enabled: !!facilityId && createOpen,
  })
  const detailQuery = useQuery({
    queryKey: prescriptionKeys.detail(detailId ?? ''),
    queryFn: () => getPrescription(detailId!),
    enabled: !!detailId,
  })

  const meds: Medication[] = medsQuery.data?.items ?? []
  const medLabel = (id: string) => { const m = meds.find((x) => x.id === id); return m ? `${m.name} (${m.code})` : '' }

  const invalidate = () => { void queryClient.invalidateQueries({ queryKey: prescriptionKeys.all(facilityId ?? '') }) }
  const invalidateDetail = () => { void queryClient.invalidateQueries({ queryKey: prescriptionKeys.detail(detailId ?? '') }) }

  const createMut = useMutation({
    mutationFn: async () => {
      const payloadLines: PrescriptionLineInput[] = lines
        .filter((l) => l.medicationId && Number(l.quantityPrescribed) > 0)
        .map((l) => ({ medicationId: l.medicationId, dosage: l.dosage.trim() || null, quantityPrescribed: Number(l.quantityPrescribed) }))
      await createPrescription(facilityId!, {
        patientName: hdr.patientName.trim(), patientReference: hdr.patientReference.trim() || null,
        prescriberName: hdr.prescriberName.trim() || null, reference: hdr.reference.trim() || null,
        prescribedOn: hdr.prescribedOn || null, notes: hdr.notes.trim() || null, lines: payloadLines,
      })
    },
    onSuccess: () => { invalidate(); setCreateOpen(false); setHdr({ patientName: '', patientReference: '', prescriberName: '', reference: '', prescribedOn: '', notes: '' }); setLines([{ ...emptyLine }]) },
  })
  const cancelMut = useMutation({ mutationFn: (id: string) => cancelPrescription(id), onSuccess: () => { invalidate(); invalidateDetail() } })
  const dispenseMut = useMutation({
    mutationFn: async () => { await dispensePrescriptionLine(detailId!, dispensing!.id, Number(dispenseQty)) },
    onSuccess: () => { invalidate(); invalidateDetail(); setDispensing(null); setDispenseQty('') },
  })

  const runSearch = () => setApplied({ page: 1, pageSize: PAGE_SIZE, search: draft.search.trim() || undefined, status: draft.status === ANY ? undefined : draft.status })

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))

  const validLines = lines.filter((l) => l.medicationId && Number(l.quantityPrescribed) > 0)
  const canCreate = hdr.patientName.trim() !== '' && validLines.length > 0 && !createMut.isPending
  const dispenseValid = dispensing != null && Number(dispenseQty) > 0 && Number(dispenseQty) <= (dispensing.quantityPrescribed - dispensing.quantityDispensed) && !dispenseMut.isPending

  const openDispense = (l: PrescriptionLine) => { setDispensing(l); setDispenseQty(String(l.quantityPrescribed - l.quantityDispensed)) }

  const detail = detailQuery.data
  const canDispense = detail && detail.header.status === 'Active'

  if (!facilityId) return <div className={styles.root}><div className={styles.state}>{t('pharmacy.noFacility')}</div></div>

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>{t('pharmacy.dispensing.title')}</Title3>
          <div className={styles.subtitle}>{t('pharmacy.dispensing.subtitle')}</div>
        </div>
      </div>

      <div className={styles.header}>
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('pharmacy.filters.search')}</span>
            <SearchBox value={draft.search} placeholder={t('pharmacy.dispensing.searchPlaceholder')}
              onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
              onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('pharmacy.fields.status')}</span>
            <Dropdown value={draft.status === ANY ? t('pharmacy.filters.allStatuses') : t(`pharmacy.dispensing.statuses.${draft.status}`)}
              selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('pharmacy.filters.allStatuses')}</Option>
              {PRESCRIPTION_STATUSES.map((s) => <Option key={s} value={s}>{t(`pharmacy.dispensing.statuses.${s}`)}</Option>)}
            </Dropdown>
          </div>
          <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('pharmacy.filters.apply')}</Button>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => setCreateOpen(true)}>{t('pharmacy.dispensing.newPrescription')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('pharmacy.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('pharmacy.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('pharmacy.dispensing.empty')}</div>
      ) : (
        <Table aria-label={t('pharmacy.dispensing.title')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('pharmacy.dispensing.reference')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.dispensing.patient')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.dispensing.prescriber')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.dispensing.dispensed')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.dispensing.prescribedOn')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.actions')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((p: Prescription) => (
              <TableRow key={p.id}>
                <TableCell><TableCellLayout>{p.reference ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout description={p.patientReference ?? undefined}>{p.patientName}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{p.prescriberName ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><Badge appearance="tint" color={statusColor(p.status)}>{t(`pharmacy.dispensing.statuses.${p.status}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{p.totalDispensed} / {p.totalPrescribed}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{fmtDate(p.prescribedOn, i18n.language)}</TableCellLayout></TableCell>
                <TableCell>
                  <RowActions primary={[{ key: 'detail', label: t('pharmacy.stock.detail'), icon: <Open20Regular />, onClick: () => setDetailId(p.id) }]} />
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

      {/* Création d'une prescription */}
      <FormDrawer
        open={createOpen}
        onOpenChange={setCreateOpen}
        title={t('pharmacy.dispensing.newPrescription')}
        onSubmit={() => { if (canCreate) createMut.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setCreateOpen(false)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={!canCreate} icon={createMut.isPending ? <Spinner size="tiny" /> : <Add20Regular />}>{t('actions.create')}</Button>
        </>}>
        <div className={styles.form}>
          <div className={styles.row}>
            <Field className={styles.grow} label={t('pharmacy.dispensing.patient')} required>
              <Input value={hdr.patientName} onChange={(_, d) => setHdrField('patientName', d.value)} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.dispensing.patientReference')}>
              <Input value={hdr.patientReference} onChange={(_, d) => setHdrField('patientReference', d.value)} placeholder={t('pharmacy.dispensing.patientReferencePlaceholder')} />
            </Field>
          </div>
          <div className={styles.row}>
            <Field className={styles.grow} label={t('pharmacy.dispensing.prescriber')}>
              <Input value={hdr.prescriberName} onChange={(_, d) => setHdrField('prescriberName', d.value)} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.dispensing.reference')}>
              <Input value={hdr.reference} onChange={(_, d) => setHdrField('reference', d.value)} placeholder={t('pharmacy.dispensing.referencePlaceholder')} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.dispensing.prescribedOn')}>
              <Input type="date" value={hdr.prescribedOn} onChange={(_, d) => setHdrField('prescribedOn', d.value)} />
            </Field>
          </div>

          <div className={styles.section}>
            <span className={styles.sectionTitle}>{t('pharmacy.dispensing.lines')}</span>
            {lines.map((l, i) => (
              <div key={i} className={styles.lineRow}>
                <Field className={styles.grow} label={i === 0 ? t('pharmacy.stock.medication') : undefined}>
                  <Dropdown value={medLabel(l.medicationId)} selectedOptions={[l.medicationId]} placeholder={t('pharmacy.stock.medicationPlaceholder')}
                    onOptionSelect={(_, d) => setLine(i, 'medicationId', d.optionValue ?? '')}>
                    {meds.map((m) => <Option key={m.id} value={m.id}>{`${m.name} (${m.code})`}</Option>)}
                  </Dropdown>
                </Field>
                <Field className={styles.grow} label={i === 0 ? t('pharmacy.dispensing.dosage') : undefined}>
                  <Input value={l.dosage} onChange={(_, d) => setLine(i, 'dosage', d.value)} placeholder={t('pharmacy.dispensing.dosagePlaceholder')} />
                </Field>
                <Field label={i === 0 ? t('pharmacy.dispensing.qtyPrescribed') : undefined}>
                  <Input type="number" min={1} value={l.quantityPrescribed} onChange={(_, d) => setLine(i, 'quantityPrescribed', d.value)} style={{ width: '100px' }} />
                </Field>
                <Button appearance="subtle" icon={<Delete20Regular />} type="button" aria-label={t('actions.delete')}
                  disabled={lines.length === 1} onClick={() => setLines((ls) => ls.filter((_, j) => j !== i))} />
              </div>
            ))}
            <Button appearance="secondary" size="small" icon={<Add20Regular />} type="button" onClick={() => setLines((ls) => [...ls, { ...emptyLine }])}>
              {t('pharmacy.dispensing.addLine')}
            </Button>
          </div>

          <Field label={t('pharmacy.fields.notes')}>
            <Textarea value={hdr.notes} onChange={(_, d) => setHdrField('notes', d.value)} resize="vertical" />
          </Field>
          {createMut.isError && <span className={styles.error}>{(createMut.error as Error).message}</span>}
        </div>
      </FormDrawer>

      {/* Détail d'une prescription */}
      <FormDrawer
        open={!!detailId}
        onOpenChange={(v) => { if (!v) setDetailId(null) }}
        title={detail ? `${detail.header.patientName}${detail.header.reference ? ` — ${detail.header.reference}` : ''}` : ''}
        footer={<Button appearance="secondary" type="button" onClick={() => setDetailId(null)}>{t('actions.close')}</Button>}>
        <div className={styles.form}>
          {detailQuery.isLoading ? (
            <div className={styles.state}><Spinner size="small" label={t('pharmacy.loading')} /></div>
          ) : detailQuery.isError || !detail ? (
            <div className={mergeClasses(styles.state, styles.error)}>{t('pharmacy.loadError')}</div>
          ) : (
            <>
              <div className={styles.detailActions}>
                <Badge appearance="tint" color={statusColor(detail.header.status)}>{t(`pharmacy.dispensing.statuses.${detail.header.status}`)}</Badge>
                {detail.header.prescriberName && <span className={styles.muted}>{t('pharmacy.dispensing.prescriber')} : {detail.header.prescriberName}</span>}
                {detail.header.status === 'Active' && (
                  <Button appearance="secondary" size="small" icon={<ProhibitedNote20Regular />} disabled={cancelMut.isPending} onClick={() => cancelMut.mutate(detail.header.id)}>
                    {t('pharmacy.dispensing.cancel')}
                  </Button>
                )}
              </div>
              <span className={styles.muted}>{t('pharmacy.dispensing.dispenseStockHint')}</span>

              <Table size="small" aria-label={t('pharmacy.dispensing.lines')}>
                <TableHeader>
                  <TableRow>
                    <TableHeaderCell>{t('pharmacy.fields.name')}</TableHeaderCell>
                    <TableHeaderCell>{t('pharmacy.dispensing.dosage')}</TableHeaderCell>
                    <TableHeaderCell>{t('pharmacy.dispensing.dispensed')}</TableHeaderCell>
                    <TableHeaderCell>{t('pharmacy.fields.actions')}</TableHeaderCell>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detail.lines.map((l) => {
                    const actions: RowAction[] = canDispense && l.quantityDispensed < l.quantityPrescribed
                      ? [{ key: 'dispense', label: t('pharmacy.dispensing.dispense'), icon: <Pill20Regular />, onClick: () => openDispense(l) }]
                      : []
                    return (
                      <TableRow key={l.id}>
                        <TableCell><TableCellLayout description={l.medicationCode}>{l.medicationName}</TableCellLayout></TableCell>
                        <TableCell><TableCellLayout>{l.dosage ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                        <TableCell><TableCellLayout>{l.quantityDispensed} / {l.quantityPrescribed} {l.unit}</TableCellLayout></TableCell>
                        <TableCell>{actions.length > 0 ? <RowActions primary={actions} /> : <span className={styles.muted}>—</span>}</TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
              {cancelMut.isError && <span className={styles.error}>{(cancelMut.error as Error).message}</span>}
            </>
          )}
        </div>
      </FormDrawer>

      {/* Dispensation d'une ligne */}
      <FormDrawer
        open={!!dispensing}
        onOpenChange={(v) => { if (!v) setDispensing(null) }}
        title={t('pharmacy.dispensing.dispenseTitle')}
        onSubmit={() => { if (dispenseValid) dispenseMut.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setDispensing(null)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={!dispenseValid} icon={dispenseMut.isPending ? <Spinner size="tiny" /> : <Pill20Regular />}>
            {t('pharmacy.dispensing.dispense')}
          </Button>
        </>}>
        <div className={styles.form}>
          {dispensing && <span className={styles.muted}>{t('pharmacy.dispensing.dispenseHint', { name: dispensing.medicationName, outstanding: dispensing.quantityPrescribed - dispensing.quantityDispensed })}</span>}
          <Field label={t('pharmacy.stock.quantity')} required>
            <Input type="number" min={1} value={dispenseQty} onChange={(_, d) => setDispenseQty(d.value)} />
          </Field>
          <span className={styles.muted}>{t('pharmacy.dispensing.dispenseFefoHint')}</span>
          {dispenseMut.isError && <span className={styles.error}>{(dispenseMut.error as Error).message}</span>}
        </div>
      </FormDrawer>
    </div>
  )
}
