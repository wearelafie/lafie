import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import {
  Badge, Button, Checkbox, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Input, Option, SearchBox, Spinner, Textarea, Title3,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Search20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { useCurrentUser } from '@features/auth'
import { getEquipmentList } from '@features/equipment/api'
import { getSpacesByFacility } from '@features/space/api'
import {
  TICKET_CRITICALITIES, TICKET_STATUSES, TICKET_TARGET_TYPES, createTicket, getTickets, ticketKeys,
  type CreateTicketInput, type MaintenanceTicket, type TicketCriticality, type TicketFilter, type TicketTargetType,
} from '../api'

const PAGE_SIZE = 10
const ANY = '__any__'

const criticalityColor = (c: string): 'danger' | 'warning' | 'brand' | 'subtle' =>
  c === 'Critical' ? 'danger' : c === 'High' ? 'warning' : c === 'Medium' ? 'brand' : 'subtle'
const statusColor = (s: string): 'informative' | 'warning' | 'success' | 'subtle' =>
  s === 'Open' ? 'informative' : s === 'Assigned' || s === 'InProgress' ? 'warning' : s === 'Resolved' ? 'success' : 'subtle'

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

type FormState = { targetType: TicketTargetType; targetId: string; title: string; description: string; criticality: TicketCriticality; blocking: boolean }
const emptyForm: FormState = { targetType: 'Equipment', targetId: '', title: '', description: '', criticality: 'Medium', blocking: false }

export function MaintenancePage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { me } = useCurrentUser()
  const storeFacilityId = useAppStore((s) => s.selectedFacilityId)
  const setSelectedTicket = useAppStore((s) => s.setSelectedTicket)
  const facilityId = me?.activeEstablishment?.facilityId ?? storeFacilityId

  const [draft, setDraft] = useState<{ search: string; status: string; criticality: string; openOnly: boolean }>({ search: '', status: ANY, criticality: ANY, openOnly: true })
  const [applied, setApplied] = useState<TicketFilter>({ page: 1, pageSize: PAGE_SIZE, openOnly: true })
  const [open, setOpen] = useState(false)
  const [f, setF] = useState<FormState>(emptyForm)
  const set = <K extends keyof FormState>(k: K, v: FormState[K]) => setF((s) => ({ ...s, [k]: v }))

  const query = useQuery({
    queryKey: ticketKeys.list(facilityId ?? '', applied),
    queryFn: () => getTickets(facilityId!, applied),
    enabled: !!facilityId,
  })

  // Cibles pour la déclaration : équipements et espaces de l'établissement.
  const equipment = useQuery({
    queryKey: ['equipment', facilityId, 'all-targets'],
    queryFn: () => getEquipmentList(facilityId!, { page: 1, pageSize: 200 }),
    enabled: !!facilityId && open && f.targetType === 'Equipment',
  })
  const spaces = useQuery({
    queryKey: ['spaces', facilityId, 'all-targets'],
    queryFn: () => getSpacesByFacility(facilityId!),
    enabled: !!facilityId && open && f.targetType === 'Space',
  })

  const targetOptions = useMemo(() => {
    if (f.targetType === 'Equipment') return (equipment.data?.items ?? []).map((e) => ({ id: e.id, label: `${e.inventoryNumber} · ${e.name}` }))
    return (spaces.data?.items ?? []).map((s) => ({ id: s.id, label: `${s.code} · ${s.name}` }))
  }, [f.targetType, equipment.data, spaces.data])
  const targetLabel = targetOptions.find((o) => o.id === f.targetId)?.label ?? ''

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ticketKeys.all(facilityId ?? '') })
  const mutation = useMutation({
    mutationFn: () => {
      const input: CreateTicketInput = {
        targetType: f.targetType,
        targetId: f.targetId,
        title: f.title.trim(),
        description: f.description.trim() || null,
        criticality: f.criticality,
        blocking: f.blocking,
      }
      return createTicket(facilityId!, input)
    },
    onSuccess: () => { invalidate(); setOpen(false); setF(emptyForm) },
  })

  const runSearch = () => setApplied({
    page: 1, pageSize: PAGE_SIZE,
    search: draft.search.trim() || undefined,
    status: draft.status === ANY ? undefined : draft.status,
    criticality: draft.criticality === ANY ? undefined : draft.criticality,
    openOnly: draft.openOnly || undefined,
  })

  const openDetail = (tk: MaintenanceTicket) => { setSelectedTicket(tk.id); void navigate({ to: '/admin/maintenance-ticket' as never }) }
  const openNew = () => { setF(emptyForm); setOpen(true) }

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))

  const canSubmit = f.targetId !== '' && f.title.trim() !== '' && !mutation.isPending

  if (!facilityId) return <div className={styles.root}><div className={styles.state}>{t('maintenance.noFacility')}</div></div>

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>{t('maintenance.title')}</Title3>
          <div className={styles.subtitle}>{t('maintenance.subtitle')}</div>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={openNew}>{t('maintenance.declare')}</Button>
      </div>

      <div className={styles.filters}>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('maintenance.filters.search')}</span>
          <SearchBox value={draft.search} placeholder={t('maintenance.filters.searchPlaceholder')}
            onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
            onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('maintenance.fields.status')}</span>
          <Dropdown value={draft.status === ANY ? t('maintenance.filters.allStatuses') : t(`maintenance.statuses.${draft.status}`)}
            selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
            <Option value={ANY}>{t('maintenance.filters.allStatuses')}</Option>
            {TICKET_STATUSES.map((s) => <Option key={s} value={s}>{t(`maintenance.statuses.${s}`)}</Option>)}
          </Dropdown>
        </div>
        <div className={styles.filterField}>
          <span className={styles.label}>{t('maintenance.fields.criticality')}</span>
          <Dropdown value={draft.criticality === ANY ? t('maintenance.filters.allCriticalities') : t(`maintenance.criticalities.${draft.criticality}`)}
            selectedOptions={[draft.criticality]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, criticality: d.optionValue ?? ANY }))}>
            <Option value={ANY}>{t('maintenance.filters.allCriticalities')}</Option>
            {TICKET_CRITICALITIES.map((c) => <Option key={c} value={c}>{t(`maintenance.criticalities.${c}`)}</Option>)}
          </Dropdown>
        </div>
        <Checkbox label={t('maintenance.filters.openOnly')} checked={draft.openOnly} onChange={(_, d) => setDraft((s) => ({ ...s, openOnly: !!d.checked }))} />
        <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('maintenance.filters.apply')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('maintenance.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('maintenance.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('maintenance.empty')}</div>
      ) : (
        <Table aria-label={t('maintenance.title')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('maintenance.fields.title')}</TableHeaderCell>
              <TableHeaderCell>{t('maintenance.fields.target')}</TableHeaderCell>
              <TableHeaderCell>{t('maintenance.fields.criticality')}</TableHeaderCell>
              <TableHeaderCell>{t('maintenance.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('maintenance.fields.assignee')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((tk) => (
              <TableRow key={tk.id} className={styles.rowLink} onClick={() => openDetail(tk)}>
                <TableCell>
                  <TableCellLayout>
                    {tk.blocking ? <Badge appearance="tint" color="danger" style={{ marginRight: 6 }}>{t('maintenance.blocking')}</Badge> : null}
                    {tk.title}
                  </TableCellLayout>
                </TableCell>
                <TableCell>
                  <TableCellLayout description={t(`maintenance.targetTypes.${tk.targetType}`)}>
                    {tk.targetName ?? tk.targetCode ?? <span className={styles.muted}>—</span>}
                  </TableCellLayout>
                </TableCell>
                <TableCell><Badge appearance="tint" color={criticalityColor(tk.criticality)}>{t(`maintenance.criticalities.${tk.criticality}`)}</Badge></TableCell>
                <TableCell><Badge appearance="tint" color={statusColor(tk.status)}>{t(`maintenance.statuses.${tk.status}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{tk.assigneeRef ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <div className={styles.pager}>
        <span className={styles.muted}>{t('maintenance.pager', { page, totalPages, total })}</span>
        <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => go(page - 1)}>{t('maintenance.prev')}</Button>
        <Button size="small" appearance="subtle" disabled={page >= totalPages} onClick={() => go(page + 1)}>{t('maintenance.next')}</Button>
      </div>

      <Dialog open={open} onOpenChange={(_, d) => setOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('maintenance.declare')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('maintenance.fields.targetType')} required>
                    <Dropdown value={t(`maintenance.targetTypes.${f.targetType}`)} selectedOptions={[f.targetType]}
                      onOptionSelect={(_, d) => { set('targetType', (d.optionValue as TicketTargetType) ?? 'Equipment'); set('targetId', '') }}>
                      {TICKET_TARGET_TYPES.map((tt) => <Option key={tt} value={tt}>{t(`maintenance.targetTypes.${tt}`)}</Option>)}
                    </Dropdown>
                  </Field>
                  <Field className={styles.grow} label={t('maintenance.fields.target')} required>
                    <Dropdown value={targetLabel} selectedOptions={[f.targetId]} placeholder={t('maintenance.form.selectTarget')}
                      onOptionSelect={(_, d) => set('targetId', d.optionValue ?? '')}>
                      {(equipment.isLoading || spaces.isLoading)
                        ? <Option value="" disabled>{t('maintenance.loading')}</Option>
                        : targetOptions.map((o) => <Option key={o.id} value={o.id}>{o.label}</Option>)}
                    </Dropdown>
                  </Field>
                </div>
                <Field label={t('maintenance.fields.title')} required>
                  <Input value={f.title} onChange={(_, d) => set('title', d.value)} />
                </Field>
                <Field label={t('maintenance.fields.description')}>
                  <Textarea value={f.description} onChange={(_, d) => set('description', d.value)} resize="vertical" />
                </Field>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('maintenance.fields.criticality')}>
                    <Dropdown value={t(`maintenance.criticalities.${f.criticality}`)} selectedOptions={[f.criticality]}
                      onOptionSelect={(_, d) => set('criticality', (d.optionValue as TicketCriticality) ?? 'Medium')}>
                      {TICKET_CRITICALITIES.map((c) => <Option key={c} value={c}>{t(`maintenance.criticalities.${c}`)}</Option>)}
                    </Dropdown>
                  </Field>
                </div>
                <Checkbox label={t('maintenance.form.blockingHint')} checked={f.blocking} onChange={(_, d) => set('blocking', !!d.checked)} />
                {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={!canSubmit} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined} onClick={() => mutation.mutate()}>
                {t('maintenance.form.submit')}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
