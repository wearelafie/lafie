import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import {
  Badge, Button, Checkbox, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Input, Option, SearchBox, Spinner, Tab, TabList, Textarea, Title3,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import { Add20Regular, DismissCircle20Regular, Edit20Regular, Search20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { useCurrentUser } from '@features/auth'
import { getSpacesByFacility } from '@features/space/api'
import {
  WASTE_STREAMS, COLLECTION_STATUSES, SLIP_STATUSES, WASTE_TREATMENTS,
  collectionKeys, slipKeys, cancelWasteCollection, createWasteCollection, updateWasteCollection,
  createWasteSlip, getWasteCollections, getWasteSlips,
  type WasteCollection, type WasteCollectionFilter, type WasteCollectionInput, type WasteStream,
  type WasteSlip, type WasteSlipFilter, type CreateWasteSlipInput, type WasteTreatment,
} from '../api'

const PAGE_SIZE = 10
const ANY = '__any__'
const NONE = '__none__'

const streamColor = (s: string): 'danger' | 'warning' | 'informative' | 'subtle' =>
  s === 'Dasri' || s === 'Sharps' || s === 'Anatomical' ? 'danger'
    : s === 'Cytotoxic' || s === 'Chemical' || s === 'Pharmaceutical' ? 'warning'
      : 'subtle'
const collStatusColor = (s: string): 'informative' | 'warning' | 'success' | 'subtle' =>
  s === 'Collected' ? 'informative' : s === 'Assigned' ? 'warning' : s === 'Eliminated' ? 'success' : 'subtle'
const slipStatusColor = (s: string): 'informative' | 'warning' | 'success' | 'subtle' =>
  s === 'Draft' ? 'informative' : s === 'Shipped' ? 'warning' : s === 'Treated' ? 'success' : 'subtle'

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
  rowLink: { cursor: 'pointer' },
  rowActions: { display: 'flex', columnGap: tokens.spacingHorizontalXS },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '480px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1, minWidth: 0 },
})

export function WastePage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { me } = useCurrentUser()
  const storeFacilityId = useAppStore((s) => s.selectedFacilityId)
  const setSelectedSlip = useAppStore((s) => s.setSelectedWasteSlip)
  const facilityId = me?.activeEstablishment?.facilityId ?? storeFacilityId

  const [tab, setTab] = useState<'collections' | 'slips'>('collections')

  if (!facilityId) return <div className={styles.root}><div className={styles.state}>{t('waste.noFacility')}</div></div>

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>{t('waste.title')}</Title3>
          <div className={styles.subtitle}>{t('waste.subtitle')}</div>
        </div>
      </div>

      <TabList selectedValue={tab} onTabSelect={(_, d) => setTab(d.value as 'collections' | 'slips')}>
        <Tab value="collections">{t('waste.tabs.collections')}</Tab>
        <Tab value="slips">{t('waste.tabs.slips')}</Tab>
      </TabList>

      {tab === 'collections' ? (
        <CollectionsTab
          facilityId={facilityId}
          defaultCollector={me?.displayName ?? me?.username ?? ''}
          invalidate={() => queryClient.invalidateQueries({ queryKey: collectionKeys.all(facilityId) })}
        />
      ) : (
        <SlipsTab
          facilityId={facilityId}
          onOpen={(s) => { setSelectedSlip(s.id); void navigate({ to: '/admin/waste-slip' as never }) }}
          invalidate={() => queryClient.invalidateQueries({ queryKey: slipKeys.all(facilityId) })}
        />
      )}
    </div>
  )
}

type CollForm = { id: string | null; stream: WasteStream; weightKg: string; containerCount: string; spaceId: string; collectedOn: string; collectedBy: string; notes: string }
const emptyColl = (collector: string): CollForm => ({ id: null, stream: 'Dasri', weightKg: '', containerCount: '1', spaceId: '', collectedOn: '', collectedBy: collector, notes: '' })

function CollectionsTab({ facilityId, defaultCollector, invalidate }: { facilityId: string; defaultCollector: string; invalidate: () => void }) {
  const styles = useStyles()
  const { t } = useTranslation()

  const [draft, setDraft] = useState<{ search: string; stream: string; status: string; unassignedOnly: boolean }>({ search: '', stream: ANY, status: ANY, unassignedOnly: false })
  const [applied, setApplied] = useState<WasteCollectionFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [open, setOpen] = useState(false)
  const [f, setF] = useState<CollForm>(emptyColl(defaultCollector))
  const set = <K extends keyof CollForm>(k: K, v: CollForm[K]) => setF((s) => ({ ...s, [k]: v }))

  const query = useQuery({ queryKey: collectionKeys.list(facilityId, applied), queryFn: () => getWasteCollections(facilityId, applied) })

  const spaces = useQuery({
    queryKey: ['spaces', facilityId, 'all-targets'],
    queryFn: () => getSpacesByFacility(facilityId),
    enabled: open,
  })
  const spaceOptions = (spaces.data?.items ?? []).map((s) => ({ id: s.id, label: `${s.code} · ${s.name}` }))
  const spaceLabel = f.spaceId === '' ? '' : (spaceOptions.find((o) => o.id === f.spaceId)?.label ?? '')

  const mutation = useMutation({
    mutationFn: () => {
      const input: WasteCollectionInput = {
        stream: f.stream, weightKg: Number.parseFloat(f.weightKg) || 0, containerCount: Number.parseInt(f.containerCount, 10) || 0,
        spaceId: f.spaceId || null, collectedOn: f.collectedOn || null, collectedBy: f.collectedBy.trim() || null, notes: f.notes.trim() || null,
      }
      return f.id ? updateWasteCollection(f.id, input) : createWasteCollection(facilityId, input)
    },
    onSuccess: () => { invalidate(); setOpen(false); setF(emptyColl(defaultCollector)) },
  })
  const cancelMut = useMutation({ mutationFn: (id: string) => cancelWasteCollection(id), onSuccess: invalidate })

  const runSearch = () => setApplied({
    page: 1, pageSize: PAGE_SIZE,
    search: draft.search.trim() || undefined,
    stream: draft.stream === ANY ? undefined : draft.stream,
    status: draft.status === ANY ? undefined : draft.status,
    unassignedOnly: draft.unassignedOnly || undefined,
  })

  const openNew = () => { setF(emptyColl(defaultCollector)); setOpen(true) }
  const openEdit = (c: WasteCollection) => {
    setF({ id: c.id, stream: c.stream, weightKg: String(c.weightKg), containerCount: String(c.containerCount),
      spaceId: c.spaceId ?? '', collectedOn: c.collectedOn ? c.collectedOn.slice(0, 10) : '', collectedBy: c.collectedBy ?? '', notes: c.notes ?? '' })
    setOpen(true)
  }

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))
  const canSubmit = Number.parseFloat(f.weightKg) >= 0 && f.weightKg !== '' && !mutation.isPending

  return (
    <>
      <div className={styles.header}>
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('waste.filters.search')}</span>
            <SearchBox value={draft.search} placeholder={t('waste.filters.collectionSearchPlaceholder')}
              onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
              onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('waste.fields.stream')}</span>
            <Dropdown value={draft.stream === ANY ? t('waste.filters.allStreams') : t(`waste.streams.${draft.stream}`)}
              selectedOptions={[draft.stream]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, stream: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('waste.filters.allStreams')}</Option>
              {WASTE_STREAMS.map((s) => <Option key={s} value={s}>{t(`waste.streams.${s}`)}</Option>)}
            </Dropdown>
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('waste.fields.status')}</span>
            <Dropdown value={draft.status === ANY ? t('waste.filters.allStatuses') : t(`waste.collectionStatuses.${draft.status}`)}
              selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('waste.filters.allStatuses')}</Option>
              {COLLECTION_STATUSES.map((s) => <Option key={s} value={s}>{t(`waste.collectionStatuses.${s}`)}</Option>)}
            </Dropdown>
          </div>
          <Checkbox label={t('waste.filters.unassignedOnly')} checked={draft.unassignedOnly} onChange={(_, d) => setDraft((s) => ({ ...s, unassignedOnly: !!d.checked }))} />
          <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('waste.filters.apply')}</Button>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={openNew}>{t('waste.collections.create')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('waste.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('waste.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('waste.collections.empty')}</div>
      ) : (
        <Table aria-label={t('waste.tabs.collections')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('waste.fields.stream')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.weight')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.containers')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.source')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.collectedOn')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.actions')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((c) => (
              <TableRow key={c.id}>
                <TableCell><Badge appearance="tint" color={streamColor(c.stream)}>{t(`waste.streams.${c.stream}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{c.weightKg} kg</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{c.containerCount}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{c.spaceName ?? c.spaceCode ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><Badge appearance="tint" color={collStatusColor(c.status)}>{t(`waste.collectionStatuses.${c.status}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{c.collectedOn ? new Date(c.collectedOn).toLocaleDateString() : <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell>
                  <div className={styles.rowActions}>
                    {c.status === 'Collected' && <Button size="small" appearance="subtle" icon={<Edit20Regular />} aria-label={t('actions.edit')} onClick={() => openEdit(c)} />}
                    {(c.status === 'Collected' || c.status === 'Assigned') && <Button size="small" appearance="subtle" icon={<DismissCircle20Regular />} aria-label={t('actions.cancel')} disabled={cancelMut.isPending} onClick={() => cancelMut.mutate(c.id)} />}
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
      {cancelMut.isError && <span className={styles.error}>{(cancelMut.error as Error).message}</span>}

      <div className={styles.pager}>
        <span className={styles.muted}>{t('waste.pager', { page, totalPages, total })}</span>
        <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => go(page - 1)}>{t('waste.prev')}</Button>
        <Button size="small" appearance="subtle" disabled={page >= totalPages} onClick={() => go(page + 1)}>{t('waste.next')}</Button>
      </div>

      <Dialog open={open} onOpenChange={(_, d) => setOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{f.id ? t('waste.collections.edit') : t('waste.collections.create')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('waste.fields.stream')} required>
                    <Dropdown value={t(`waste.streams.${f.stream}`)} selectedOptions={[f.stream]}
                      onOptionSelect={(_, d) => set('stream', (d.optionValue as WasteStream) ?? 'Dasri')}>
                      {WASTE_STREAMS.map((s) => <Option key={s} value={s}>{t(`waste.streams.${s}`)}</Option>)}
                    </Dropdown>
                  </Field>
                  <Field className={styles.grow} label={t('waste.fields.weight')} required>
                    <Input type="number" min={0} step="0.001" value={f.weightKg} onChange={(_, d) => set('weightKg', d.value)} contentAfter="kg" />
                  </Field>
                  <Field className={styles.grow} label={t('waste.fields.containers')}>
                    <Input type="number" min={0} value={f.containerCount} onChange={(_, d) => set('containerCount', d.value)} />
                  </Field>
                </div>
                <Field label={t('waste.fields.source')}>
                  <Dropdown value={spaceLabel} selectedOptions={f.spaceId ? [f.spaceId] : [NONE]} placeholder={t('waste.form.selectSpace')}
                    onOptionSelect={(_, d) => set('spaceId', d.optionValue === NONE ? '' : (d.optionValue ?? ''))}>
                    <Option value={NONE}>{t('waste.form.noSpace')}</Option>
                    {spaces.isLoading
                      ? <Option value="" disabled>{t('waste.loading')}</Option>
                      : spaceOptions.map((o) => <Option key={o.id} value={o.id}>{o.label}</Option>)}
                  </Dropdown>
                </Field>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('waste.fields.collectedOn')}>
                    <Input type="date" value={f.collectedOn} onChange={(_, d) => set('collectedOn', d.value)} />
                  </Field>
                  <Field className={styles.grow} label={t('waste.fields.collectedBy')}>
                    <Input value={f.collectedBy} onChange={(_, d) => set('collectedBy', d.value)} />
                  </Field>
                </div>
                <Field label={t('waste.fields.notes')}>
                  <Textarea value={f.notes} onChange={(_, d) => set('notes', d.value)} resize="vertical" />
                </Field>
                {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={!canSubmit} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined} onClick={() => mutation.mutate()}>
                {f.id ? t('actions.save') : t('actions.create')}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </>
  )
}

type SlipForm = { reference: string; stream: WasteStream; carrier: string; destination: string; treatment: string }
const emptySlip: SlipForm = { reference: '', stream: 'Dasri', carrier: '', destination: '', treatment: NONE }

function SlipsTab({ facilityId, onOpen, invalidate }: { facilityId: string; onOpen: (s: WasteSlip) => void; invalidate: () => void }) {
  const styles = useStyles()
  const { t } = useTranslation()

  const [draft, setDraft] = useState<{ search: string; stream: string; status: string; openOnly: boolean }>({ search: '', stream: ANY, status: ANY, openOnly: false })
  const [applied, setApplied] = useState<WasteSlipFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [open, setOpen] = useState(false)
  const [f, setF] = useState<SlipForm>(emptySlip)
  const set = <K extends keyof SlipForm>(k: K, v: SlipForm[K]) => setF((s) => ({ ...s, [k]: v }))

  const query = useQuery({ queryKey: slipKeys.list(facilityId, applied), queryFn: () => getWasteSlips(facilityId, applied) })

  const mutation = useMutation({
    mutationFn: () => {
      const input: CreateWasteSlipInput = {
        reference: f.reference.trim(), stream: f.stream, carrier: f.carrier.trim() || null,
        destination: f.destination.trim() || null, treatment: f.treatment === NONE ? null : (f.treatment as WasteTreatment),
      }
      return createWasteSlip(facilityId, input)
    },
    onSuccess: () => { invalidate(); setOpen(false); setF(emptySlip) },
  })

  const runSearch = () => setApplied({
    page: 1, pageSize: PAGE_SIZE,
    search: draft.search.trim() || undefined,
    stream: draft.stream === ANY ? undefined : draft.stream,
    status: draft.status === ANY ? undefined : draft.status,
    openOnly: draft.openOnly || undefined,
  })

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))
  const canSubmit = f.reference.trim() !== '' && !mutation.isPending

  return (
    <>
      <div className={styles.header}>
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('waste.filters.search')}</span>
            <SearchBox value={draft.search} placeholder={t('waste.filters.slipSearchPlaceholder')}
              onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
              onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('waste.fields.stream')}</span>
            <Dropdown value={draft.stream === ANY ? t('waste.filters.allStreams') : t(`waste.streams.${draft.stream}`)}
              selectedOptions={[draft.stream]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, stream: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('waste.filters.allStreams')}</Option>
              {WASTE_STREAMS.map((s) => <Option key={s} value={s}>{t(`waste.streams.${s}`)}</Option>)}
            </Dropdown>
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('waste.fields.status')}</span>
            <Dropdown value={draft.status === ANY ? t('waste.filters.allStatuses') : t(`waste.slipStatuses.${draft.status}`)}
              selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('waste.filters.allStatuses')}</Option>
              {SLIP_STATUSES.map((s) => <Option key={s} value={s}>{t(`waste.slipStatuses.${s}`)}</Option>)}
            </Dropdown>
          </div>
          <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('waste.filters.apply')}</Button>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setF(emptySlip); setOpen(true) }}>{t('waste.slips.create')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('waste.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('waste.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('waste.slips.empty')}</div>
      ) : (
        <Table aria-label={t('waste.tabs.slips')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('waste.fields.reference')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.stream')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.carrier')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.collections')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.weight')}</TableHeaderCell>
              <TableHeaderCell>{t('waste.fields.status')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((s) => (
              <TableRow key={s.id} className={styles.rowLink} onClick={() => onOpen(s)}>
                <TableCell><TableCellLayout>{s.reference}</TableCellLayout></TableCell>
                <TableCell><Badge appearance="tint" color={streamColor(s.stream)}>{t(`waste.streams.${s.stream}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{s.carrier ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{s.collectionCount}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{(s.status === 'Draft' ? s.liveWeightKg : s.totalWeightKg)} kg</TableCellLayout></TableCell>
                <TableCell><Badge appearance="tint" color={slipStatusColor(s.status)}>{t(`waste.slipStatuses.${s.status}`)}</Badge></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <div className={styles.pager}>
        <span className={styles.muted}>{t('waste.pager', { page, totalPages, total })}</span>
        <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => go(page - 1)}>{t('waste.prev')}</Button>
        <Button size="small" appearance="subtle" disabled={page >= totalPages} onClick={() => go(page + 1)}>{t('waste.next')}</Button>
      </div>

      <Dialog open={open} onOpenChange={(_, d) => setOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('waste.slips.create')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('waste.fields.reference')} required>
                    <Input value={f.reference} onChange={(_, d) => set('reference', d.value)} placeholder={t('waste.form.referencePlaceholder')} />
                  </Field>
                  <Field className={styles.grow} label={t('waste.fields.stream')} required>
                    <Dropdown value={t(`waste.streams.${f.stream}`)} selectedOptions={[f.stream]}
                      onOptionSelect={(_, d) => set('stream', (d.optionValue as WasteStream) ?? 'Dasri')}>
                      {WASTE_STREAMS.map((s) => <Option key={s} value={s}>{t(`waste.streams.${s}`)}</Option>)}
                    </Dropdown>
                  </Field>
                </div>
                <Field label={t('waste.fields.carrier')}>
                  <Input value={f.carrier} onChange={(_, d) => set('carrier', d.value)} />
                </Field>
                <Field label={t('waste.fields.destination')}>
                  <Input value={f.destination} onChange={(_, d) => set('destination', d.value)} />
                </Field>
                <Field label={t('waste.fields.treatment')}>
                  <Dropdown value={f.treatment === NONE ? t('waste.form.noTreatment') : t(`waste.treatments.${f.treatment}`)} selectedOptions={[f.treatment]}
                    onOptionSelect={(_, d) => set('treatment', d.optionValue ?? NONE)}>
                    <Option value={NONE}>{t('waste.form.noTreatment')}</Option>
                    {WASTE_TREATMENTS.map((w) => <Option key={w} value={w}>{t(`waste.treatments.${w}`)}</Option>)}
                  </Dropdown>
                </Field>
                {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={!canSubmit} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined} onClick={() => mutation.mutate()}>
                {t('actions.create')}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </>
  )
}
