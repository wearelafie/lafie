import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import {
  Badge, Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Input, Option, SearchBox, Spinner, Tab, TabList, Textarea, Title3,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Search20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { useActiveFacility, useCurrentUser } from '@features/auth'
import {
  SET_STATUSES, CYCLE_STATUSES, setKeys, cycleKeys, createInstrumentSet, createCycle, getInstrumentSets, getCycles,
  type InstrumentSet, type InstrumentSetFilter, type SterilizationCycle, type SterilizationCycleFilter,
  type CreateInstrumentSetInput, type CreateCycleInput,
} from '../api'

const PAGE_SIZE = 10
const ANY = '__any__'

const setStatusColor = (s: string): 'success' | 'warning' | 'informative' | 'danger' | 'subtle' =>
  s === 'Sterile' ? 'success' : s === 'InSterilization' ? 'informative' : s === 'InUse' ? 'warning'
    : s === 'Quarantine' ? 'danger' : s === 'Retired' ? 'subtle' : 'informative'
const cycleStatusColor = (s: string): 'informative' | 'warning' | 'success' | 'danger' =>
  s === 'Planned' ? 'informative' : s === 'Running' ? 'warning' : s === 'Passed' ? 'success' : 'danger'

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
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '480px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1, minWidth: 0 },
})

type SetForm = { code: string; name: string; category: string; itemCount: string; description: string }
const emptySetForm: SetForm = { code: '', name: '', category: '', itemCount: '0', description: '' }
type CycleForm = { autoclave: string; cycleNumber: string; program: string; operator: string }
const emptyCycleForm: CycleForm = { autoclave: '', cycleNumber: '', program: '', operator: '' }

export function SterilizationPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { me } = useCurrentUser()
  const setSelectedSet = useAppStore((s) => s.setSelectedInstrumentSet)
  const setSelectedCycle = useAppStore((s) => s.setSelectedCycle)
  const facilityId = useActiveFacility()

  const [tab, setTab] = useState<'sets' | 'cycles'>('sets')

  if (!facilityId) return <div className={styles.root}><div className={styles.state}>{t('sterilization.noFacility')}</div></div>

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>{t('sterilization.title')}</Title3>
          <div className={styles.subtitle}>{t('sterilization.subtitle')}</div>
        </div>
      </div>

      <TabList selectedValue={tab} onTabSelect={(_, d) => setTab(d.value as 'sets' | 'cycles')}>
        <Tab value="sets">{t('sterilization.tabs.sets')}</Tab>
        <Tab value="cycles">{t('sterilization.tabs.cycles')}</Tab>
      </TabList>

      {tab === 'sets' ? (
        <SetsTab
          facilityId={facilityId}
          onOpen={(s) => { setSelectedSet(s.id); void navigate({ to: '/admin/instrument-set' as never }) }}
          invalidate={() => queryClient.invalidateQueries({ queryKey: setKeys.all(facilityId) })}
        />
      ) : (
        <CyclesTab
          facilityId={facilityId}
          defaultOperator={me?.displayName ?? me?.username ?? ''}
          onOpen={(c) => { setSelectedCycle(c.id); void navigate({ to: '/admin/sterilization-cycle' as never }) }}
          invalidate={() => queryClient.invalidateQueries({ queryKey: cycleKeys.all(facilityId) })}
        />
      )}
    </div>
  )
}

function SetsTab({ facilityId, onOpen, invalidate }: { facilityId: string; onOpen: (s: InstrumentSet) => void; invalidate: () => void }) {
  const styles = useStyles()
  const { t } = useTranslation()

  const [draft, setDraft] = useState<{ search: string; status: string; category: string }>({ search: '', status: ANY, category: '' })
  const [applied, setApplied] = useState<InstrumentSetFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [open, setOpen] = useState(false)
  const [f, setF] = useState<SetForm>(emptySetForm)
  const set = <K extends keyof SetForm>(k: K, v: SetForm[K]) => setF((s) => ({ ...s, [k]: v }))

  const query = useQuery({
    queryKey: setKeys.list(facilityId, applied),
    queryFn: () => getInstrumentSets(facilityId, applied),
  })

  const mutation = useMutation({
    mutationFn: () => {
      const input: CreateInstrumentSetInput = {
        code: f.code.trim(), name: f.name.trim(), category: f.category.trim() || null,
        itemCount: Number.parseInt(f.itemCount, 10) || 0, description: f.description.trim() || null,
      }
      return createInstrumentSet(facilityId, input)
    },
    onSuccess: () => { invalidate(); setOpen(false); setF(emptySetForm) },
  })

  const runSearch = () => setApplied({
    page: 1, pageSize: PAGE_SIZE,
    search: draft.search.trim() || undefined,
    status: draft.status === ANY ? undefined : draft.status,
    category: draft.category.trim() || undefined,
  })

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))
  const canSubmit = f.code.trim() !== '' && f.name.trim() !== '' && !mutation.isPending

  return (
    <>
      <div className={styles.header}>
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('sterilization.filters.search')}</span>
            <SearchBox value={draft.search} placeholder={t('sterilization.filters.setSearchPlaceholder')}
              onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
              onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('sterilization.fields.status')}</span>
            <Dropdown value={draft.status === ANY ? t('sterilization.filters.allStatuses') : t(`sterilization.setStatuses.${draft.status}`)}
              selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('sterilization.filters.allStatuses')}</Option>
              {SET_STATUSES.map((s) => <Option key={s} value={s}>{t(`sterilization.setStatuses.${s}`)}</Option>)}
            </Dropdown>
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('sterilization.fields.category')}</span>
            <Input value={draft.category} onChange={(_, d) => setDraft((s) => ({ ...s, category: d.value }))}
              onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
          </div>
          <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('sterilization.filters.apply')}</Button>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setF(emptySetForm); setOpen(true) }}>{t('sterilization.sets.create')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('sterilization.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('sterilization.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('sterilization.sets.empty')}</div>
      ) : (
        <Table aria-label={t('sterilization.tabs.sets')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('sterilization.fields.code')}</TableHeaderCell>
              <TableHeaderCell>{t('sterilization.fields.name')}</TableHeaderCell>
              <TableHeaderCell>{t('sterilization.fields.category')}</TableHeaderCell>
              <TableHeaderCell>{t('sterilization.fields.items')}</TableHeaderCell>
              <TableHeaderCell>{t('sterilization.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('sterilization.fields.expiry')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((s) => (
              <TableRow key={s.id} className={styles.rowLink} onClick={() => onOpen(s)}>
                <TableCell><TableCellLayout>{s.code}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{s.name}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{s.category ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{s.itemCount}</TableCellLayout></TableCell>
                <TableCell><Badge appearance="tint" color={setStatusColor(s.status)}>{t(`sterilization.setStatuses.${s.status}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{s.expiryOn ? new Date(s.expiryOn).toLocaleDateString() : <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <div className={styles.pager}>
        <span className={styles.muted}>{t('sterilization.pager', { page, totalPages, total })}</span>
        <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => go(page - 1)}>{t('sterilization.prev')}</Button>
        <Button size="small" appearance="subtle" disabled={page >= totalPages} onClick={() => go(page + 1)}>{t('sterilization.next')}</Button>
      </div>

      <Dialog open={open} onOpenChange={(_, d) => setOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('sterilization.sets.create')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('sterilization.fields.code')} required>
                    <Input value={f.code} onChange={(_, d) => set('code', d.value)} />
                  </Field>
                  <Field className={styles.grow} label={t('sterilization.fields.items')}>
                    <Input type="number" min={0} value={f.itemCount} onChange={(_, d) => set('itemCount', d.value)} />
                  </Field>
                </div>
                <Field label={t('sterilization.fields.name')} required>
                  <Input value={f.name} onChange={(_, d) => set('name', d.value)} />
                </Field>
                <Field label={t('sterilization.fields.category')}>
                  <Input value={f.category} onChange={(_, d) => set('category', d.value)} placeholder={t('sterilization.form.categoryPlaceholder')} />
                </Field>
                <Field label={t('sterilization.fields.description')}>
                  <Textarea value={f.description} onChange={(_, d) => set('description', d.value)} resize="vertical" />
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

function CyclesTab({ facilityId, defaultOperator, onOpen, invalidate }: { facilityId: string; defaultOperator: string; onOpen: (c: SterilizationCycle) => void; invalidate: () => void }) {
  const styles = useStyles()
  const { t } = useTranslation()

  const [draft, setDraft] = useState<{ search: string; status: string; openOnly: boolean }>({ search: '', status: ANY, openOnly: false })
  const [applied, setApplied] = useState<SterilizationCycleFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [open, setOpen] = useState(false)
  const [f, setF] = useState<CycleForm>(emptyCycleForm)
  const set = <K extends keyof CycleForm>(k: K, v: CycleForm[K]) => setF((s) => ({ ...s, [k]: v }))

  const query = useQuery({
    queryKey: cycleKeys.list(facilityId, applied),
    queryFn: () => getCycles(facilityId, applied),
  })

  const mutation = useMutation({
    mutationFn: () => {
      const input: CreateCycleInput = {
        autoclave: f.autoclave.trim(), cycleNumber: f.cycleNumber.trim() || null,
        program: f.program.trim(), operator: f.operator.trim() || null,
      }
      return createCycle(facilityId, input)
    },
    onSuccess: () => { invalidate(); setOpen(false); setF(emptyCycleForm) },
  })

  const runSearch = () => setApplied({
    page: 1, pageSize: PAGE_SIZE,
    search: draft.search.trim() || undefined,
    status: draft.status === ANY ? undefined : draft.status,
    openOnly: draft.openOnly || undefined,
  })

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))
  const canSubmit = f.autoclave.trim() !== '' && f.program.trim() !== '' && !mutation.isPending

  return (
    <>
      <div className={styles.header}>
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('sterilization.filters.search')}</span>
            <SearchBox value={draft.search} placeholder={t('sterilization.filters.cycleSearchPlaceholder')}
              onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
              onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('sterilization.fields.status')}</span>
            <Dropdown value={draft.status === ANY ? t('sterilization.filters.allStatuses') : t(`sterilization.cycleStatuses.${draft.status}`)}
              selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('sterilization.filters.allStatuses')}</Option>
              {CYCLE_STATUSES.map((s) => <Option key={s} value={s}>{t(`sterilization.cycleStatuses.${s}`)}</Option>)}
            </Dropdown>
          </div>
          <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('sterilization.filters.apply')}</Button>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => { setF({ ...emptyCycleForm, operator: defaultOperator }); setOpen(true) }}>{t('sterilization.cycles.create')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('sterilization.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('sterilization.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('sterilization.cycles.empty')}</div>
      ) : (
        <Table aria-label={t('sterilization.tabs.cycles')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('sterilization.fields.autoclave')}</TableHeaderCell>
              <TableHeaderCell>{t('sterilization.fields.program')}</TableHeaderCell>
              <TableHeaderCell>{t('sterilization.fields.load')}</TableHeaderCell>
              <TableHeaderCell>{t('sterilization.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('sterilization.fields.operator')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((c) => (
              <TableRow key={c.id} className={styles.rowLink} onClick={() => onOpen(c)}>
                <TableCell>
                  <TableCellLayout description={c.cycleNumber ?? undefined}>{c.autoclave}</TableCellLayout>
                </TableCell>
                <TableCell><TableCellLayout>{c.program}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{c.loadCount}</TableCellLayout></TableCell>
                <TableCell><Badge appearance="tint" color={cycleStatusColor(c.status)}>{t(`sterilization.cycleStatuses.${c.status}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{c.operator ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <div className={styles.pager}>
        <span className={styles.muted}>{t('sterilization.pager', { page, totalPages, total })}</span>
        <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => go(page - 1)}>{t('sterilization.prev')}</Button>
        <Button size="small" appearance="subtle" disabled={page >= totalPages} onClick={() => go(page + 1)}>{t('sterilization.next')}</Button>
      </div>

      <Dialog open={open} onOpenChange={(_, d) => setOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('sterilization.cycles.create')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('sterilization.fields.autoclave')} required>
                    <Input value={f.autoclave} onChange={(_, d) => set('autoclave', d.value)} />
                  </Field>
                  <Field className={styles.grow} label={t('sterilization.fields.cycleNumber')}>
                    <Input value={f.cycleNumber} onChange={(_, d) => set('cycleNumber', d.value)} />
                  </Field>
                </div>
                <Field label={t('sterilization.fields.program')} required>
                  <Input value={f.program} onChange={(_, d) => set('program', d.value)} placeholder={t('sterilization.form.programPlaceholder')} />
                </Field>
                <Field label={t('sterilization.fields.operator')}>
                  <Input value={f.operator} onChange={(_, d) => set('operator', d.value)} />
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
