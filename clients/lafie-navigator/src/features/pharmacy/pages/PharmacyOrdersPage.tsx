import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Dropdown, Field, Input, Option, SearchBox, Spinner, Tab, TabList, Textarea, Title3,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import {
  Add20Regular, ArrowDownload20Regular, CheckmarkCircle20Regular, Delete20Regular, Edit20Regular,
  Open20Regular, ProhibitedNote20Regular, Search20Regular, Send20Regular,
} from '@fluentui/react-icons'
import { useActiveFacility } from '@features/auth'
import { FormDrawer, RowActions, type RowAction } from '@shared/components'
import { medicationKeys, getMedications, type Medication } from '../api'
import {
  PURCHASE_ORDER_STATUSES,
  supplierKeys, getSuppliers, createSupplier, updateSupplier, setSupplierStatus,
  purchaseOrderKeys, getPurchaseOrders, getPurchaseOrder, createPurchaseOrder,
  submitPurchaseOrder, cancelPurchaseOrder, receivePurchaseOrderLine,
  type Supplier, type SupplierFilter, type PurchaseOrder, type PurchaseOrderFilter,
  type PurchaseOrderLine, type PurchaseOrderLineInput,
} from '../api'

const PAGE_SIZE = 10
const ANY = '__any__'

const statusColor = (s: string): 'success' | 'warning' | 'informative' | 'danger' | 'subtle' =>
  s === 'Received' ? 'success' : s === 'Ordered' ? 'informative' : s === 'PartiallyReceived' ? 'warning' : s === 'Cancelled' ? 'danger' : 'subtle'

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
  detailActions: { display: 'flex', columnGap: tokens.spacingHorizontalS, flexWrap: 'wrap' },
})

const money = (n: number, locale: string) => n.toLocaleString(locale, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const fmtDate = (iso: string | null, locale: string) => (iso ? new Date(iso).toLocaleDateString(locale) : '—')

// ------------------------------------------------------------------ Fournisseurs

type SupplierForm = { code: string; name: string; contactName: string; email: string; phone: string; address: string; notes: string }
const emptySupplier: SupplierForm = { code: '', name: '', contactName: '', email: '', phone: '', address: '', notes: '' }

function SuppliersPanel({ facilityId }: { facilityId: string }) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [draft, setDraft] = useState<{ search: string; active: string }>({ search: '', active: ANY })
  const [applied, setApplied] = useState<SupplierFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<Supplier | null>(null)
  const [f, setF] = useState<SupplierForm>(emptySupplier)
  const set = <K extends keyof SupplierForm>(k: K, v: SupplierForm[K]) => setF((s) => ({ ...s, [k]: v }))

  const query = useQuery({
    queryKey: supplierKeys.list(facilityId, applied),
    queryFn: () => getSuppliers(facilityId, applied),
  })
  const invalidate = () => { void queryClient.invalidateQueries({ queryKey: supplierKeys.all(facilityId) }) }

  const mutation = useMutation({
    mutationFn: async () => {
      const base = {
        name: f.name.trim(), contactName: f.contactName.trim() || null, email: f.email.trim() || null,
        phone: f.phone.trim() || null, address: f.address.trim() || null, notes: f.notes.trim() || null,
      }
      if (editing) { await updateSupplier(editing.id, base); return }
      await createSupplier(facilityId, { ...base, code: f.code.trim() })
    },
    onSuccess: () => { invalidate(); setOpen(false); setF(emptySupplier); setEditing(null) },
  })
  const statusMut = useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) => setSupplierStatus(id, active),
    onSuccess: invalidate,
  })

  const openNew = () => { setEditing(null); setF(emptySupplier); setOpen(true) }
  const openEdit = (s: Supplier) => {
    setEditing(s)
    setF({ code: s.code, name: s.name, contactName: s.contactName ?? '', email: s.email ?? '', phone: s.phone ?? '', address: s.address ?? '', notes: s.notes ?? '' })
    setOpen(true)
  }
  const runSearch = () => setApplied({ page: 1, pageSize: PAGE_SIZE, search: draft.search.trim() || undefined, active: draft.active === ANY ? undefined : draft.active === 'true' })

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))

  const canSubmit = (editing ? true : f.code.trim() !== '') && f.name.trim() !== '' && !mutation.isPending

  return (
    <>
      <div className={styles.header}>
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('pharmacy.filters.search')}</span>
            <SearchBox value={draft.search} placeholder={t('pharmacy.purchasing.supplierSearchPlaceholder')}
              onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
              onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('pharmacy.purchasing.supplierStatus')}</span>
            <Dropdown value={draft.active === ANY ? t('pharmacy.filters.allStatuses') : t(`pharmacy.purchasing.active.${draft.active}`)}
              selectedOptions={[draft.active]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, active: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('pharmacy.filters.allStatuses')}</Option>
              <Option value="true">{t('pharmacy.purchasing.active.true')}</Option>
              <Option value="false">{t('pharmacy.purchasing.active.false')}</Option>
            </Dropdown>
          </div>
          <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('pharmacy.filters.apply')}</Button>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={openNew}>{t('pharmacy.purchasing.newSupplier')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('pharmacy.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('pharmacy.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('pharmacy.purchasing.noSuppliers')}</div>
      ) : (
        <Table aria-label={t('pharmacy.purchasing.suppliers')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('pharmacy.fields.code')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.name')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.purchasing.contact')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.purchasing.phone')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.actions')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((s) => (
              <TableRow key={s.id}>
                <TableCell><TableCellLayout>{s.code}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{s.name}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{s.contactName ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{s.phone ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><Badge appearance="tint" color={s.active ? 'success' : 'subtle'}>{t(`pharmacy.purchasing.active.${s.active}`)}</Badge></TableCell>
                <TableCell>
                  <RowActions
                    primary={[{ key: 'edit', label: t('actions.edit'), icon: <Edit20Regular />, onClick: () => openEdit(s) }]}
                    menu={[s.active
                      ? { key: 'off', label: t('pharmacy.purchasing.deactivate'), icon: <ProhibitedNote20Regular />, onClick: () => statusMut.mutate({ id: s.id, active: false }) }
                      : { key: 'on', label: t('pharmacy.purchasing.reactivate'), icon: <CheckmarkCircle20Regular />, onClick: () => statusMut.mutate({ id: s.id, active: true }) }]}
                  />
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

      <FormDrawer
        open={open}
        onOpenChange={(v) => { setOpen(v); if (!v) setEditing(null) }}
        title={t(editing ? 'pharmacy.purchasing.editSupplier' : 'pharmacy.purchasing.newSupplier')}
        onSubmit={() => { if (canSubmit) mutation.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setOpen(false)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={!canSubmit} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined}>
            {t(editing ? 'actions.save' : 'actions.create')}
          </Button>
        </>}>
        <div className={styles.form}>
          <div className={styles.row}>
            <Field className={styles.grow} label={t('pharmacy.fields.code')} required>
              <Input value={f.code} disabled={!!editing} onChange={(_, d) => set('code', d.value)} placeholder={t('pharmacy.purchasing.supplierCodePlaceholder')} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.fields.name')} required>
              <Input value={f.name} onChange={(_, d) => set('name', d.value)} />
            </Field>
          </div>
          <div className={styles.row}>
            <Field className={styles.grow} label={t('pharmacy.purchasing.contact')}>
              <Input value={f.contactName} onChange={(_, d) => set('contactName', d.value)} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.purchasing.phone')}>
              <Input value={f.phone} onChange={(_, d) => set('phone', d.value)} />
            </Field>
          </div>
          <Field label={t('pharmacy.purchasing.email')}>
            <Input type="email" value={f.email} onChange={(_, d) => set('email', d.value)} />
          </Field>
          <Field label={t('pharmacy.purchasing.address')}>
            <Input value={f.address} onChange={(_, d) => set('address', d.value)} />
          </Field>
          <Field label={t('pharmacy.fields.notes')}>
            <Textarea value={f.notes} onChange={(_, d) => set('notes', d.value)} resize="vertical" />
          </Field>
          {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
        </div>
      </FormDrawer>
    </>
  )
}

// ------------------------------------------------------------------ Bons de commande

type LineDraft = { medicationId: string; quantityOrdered: string; unitPrice: string }
const emptyLine: LineDraft = { medicationId: '', quantityOrdered: '', unitPrice: '' }

function OrdersPanel({ facilityId }: { facilityId: string }) {
  const styles = useStyles()
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()

  const [draft, setDraft] = useState<{ search: string; status: string }>({ search: '', status: ANY })
  const [applied, setApplied] = useState<PurchaseOrderFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [createOpen, setCreateOpen] = useState(false)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [receiving, setReceiving] = useState<PurchaseOrderLine | null>(null)

  const [hdr, setHdr] = useState({ supplierId: '', reference: '', orderDate: '', expectedOn: '', notes: '' })
  const [lines, setLines] = useState<LineDraft[]>([{ ...emptyLine }])
  const setHdrField = (k: keyof typeof hdr, v: string) => setHdr((s) => ({ ...s, [k]: v }))
  const setLine = (i: number, k: keyof LineDraft, v: string) => setLines((ls) => ls.map((l, j) => (j === i ? { ...l, [k]: v } : l)))

  const [rf, setRf] = useState({ quantity: '', lotNumber: '', expiryOn: '', receivedOn: '' })
  const setRfField = (k: keyof typeof rf, v: string) => setRf((s) => ({ ...s, [k]: v }))

  const query = useQuery({
    queryKey: purchaseOrderKeys.list(facilityId, applied),
    queryFn: () => getPurchaseOrders(facilityId, applied),
  })
  const suppliersQuery = useQuery({
    queryKey: supplierKeys.list(facilityId, { active: true, pageSize: 200 }),
    queryFn: () => getSuppliers(facilityId, { active: true, pageSize: 200 }),
    enabled: createOpen,
  })
  const medsQuery = useQuery({
    queryKey: medicationKeys.list(facilityId, { status: 'Active', pageSize: 200 }),
    queryFn: () => getMedications(facilityId, { status: 'Active', pageSize: 200 }),
    enabled: createOpen,
  })
  const detailQuery = useQuery({
    queryKey: purchaseOrderKeys.detail(detailId ?? ''),
    queryFn: () => getPurchaseOrder(detailId!),
    enabled: !!detailId,
  })

  const suppliers: Supplier[] = suppliersQuery.data?.items ?? []
  const meds: Medication[] = medsQuery.data?.items ?? []
  const supplierLabel = (id: string) => { const s = suppliers.find((x) => x.id === id); return s ? `${s.name} (${s.code})` : '' }
  const medLabel = (id: string) => { const m = meds.find((x) => x.id === id); return m ? `${m.name} (${m.code})` : '' }

  const invalidate = () => { void queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.all(facilityId) }) }
  const invalidateDetail = () => { void queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.detail(detailId ?? '') }) }

  const createMut = useMutation({
    mutationFn: async () => {
      const payloadLines: PurchaseOrderLineInput[] = lines
        .filter((l) => l.medicationId && Number(l.quantityOrdered) > 0)
        .map((l) => ({ medicationId: l.medicationId, quantityOrdered: Number(l.quantityOrdered), unitPrice: l.unitPrice.trim() ? Number(l.unitPrice) : null }))
      await createPurchaseOrder(facilityId, {
        supplierId: hdr.supplierId, reference: hdr.reference.trim(),
        orderDate: hdr.orderDate || null, expectedOn: hdr.expectedOn || null, notes: hdr.notes.trim() || null,
        lines: payloadLines,
      })
    },
    onSuccess: () => { invalidate(); setCreateOpen(false); setHdr({ supplierId: '', reference: '', orderDate: '', expectedOn: '', notes: '' }); setLines([{ ...emptyLine }]) },
  })
  const submitMut = useMutation({ mutationFn: (id: string) => submitPurchaseOrder(id), onSuccess: () => { invalidate(); invalidateDetail() } })
  const cancelMut = useMutation({ mutationFn: (id: string) => cancelPurchaseOrder(id), onSuccess: () => { invalidate(); invalidateDetail() } })
  const receiveMut = useMutation({
    mutationFn: async () => {
      await receivePurchaseOrderLine(detailId!, receiving!.id, {
        quantity: Number(rf.quantity), lotNumber: rf.lotNumber.trim() || null,
        expiryOn: rf.expiryOn || null, receivedOn: rf.receivedOn || null,
      })
    },
    onSuccess: () => { invalidate(); invalidateDetail(); setReceiving(null); setRf({ quantity: '', lotNumber: '', expiryOn: '', receivedOn: '' }) },
  })

  const runSearch = () => setApplied({ page: 1, pageSize: PAGE_SIZE, search: draft.search.trim() || undefined, status: draft.status === ANY ? undefined : draft.status })

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))

  const validLines = lines.filter((l) => l.medicationId && Number(l.quantityOrdered) > 0)
  const canCreate = hdr.supplierId !== '' && hdr.reference.trim() !== '' && validLines.length > 0 && !createMut.isPending
  const receiveValid = receiving != null && Number(rf.quantity) > 0 && Number(rf.quantity) <= (receiving.quantityOrdered - receiving.quantityReceived) && !receiveMut.isPending

  const openReceive = (l: PurchaseOrderLine) => { setReceiving(l); setRf({ quantity: String(l.quantityOrdered - l.quantityReceived), lotNumber: '', expiryOn: '', receivedOn: '' }) }

  const detail = detailQuery.data
  const canReceiveOrder = detail && (detail.header.status === 'Ordered' || detail.header.status === 'PartiallyReceived')

  return (
    <>
      <div className={styles.header}>
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('pharmacy.filters.search')}</span>
            <SearchBox value={draft.search} placeholder={t('pharmacy.purchasing.orderSearchPlaceholder')}
              onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
              onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('pharmacy.fields.status')}</span>
            <Dropdown value={draft.status === ANY ? t('pharmacy.filters.allStatuses') : t(`pharmacy.purchasing.statuses.${draft.status}`)}
              selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('pharmacy.filters.allStatuses')}</Option>
              {PURCHASE_ORDER_STATUSES.map((s) => <Option key={s} value={s}>{t(`pharmacy.purchasing.statuses.${s}`)}</Option>)}
            </Dropdown>
          </div>
          <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('pharmacy.filters.apply')}</Button>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => setCreateOpen(true)}>{t('pharmacy.purchasing.newOrder')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('pharmacy.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('pharmacy.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('pharmacy.purchasing.noOrders')}</div>
      ) : (
        <Table aria-label={t('pharmacy.purchasing.orders')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('pharmacy.purchasing.reference')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.purchasing.supplier')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.purchasing.received')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.purchasing.amount')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.purchasing.expectedOn')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.actions')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((o: PurchaseOrder) => (
              <TableRow key={o.id}>
                <TableCell><TableCellLayout>{o.reference}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{o.supplierName}</TableCellLayout></TableCell>
                <TableCell><Badge appearance="tint" color={statusColor(o.status)}>{t(`pharmacy.purchasing.statuses.${o.status}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{o.totalReceived} / {o.totalOrdered}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{money(o.totalAmount, i18n.language)}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{fmtDate(o.expectedOn, i18n.language)}</TableCellLayout></TableCell>
                <TableCell>
                  <RowActions primary={[{ key: 'detail', label: t('pharmacy.stock.detail'), icon: <Open20Regular />, onClick: () => setDetailId(o.id) }]} />
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

      {/* Création d'un bon de commande */}
      <FormDrawer
        open={createOpen}
        onOpenChange={setCreateOpen}
        title={t('pharmacy.purchasing.newOrder')}
        onSubmit={() => { if (canCreate) createMut.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setCreateOpen(false)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={!canCreate} icon={createMut.isPending ? <Spinner size="tiny" /> : <Add20Regular />}>{t('actions.create')}</Button>
        </>}>
        <div className={styles.form}>
          <Field label={t('pharmacy.purchasing.supplier')} required>
            <Dropdown value={supplierLabel(hdr.supplierId)} selectedOptions={[hdr.supplierId]} placeholder={t('pharmacy.purchasing.supplierPlaceholder')}
              onOptionSelect={(_, d) => setHdrField('supplierId', d.optionValue ?? '')}>
              {suppliers.map((s) => <Option key={s.id} value={s.id}>{`${s.name} (${s.code})`}</Option>)}
            </Dropdown>
          </Field>
          <div className={styles.row}>
            <Field className={styles.grow} label={t('pharmacy.purchasing.reference')} required>
              <Input value={hdr.reference} onChange={(_, d) => setHdrField('reference', d.value)} placeholder={t('pharmacy.purchasing.referencePlaceholder')} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.purchasing.orderDate')}>
              <Input type="date" value={hdr.orderDate} onChange={(_, d) => setHdrField('orderDate', d.value)} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.purchasing.expectedOn')}>
              <Input type="date" value={hdr.expectedOn} onChange={(_, d) => setHdrField('expectedOn', d.value)} />
            </Field>
          </div>

          <div className={styles.section}>
            <span className={styles.sectionTitle}>{t('pharmacy.purchasing.lines')}</span>
            {lines.map((l, i) => (
              <div key={i} className={styles.lineRow}>
                <Field className={styles.grow} label={i === 0 ? t('pharmacy.stock.medication') : undefined}>
                  <Dropdown value={medLabel(l.medicationId)} selectedOptions={[l.medicationId]} placeholder={t('pharmacy.stock.medicationPlaceholder')}
                    onOptionSelect={(_, d) => setLine(i, 'medicationId', d.optionValue ?? '')}>
                    {meds.map((m) => <Option key={m.id} value={m.id}>{`${m.name} (${m.code})`}</Option>)}
                  </Dropdown>
                </Field>
                <Field label={i === 0 ? t('pharmacy.purchasing.qtyOrdered') : undefined}>
                  <Input type="number" min={1} value={l.quantityOrdered} onChange={(_, d) => setLine(i, 'quantityOrdered', d.value)} style={{ width: '110px' }} />
                </Field>
                <Field label={i === 0 ? t('pharmacy.purchasing.unitPrice') : undefined}>
                  <Input type="number" min={0} step="0.01" value={l.unitPrice} onChange={(_, d) => setLine(i, 'unitPrice', d.value)} style={{ width: '110px' }} />
                </Field>
                <Button appearance="subtle" icon={<Delete20Regular />} type="button" aria-label={t('actions.delete')}
                  disabled={lines.length === 1} onClick={() => setLines((ls) => ls.filter((_, j) => j !== i))} />
              </div>
            ))}
            <Button appearance="secondary" size="small" icon={<Add20Regular />} type="button" onClick={() => setLines((ls) => [...ls, { ...emptyLine }])}>
              {t('pharmacy.purchasing.addLine')}
            </Button>
          </div>

          <Field label={t('pharmacy.fields.notes')}>
            <Textarea value={hdr.notes} onChange={(_, d) => setHdrField('notes', d.value)} resize="vertical" />
          </Field>
          {createMut.isError && <span className={styles.error}>{(createMut.error as Error).message}</span>}
        </div>
      </FormDrawer>

      {/* Détail d'un bon de commande */}
      <FormDrawer
        open={!!detailId}
        onOpenChange={(v) => { if (!v) setDetailId(null) }}
        title={detail ? `${detail.header.reference} — ${detail.header.supplierName}` : ''}
        footer={<Button appearance="secondary" type="button" onClick={() => setDetailId(null)}>{t('actions.close')}</Button>}>
        <div className={styles.form}>
          {detailQuery.isLoading ? (
            <div className={styles.state}><Spinner size="small" label={t('pharmacy.loading')} /></div>
          ) : detailQuery.isError || !detail ? (
            <div className={mergeClasses(styles.state, styles.error)}>{t('pharmacy.loadError')}</div>
          ) : (
            <>
              <div className={styles.detailActions}>
                <Badge appearance="tint" color={statusColor(detail.header.status)}>{t(`pharmacy.purchasing.statuses.${detail.header.status}`)}</Badge>
                <span className={styles.muted}>{t('pharmacy.purchasing.amount')} : {money(detail.header.totalAmount, i18n.language)}</span>
                {detail.header.status === 'Draft' && (
                  <Button appearance="primary" size="small" icon={<Send20Regular />} disabled={submitMut.isPending} onClick={() => submitMut.mutate(detail.header.id)}>
                    {t('pharmacy.purchasing.submit')}
                  </Button>
                )}
                {detail.header.status !== 'Received' && detail.header.status !== 'Cancelled' && (
                  <Button appearance="secondary" size="small" icon={<ProhibitedNote20Regular />} disabled={cancelMut.isPending} onClick={() => cancelMut.mutate(detail.header.id)}>
                    {t('pharmacy.purchasing.cancelOrder')}
                  </Button>
                )}
              </div>

              <Table size="small" aria-label={t('pharmacy.purchasing.lines')}>
                <TableHeader>
                  <TableRow>
                    <TableHeaderCell>{t('pharmacy.fields.name')}</TableHeaderCell>
                    <TableHeaderCell>{t('pharmacy.purchasing.qtyOrdered')}</TableHeaderCell>
                    <TableHeaderCell>{t('pharmacy.purchasing.received')}</TableHeaderCell>
                    <TableHeaderCell>{t('pharmacy.purchasing.unitPrice')}</TableHeaderCell>
                    <TableHeaderCell>{t('pharmacy.fields.actions')}</TableHeaderCell>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detail.lines.map((l) => {
                    const actions: RowAction[] = canReceiveOrder && l.quantityReceived < l.quantityOrdered
                      ? [{ key: 'receive', label: t('pharmacy.purchasing.receive'), icon: <ArrowDownload20Regular />, onClick: () => openReceive(l) }]
                      : []
                    return (
                      <TableRow key={l.id}>
                        <TableCell><TableCellLayout description={l.medicationCode}>{l.medicationName}</TableCellLayout></TableCell>
                        <TableCell><TableCellLayout>{l.quantityOrdered} {l.unit}</TableCellLayout></TableCell>
                        <TableCell><TableCellLayout>{l.quantityReceived} / {l.quantityOrdered}</TableCellLayout></TableCell>
                        <TableCell><TableCellLayout>{l.unitPrice != null ? money(l.unitPrice, i18n.language) : <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                        <TableCell>{actions.length > 0 ? <RowActions primary={actions} /> : <span className={styles.muted}>—</span>}</TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
              {(submitMut.isError || cancelMut.isError) && <span className={styles.error}>{((submitMut.error ?? cancelMut.error) as Error).message}</span>}
            </>
          )}
        </div>
      </FormDrawer>

      {/* Réception d'une ligne */}
      <FormDrawer
        open={!!receiving}
        onOpenChange={(v) => { if (!v) setReceiving(null) }}
        title={t('pharmacy.purchasing.receiveTitle')}
        onSubmit={() => { if (receiveValid) receiveMut.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setReceiving(null)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={!receiveValid} icon={receiveMut.isPending ? <Spinner size="tiny" /> : <ArrowDownload20Regular />}>
            {t('pharmacy.purchasing.receive')}
          </Button>
        </>}>
        <div className={styles.form}>
          {receiving && <span className={styles.muted}>{t('pharmacy.purchasing.receiveHint', { name: receiving.medicationName, outstanding: receiving.quantityOrdered - receiving.quantityReceived })}</span>}
          <div className={styles.row}>
            <Field className={styles.grow} label={t('pharmacy.stock.quantity')} required>
              <Input type="number" min={1} value={rf.quantity} onChange={(_, d) => setRfField('quantity', d.value)} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.stock.lotNumber')}>
              <Input value={rf.lotNumber} onChange={(_, d) => setRfField('lotNumber', d.value)} />
            </Field>
          </div>
          <div className={styles.row}>
            <Field className={styles.grow} label={t('pharmacy.stock.expiryOn')}>
              <Input type="date" value={rf.expiryOn} onChange={(_, d) => setRfField('expiryOn', d.value)} />
            </Field>
            <Field className={styles.grow} label={t('pharmacy.stock.receivedOn')}>
              <Input type="date" value={rf.receivedOn} onChange={(_, d) => setRfField('receivedOn', d.value)} />
            </Field>
          </div>
          <span className={styles.muted}>{t('pharmacy.purchasing.receiveStockHint')}</span>
          {receiveMut.isError && <span className={styles.error}>{(receiveMut.error as Error).message}</span>}
        </div>
      </FormDrawer>
    </>
  )
}

// ------------------------------------------------------------------ Page

export function PharmacyOrdersPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const facilityId = useActiveFacility()
  const [tab, setTab] = useState<'orders' | 'suppliers'>('orders')

  if (!facilityId) return <div className={styles.root}><div className={styles.state}>{t('pharmacy.noFacility')}</div></div>

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>{t('pharmacy.purchasing.title')}</Title3>
          <div className={styles.subtitle}>{t('pharmacy.purchasing.subtitle')}</div>
        </div>
      </div>

      <TabList selectedValue={tab} onTabSelect={(_, d) => setTab(d.value as 'orders' | 'suppliers')}>
        <Tab value="orders">{t('pharmacy.purchasing.ordersTab')}</Tab>
        <Tab value="suppliers">{t('pharmacy.purchasing.suppliersTab')}</Tab>
      </TabList>

      {tab === 'orders' ? <OrdersPanel facilityId={facilityId} /> : <SuppliersPanel facilityId={facilityId} />}
    </div>
  )
}
