import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Dropdown, Field, Input, Option, Spinner, Textarea,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Delete20Regular, PersonAdd20Regular, PlayCircle20Regular, CheckmarkCircle20Regular, LockClosed20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { DetailHeader, FieldList, FormDrawer, type Crumb, type FieldRow } from '@shared/components'
import {
  TICKET_ASSIGNEE_TYPES, addTicketPart, assignTicket, deleteTicketPart, getTicket, ticketKeys, transitionTicket,
  type TicketAction, type TicketAssigneeType,
} from '../api'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  actions: { display: 'flex', columnGap: tokens.spacingHorizontalS, flexWrap: 'wrap' },
  section: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM },
  sectionTitle: { fontWeight: tokens.fontWeightSemibold, fontSize: tokens.fontSizeBase300 },
  toolbar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center' },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '420px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1, minWidth: 0 },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
})

const criticalityColor = (c: string): 'danger' | 'warning' | 'brand' | 'subtle' =>
  c === 'Critical' ? 'danger' : c === 'High' ? 'warning' : c === 'Medium' ? 'brand' : 'subtle'
const statusColor = (s: string): 'informative' | 'warning' | 'success' | 'subtle' =>
  s === 'Open' ? 'informative' : s === 'Assigned' || s === 'InProgress' ? 'warning' : s === 'Resolved' ? 'success' : 'subtle'

export function MaintenanceTicketDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const id = useAppStore((s) => s.selectedTicketId)

  const q = useQuery({ queryKey: ticketKeys.detail(id ?? ''), queryFn: () => getTicket(id!), enabled: !!id })
  const invalidate = () => { void queryClient.invalidateQueries({ queryKey: ticketKeys.detail(id ?? '') }) }

  const [assignOpen, setAssignOpen] = useState(false)
  const [assignType, setAssignType] = useState<TicketAssigneeType>('Internal')
  const [assignRef, setAssignRef] = useState('')
  const [resolveOpen, setResolveOpen] = useState(false)
  const [resolution, setResolution] = useState('')
  const [partOpen, setPartOpen] = useState(false)
  const [part, setPart] = useState({ label: '', reference: '', quantity: '1', unitCost: '' })

  const transitionMut = useMutation({
    mutationFn: ({ action, res }: { action: TicketAction; res?: string }) => transitionTicket(id!, action, res),
    onSuccess: () => { invalidate(); setResolveOpen(false); setResolution('') },
  })
  const assignMut = useMutation({
    mutationFn: () => assignTicket(id!, assignType, assignRef.trim()),
    onSuccess: () => { invalidate(); setAssignOpen(false); setAssignRef('') },
  })
  const addPartMut = useMutation({
    mutationFn: () => addTicketPart(id!, {
      label: part.label.trim(),
      reference: part.reference.trim() || null,
      quantity: Number.parseInt(part.quantity, 10) || 1,
      unitCost: part.unitCost.trim() === '' ? null : Number.parseFloat(part.unitCost.replace(',', '.')),
    }),
    onSuccess: () => { invalidate(); setPartOpen(false); setPart({ label: '', reference: '', quantity: '1', unitCost: '' }) },
  })
  const deletePartMut = useMutation({ mutationFn: (partId: string) => deleteTicketPart(partId), onSuccess: invalidate })

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('maintenance.detail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('maintenance.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('maintenance.loadError')}</div></div>

  const { ticket: tk, parts } = q.data
  const fmt = (v: string | null) => (v ? new Date(v).toLocaleString() : null)

  const rows: FieldRow[] = [
    { label: t('maintenance.fields.target'), value: `${tk.targetName ?? tk.targetCode ?? '—'} (${t(`maintenance.targetTypes.${tk.targetType}`)})` },
    { label: t('maintenance.fields.criticality'), value: <Badge appearance="tint" color={criticalityColor(tk.criticality)}>{t(`maintenance.criticalities.${tk.criticality}`)}</Badge> },
    { label: t('maintenance.fields.status'), value: <Badge appearance="tint" color={statusColor(tk.status)}>{t(`maintenance.statuses.${tk.status}`)}</Badge> },
    { label: t('maintenance.fields.blocking'), value: tk.blocking ? t('common.yes') : t('common.no') },
    { label: t('maintenance.fields.description'), value: tk.description },
    { label: t('maintenance.fields.assignee'), value: tk.assigneeRef ? `${tk.assigneeRef} (${t(`maintenance.assigneeTypes.${tk.assigneeType}`)})` : null },
    { label: t('maintenance.fields.reportedBy'), value: tk.reportedBy },
    { label: t('maintenance.fields.resolution'), value: tk.resolution },
    { label: t('maintenance.fields.downtimeStart'), value: fmt(tk.downtimeStart) },
    { label: t('maintenance.fields.downtimeEnd'), value: fmt(tk.downtimeEnd) },
  ]
  const crumbs: Crumb[] = [
    { label: t('nav.infrastructure'), to: '/admin' },
    { label: t('maintenance.title'), to: '/admin/maintenance' },
  ]

  const canAssign = tk.status !== 'Closed'
  const canStart = tk.status === 'Open' || tk.status === 'Assigned'
  const canResolve = tk.status === 'InProgress'
  const canClose = tk.status === 'Resolved'

  return (
    <div className={styles.root}>
      <DetailHeader title={tk.title} crumbs={crumbs} />

      <div className={styles.actions}>
        {canAssign && <Button icon={<PersonAdd20Regular />} onClick={() => setAssignOpen(true)}>{t('maintenance.actions.assign')}</Button>}
        {canStart && <Button appearance="primary" icon={<PlayCircle20Regular />} onClick={() => transitionMut.mutate({ action: 'start' })}>{t('maintenance.actions.start')}</Button>}
        {canResolve && <Button appearance="primary" icon={<CheckmarkCircle20Regular />} onClick={() => setResolveOpen(true)}>{t('maintenance.actions.resolve')}</Button>}
        {canClose && <Button icon={<LockClosed20Regular />} onClick={() => transitionMut.mutate({ action: 'close' })}>{t('maintenance.actions.close')}</Button>}
      </div>
      {transitionMut.isError && <span className={styles.error}>{(transitionMut.error as Error).message}</span>}

      <FieldList rows={rows} />

      <div className={styles.section}>
        <div className={styles.toolbar}>
          <span className={styles.sectionTitle}>{t('maintenance.parts.title')}</span>
          <Button size="small" icon={<Add20Regular />} onClick={() => setPartOpen(true)}>{t('maintenance.parts.add')}</Button>
        </div>
        {parts.length === 0 ? (
          <div className={styles.muted}>{t('maintenance.parts.empty')}</div>
        ) : (
          <Table aria-label={t('maintenance.parts.title')} size="small">
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('maintenance.parts.fields.label')}</TableHeaderCell>
                <TableHeaderCell>{t('maintenance.parts.fields.reference')}</TableHeaderCell>
                <TableHeaderCell>{t('maintenance.parts.fields.quantity')}</TableHeaderCell>
                <TableHeaderCell>{t('maintenance.parts.fields.unitCost')}</TableHeaderCell>
                <TableHeaderCell />
              </TableRow>
            </TableHeader>
            <TableBody>
              {parts.map((p) => (
                <TableRow key={p.id}>
                  <TableCell><TableCellLayout>{p.label}</TableCellLayout></TableCell>
                  <TableCell><TableCellLayout>{p.reference ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                  <TableCell>{p.quantity}</TableCell>
                  <TableCell>{p.unitCost != null ? p.unitCost.toLocaleString() : <span className={styles.muted}>—</span>}</TableCell>
                  <TableCell>
                    <Button size="small" appearance="subtle" icon={<Delete20Regular />} onClick={() => deletePartMut.mutate(p.id)} aria-label={t('actions.delete')} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>

      {/* Tiroir d'affectation */}
      <FormDrawer
        open={assignOpen}
        onOpenChange={setAssignOpen}
        title={t('maintenance.actions.assign')}
        onSubmit={() => { if (assignRef.trim() !== '' && !assignMut.isPending) assignMut.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setAssignOpen(false)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={assignRef.trim() === '' || assignMut.isPending}>{t('actions.save')}</Button>
        </>}>
              <div className={styles.form}>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('maintenance.fields.assigneeType')}>
                    <Dropdown value={t(`maintenance.assigneeTypes.${assignType}`)} selectedOptions={[assignType]}
                      onOptionSelect={(_, d) => setAssignType((d.optionValue as TicketAssigneeType) ?? 'Internal')}>
                      {TICKET_ASSIGNEE_TYPES.map((a) => <Option key={a} value={a}>{t(`maintenance.assigneeTypes.${a}`)}</Option>)}
                    </Dropdown>
                  </Field>
                </div>
                <Field label={t('maintenance.fields.assignee')} required>
                  <Input value={assignRef} onChange={(_, d) => setAssignRef(d.value)} placeholder={t('maintenance.form.assigneePlaceholder')} />
                </Field>
                {assignMut.isError && <span className={styles.error}>{(assignMut.error as Error).message}</span>}
              </div>
      </FormDrawer>

      {/* Tiroir de résolution */}
      <FormDrawer
        open={resolveOpen}
        onOpenChange={setResolveOpen}
        title={t('maintenance.actions.resolve')}
        onSubmit={() => { if (!transitionMut.isPending) transitionMut.mutate({ action: 'resolve', res: resolution.trim() || undefined }) }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setResolveOpen(false)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={transitionMut.isPending}>{t('maintenance.actions.resolve')}</Button>
        </>}>
              <div className={styles.form}>
                <Field label={t('maintenance.fields.resolution')}>
                  <Textarea value={resolution} onChange={(_, d) => setResolution(d.value)} resize="vertical" />
                </Field>
                {transitionMut.isError && <span className={styles.error}>{(transitionMut.error as Error).message}</span>}
              </div>
      </FormDrawer>

      {/* Tiroir d'ajout de pièce */}
      <FormDrawer
        open={partOpen}
        onOpenChange={setPartOpen}
        title={t('maintenance.parts.add')}
        onSubmit={() => { if (part.label.trim() !== '' && !addPartMut.isPending) addPartMut.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setPartOpen(false)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={part.label.trim() === '' || addPartMut.isPending}>{t('actions.save')}</Button>
        </>}>
              <div className={styles.form}>
                <Field label={t('maintenance.parts.fields.label')} required>
                  <Input value={part.label} onChange={(_, d) => setPart((s) => ({ ...s, label: d.value }))} />
                </Field>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('maintenance.parts.fields.reference')}>
                    <Input value={part.reference} onChange={(_, d) => setPart((s) => ({ ...s, reference: d.value }))} />
                  </Field>
                  <Field label={t('maintenance.parts.fields.quantity')}>
                    <Input type="number" value={part.quantity} onChange={(_, d) => setPart((s) => ({ ...s, quantity: d.value }))} style={{ width: '100px' }} />
                  </Field>
                  <Field label={t('maintenance.parts.fields.unitCost')}>
                    <Input type="number" value={part.unitCost} onChange={(_, d) => setPart((s) => ({ ...s, unitCost: d.value }))} style={{ width: '140px' }} />
                  </Field>
                </div>
                {addPartMut.isError && <span className={styles.error}>{(addPartMut.error as Error).message}</span>}
              </div>
      </FormDrawer>
    </div>
  )
}
