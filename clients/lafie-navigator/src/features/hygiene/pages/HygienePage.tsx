import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import {
  Badge, Button, Checkbox, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Option, SearchBox, Spinner, Textarea, Title3,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Search20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { useActiveFacility } from '@features/auth'
import { getSpacesByFacility } from '@features/space/api'
import {
  CLEANING_PROTOCOLS, CLEANING_STATUSES, CLEANING_TRIGGERS, cleaningKeys, createCleaningTask, getCleaningTasks,
  type CleaningFilter, type CleaningProtocol, type CleaningTask, type CleaningTrigger, type CreateCleaningInput,
} from '../api'

const PAGE_SIZE = 10
const ANY = '__any__'

const protocolColor = (p: string): 'brand' | 'warning' | 'danger' =>
  p === 'Terminal' ? 'danger' : p === 'Deep' ? 'warning' : 'brand'
const statusColor = (s: string): 'informative' | 'warning' | 'success' | 'subtle' =>
  s === 'Pending' ? 'informative' : s === 'InProgress' ? 'warning' : s === 'Completed' ? 'success' : 'subtle'

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

type FormState = { spaceId: string; protocol: CleaningProtocol; trigger: CleaningTrigger; notes: string }
const emptyForm: FormState = { spaceId: '', protocol: 'Standard', trigger: 'OnDemand', notes: '' }

export function HygienePage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const setSelectedCleaningTask = useAppStore((s) => s.setSelectedCleaningTask)
  const facilityId = useActiveFacility()

  const [draft, setDraft] = useState<{ search: string; status: string; protocol: string; activeOnly: boolean }>({ search: '', status: ANY, protocol: ANY, activeOnly: true })
  const [applied, setApplied] = useState<CleaningFilter>({ page: 1, pageSize: PAGE_SIZE, activeOnly: true })
  const [open, setOpen] = useState(false)
  const [f, setF] = useState<FormState>(emptyForm)
  const set = <K extends keyof FormState>(k: K, v: FormState[K]) => setF((s) => ({ ...s, [k]: v }))

  const query = useQuery({
    queryKey: cleaningKeys.list(facilityId ?? '', applied),
    queryFn: () => getCleaningTasks(facilityId!, applied),
    enabled: !!facilityId,
  })

  const spaces = useQuery({
    queryKey: ['spaces', facilityId, 'all-targets'],
    queryFn: () => getSpacesByFacility(facilityId!),
    enabled: !!facilityId && open,
  })
  const spaceOptions = (spaces.data?.items ?? []).map((s) => ({ id: s.id, label: `${s.code} · ${s.name}` }))
  const spaceLabel = spaceOptions.find((o) => o.id === f.spaceId)?.label ?? ''

  const invalidate = () => queryClient.invalidateQueries({ queryKey: cleaningKeys.all(facilityId ?? '') })
  const mutation = useMutation({
    mutationFn: () => {
      const input: CreateCleaningInput = { spaceId: f.spaceId, protocol: f.protocol, trigger: f.trigger, notes: f.notes.trim() || null }
      return createCleaningTask(facilityId!, input)
    },
    onSuccess: () => { invalidate(); setOpen(false); setF(emptyForm) },
  })

  const runSearch = () => setApplied({
    page: 1, pageSize: PAGE_SIZE,
    search: draft.search.trim() || undefined,
    status: draft.status === ANY ? undefined : draft.status,
    protocol: draft.protocol === ANY ? undefined : draft.protocol,
    activeOnly: draft.activeOnly || undefined,
  })

  const openDetail = (tk: CleaningTask) => { setSelectedCleaningTask(tk.id); void navigate({ to: '/admin/cleaning-task' as never }) }
  const openNew = () => { setF(emptyForm); setOpen(true) }

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))

  const canSubmit = f.spaceId !== '' && !mutation.isPending

  if (!facilityId) return <div className={styles.root}><div className={styles.state}>{t('hygiene.noFacility')}</div></div>

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>{t('hygiene.title')}</Title3>
          <div className={styles.subtitle}>{t('hygiene.subtitle')}</div>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={openNew}>{t('hygiene.declare')}</Button>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('hygiene.filters.search')}</span>
          <SearchBox value={draft.search} placeholder={t('hygiene.filters.searchPlaceholder')}
            onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
            onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('hygiene.fields.status')}</span>
          <Dropdown value={draft.status === ANY ? t('hygiene.filters.allStatuses') : t(`hygiene.statuses.${draft.status}`)}
            selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
            <Option value={ANY}>{t('hygiene.filters.allStatuses')}</Option>
            {CLEANING_STATUSES.map((s) => <Option key={s} value={s}>{t(`hygiene.statuses.${s}`)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('hygiene.fields.protocol')}</span>
          <Dropdown value={draft.protocol === ANY ? t('hygiene.filters.allProtocols') : t(`hygiene.protocols.${draft.protocol}`)}
            selectedOptions={[draft.protocol]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, protocol: d.optionValue ?? ANY }))}>
            <Option value={ANY}>{t('hygiene.filters.allProtocols')}</Option>
            {CLEANING_PROTOCOLS.map((p) => <Option key={p} value={p}>{t(`hygiene.protocols.${p}`)}</Option>)}
          </Dropdown>
        </div>
        <Checkbox label={t('hygiene.filters.activeOnly')} checked={draft.activeOnly} onChange={(_, d) => setDraft((s) => ({ ...s, activeOnly: !!d.checked }))} />
        <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('hygiene.filters.apply')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('hygiene.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('hygiene.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('hygiene.empty')}</div>
      ) : (
        <Table aria-label={t('hygiene.title')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('hygiene.fields.space')}</TableHeaderCell>
              <TableHeaderCell>{t('hygiene.fields.protocol')}</TableHeaderCell>
              <TableHeaderCell>{t('hygiene.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('hygiene.fields.checklist')}</TableHeaderCell>
              <TableHeaderCell>{t('hygiene.fields.assignee')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((tk) => (
              <TableRow key={tk.id} className={styles.rowLink} onClick={() => openDetail(tk)}>
                <TableCell>
                  <TableCellLayout description={t(`hygiene.triggers.${tk.trigger}`)}>
                    {tk.spaceName ?? tk.spaceCode ?? <span className={styles.muted}>—</span>}
                  </TableCellLayout>
                </TableCell>
                <TableCell><Badge appearance="tint" color={protocolColor(tk.protocol)}>{t(`hygiene.protocols.${tk.protocol}`)}</Badge></TableCell>
                <TableCell><Badge appearance="tint" color={statusColor(tk.status)}>{t(`hygiene.statuses.${tk.status}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{tk.itemsDone}/{tk.itemsTotal}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{tk.assignedTo ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <div className={styles.pager}>
        <span className={styles.muted}>{t('hygiene.pager', { page, totalPages, total })}</span>
        <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => go(page - 1)}>{t('hygiene.prev')}</Button>
        <Button size="small" appearance="subtle" disabled={page >= totalPages} onClick={() => go(page + 1)}>{t('hygiene.next')}</Button>
      </div>

      <Dialog open={open} onOpenChange={(_, d) => setOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('hygiene.declare')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <Field label={t('hygiene.fields.space')} required>
                  <Dropdown value={spaceLabel} selectedOptions={[f.spaceId]} placeholder={t('hygiene.form.selectSpace')}
                    onOptionSelect={(_, d) => set('spaceId', d.optionValue ?? '')}>
                    {spaces.isLoading
                      ? <Option value="" disabled>{t('hygiene.loading')}</Option>
                      : spaceOptions.map((o) => <Option key={o.id} value={o.id}>{o.label}</Option>)}
                  </Dropdown>
                </Field>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('hygiene.fields.protocol')} required>
                    <Dropdown value={t(`hygiene.protocols.${f.protocol}`)} selectedOptions={[f.protocol]}
                      onOptionSelect={(_, d) => set('protocol', (d.optionValue as CleaningProtocol) ?? 'Standard')}>
                      {CLEANING_PROTOCOLS.map((p) => <Option key={p} value={p}>{t(`hygiene.protocols.${p}`)}</Option>)}
                    </Dropdown>
                  </Field>
                  <Field className={styles.grow} label={t('hygiene.fields.trigger')}>
                    <Dropdown value={t(`hygiene.triggers.${f.trigger}`)} selectedOptions={[f.trigger]}
                      onOptionSelect={(_, d) => set('trigger', (d.optionValue as CleaningTrigger) ?? 'OnDemand')}>
                      {CLEANING_TRIGGERS.map((g) => <Option key={g} value={g}>{t(`hygiene.triggers.${g}`)}</Option>)}
                    </Dropdown>
                  </Field>
                </div>
                <div className={styles.muted}>{t('hygiene.form.protocolHint')}</div>
                <Field label={t('hygiene.fields.notes')}>
                  <Textarea value={f.notes} onChange={(_, d) => set('notes', d.value)} resize="vertical" />
                </Field>
                {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={!canSubmit} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined} onClick={() => mutation.mutate()}>
                {t('hygiene.form.submit')}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
