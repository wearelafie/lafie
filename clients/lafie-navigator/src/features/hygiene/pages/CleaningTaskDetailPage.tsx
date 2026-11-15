import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Checkbox, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Field, Input, ProgressBar, Spinner,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Delete20Regular, PersonAdd20Regular, PlayCircle20Regular, CheckmarkCircle20Regular, DismissCircle20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import {
  addCleaningItem, assignCleaningTask, cleaningKeys, deleteCleaningItem, getCleaningTask, toggleCleaningItem, transitionCleaningTask,
  type CleaningAction,
} from '../api'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  actions: { display: 'flex', columnGap: tokens.spacingHorizontalS, flexWrap: 'wrap' },
  section: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM },
  sectionTitle: { fontWeight: tokens.fontWeightSemibold, fontSize: tokens.fontSizeBase300 },
  toolbar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', columnGap: tokens.spacingHorizontalM },
  progress: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXS },
  checklist: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXS },
  item: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalS, justifyContent: 'space-between' },
  itemMain: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalS, minWidth: 0 },
  done: { textDecoration: 'line-through', color: tokens.colorNeutralForeground3 },
  form: { display: 'flex', columnGap: tokens.spacingHorizontalS, alignItems: 'flex-end' },
  grow: { flex: 1, minWidth: 0 },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
})

const protocolColor = (p: string): 'brand' | 'warning' | 'danger' =>
  p === 'Terminal' ? 'danger' : p === 'Deep' ? 'warning' : 'brand'
const statusColor = (s: string): 'informative' | 'warning' | 'success' | 'subtle' =>
  s === 'Pending' ? 'informative' : s === 'InProgress' ? 'warning' : s === 'Completed' ? 'success' : 'subtle'

export function CleaningTaskDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const id = useAppStore((s) => s.selectedCleaningTaskId)

  const q = useQuery({ queryKey: cleaningKeys.detail(id ?? ''), queryFn: () => getCleaningTask(id!), enabled: !!id })
  const invalidate = () => { void queryClient.invalidateQueries({ queryKey: cleaningKeys.detail(id ?? '') }) }

  const [assignOpen, setAssignOpen] = useState(false)
  const [assignTo, setAssignTo] = useState('')
  const [newItem, setNewItem] = useState('')

  const transitionMut = useMutation({
    mutationFn: (action: CleaningAction) => transitionCleaningTask(id!, action),
    onSuccess: invalidate,
  })
  const assignMut = useMutation({
    mutationFn: () => assignCleaningTask(id!, assignTo.trim()),
    onSuccess: () => { invalidate(); setAssignOpen(false); setAssignTo('') },
  })
  const toggleMut = useMutation({
    mutationFn: ({ itemId, done }: { itemId: string; done: boolean }) => toggleCleaningItem(itemId, done),
    onSuccess: invalidate,
  })
  const addItemMut = useMutation({
    mutationFn: () => addCleaningItem(id!, newItem.trim()),
    onSuccess: () => { invalidate(); setNewItem('') },
  })
  const deleteItemMut = useMutation({ mutationFn: (itemId: string) => deleteCleaningItem(itemId), onSuccess: invalidate })

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('hygiene.detail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('hygiene.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('hygiene.loadError')}</div></div>

  const { task: tk, items } = q.data
  const fmt = (v: string | null) => (v ? new Date(v).toLocaleString() : null)
  const doneCount = items.filter((i) => i.done).length
  const allDone = items.length > 0 && doneCount === items.length
  const closed = tk.status === 'Completed' || tk.status === 'Cancelled'

  const rows: FieldRow[] = [
    { label: t('hygiene.fields.space'), value: `${tk.spaceName ?? tk.spaceCode ?? '—'}` },
    { label: t('hygiene.fields.protocol'), value: <Badge appearance="tint" color={protocolColor(tk.protocol)}>{t(`hygiene.protocols.${tk.protocol}`)}</Badge> },
    { label: t('hygiene.fields.status'), value: <Badge appearance="tint" color={statusColor(tk.status)}>{t(`hygiene.statuses.${tk.status}`)}</Badge> },
    { label: t('hygiene.fields.trigger'), value: t(`hygiene.triggers.${tk.trigger}`) },
    { label: t('hygiene.fields.assignee'), value: tk.assignedTo },
    { label: t('hygiene.fields.requestedBy'), value: tk.requestedBy },
    { label: t('hygiene.fields.notes'), value: tk.notes },
    { label: t('hygiene.fields.startedOn'), value: fmt(tk.startedOnUtc) },
    { label: t('hygiene.fields.completedOn'), value: fmt(tk.completedOnUtc) },
  ]
  const crumbs: Crumb[] = [
    { label: t('nav.infrastructure'), to: '/admin' },
    { label: t('hygiene.title'), to: '/admin/hygiene' },
  ]

  const canStart = tk.status === 'Pending'
  const canComplete = (tk.status === 'Pending' || tk.status === 'InProgress') && allDone
  const canCancel = !closed

  return (
    <div className={styles.root}>
      <DetailHeader title={`${tk.spaceName ?? tk.spaceCode ?? t('hygiene.title')}`} crumbs={crumbs} />

      <div className={styles.actions}>
        {!closed && <Button icon={<PersonAdd20Regular />} onClick={() => setAssignOpen(true)}>{t('hygiene.actions.assign')}</Button>}
        {canStart && <Button appearance="primary" icon={<PlayCircle20Regular />} onClick={() => transitionMut.mutate('start')}>{t('hygiene.actions.start')}</Button>}
        {!closed && <Button appearance="primary" icon={<CheckmarkCircle20Regular />} disabled={!canComplete} onClick={() => transitionMut.mutate('complete')}>{t('hygiene.actions.complete')}</Button>}
        {canCancel && <Button icon={<DismissCircle20Regular />} onClick={() => transitionMut.mutate('cancel')}>{t('hygiene.actions.cancel')}</Button>}
      </div>
      {transitionMut.isError && <span className={styles.error}>{(transitionMut.error as Error).message}</span>}

      <FieldList rows={rows} />

      <div className={styles.section}>
        <div className={styles.toolbar}>
          <span className={styles.sectionTitle}>{t('hygiene.checklist.title')}</span>
          <span className={styles.muted}>{t('hygiene.checklist.progress', { done: doneCount, total: items.length })}</span>
        </div>
        <div className={styles.progress}>
          <ProgressBar value={items.length === 0 ? 0 : doneCount / items.length} thickness="large" color={allDone ? 'success' : 'brand'} />
        </div>

        <div className={styles.checklist}>
          {items.map((it) => (
            <div key={it.id} className={styles.item}>
              <div className={styles.itemMain}>
                <Checkbox checked={it.done} disabled={closed || toggleMut.isPending}
                  onChange={(_, d) => toggleMut.mutate({ itemId: it.id, done: !!d.checked })} />
                <span className={it.done ? styles.done : undefined}>{it.label}</span>
              </div>
              {!closed && (
                <Button size="small" appearance="subtle" icon={<Delete20Regular />} aria-label={t('actions.delete')}
                  onClick={() => deleteItemMut.mutate(it.id)} />
              )}
            </div>
          ))}
          {items.length === 0 && <div className={styles.muted}>{t('hygiene.checklist.empty')}</div>}
        </div>

        {!closed && (
          <div className={styles.form}>
            <Field className={styles.grow} label={t('hygiene.checklist.addLabel')}>
              <Input value={newItem} onChange={(_, d) => setNewItem(d.value)}
                onKeyDown={(e) => { if (e.key === 'Enter' && newItem.trim()) addItemMut.mutate() }} />
            </Field>
            <Button icon={<Add20Regular />} disabled={newItem.trim() === '' || addItemMut.isPending} onClick={() => addItemMut.mutate()}>
              {t('hygiene.checklist.add')}
            </Button>
          </div>
        )}
      </div>

      <Dialog open={assignOpen} onOpenChange={(_, d) => setAssignOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('hygiene.actions.assign')}</DialogTitle>
            <DialogContent>
              <Field label={t('hygiene.fields.assignee')} required>
                <Input value={assignTo} onChange={(_, d) => setAssignTo(d.value)} placeholder={t('hygiene.form.assigneePlaceholder')} />
              </Field>
              {assignMut.isError && <span className={styles.error}>{(assignMut.error as Error).message}</span>}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setAssignOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={assignTo.trim() === '' || assignMut.isPending} onClick={() => assignMut.mutate()}>{t('actions.save')}</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
