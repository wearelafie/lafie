import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Field, Input, Spinner, Textarea,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { ArchiveRegular, BoxRegular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import {
  allocateSet, getInstrumentSet, retireInstrumentSet, setKeys, transitionUsage,
  type AllocateSetInput, type UsageAction,
} from '../api'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  actions: { display: 'flex', columnGap: tokens.spacingHorizontalS, flexWrap: 'wrap' },
  section: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM },
  sectionTitle: { fontWeight: tokens.fontWeightSemibold, fontSize: tokens.fontSizeBase300 },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '440px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1, minWidth: 0 },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
  usageActions: { display: 'flex', columnGap: tokens.spacingHorizontalXS },
})

const setStatusColor = (s: string): 'success' | 'warning' | 'informative' | 'danger' | 'subtle' =>
  s === 'Sterile' ? 'success' : s === 'InSterilization' ? 'informative' : s === 'InUse' ? 'warning'
    : s === 'Quarantine' ? 'danger' : s === 'Retired' ? 'subtle' : 'informative'
const usageStatusColor = (s: string): 'informative' | 'success' | 'subtle' | 'danger' =>
  s === 'Allocated' ? 'informative' : s === 'Used' ? 'success' : s === 'Cancelled' ? 'danger' : 'subtle'

type AllocForm = { interventionLabel: string; patientRef: string; scheduledOn: string; performedBy: string; notes: string }
const emptyAlloc: AllocForm = { interventionLabel: '', patientRef: '', scheduledOn: '', performedBy: '', notes: '' }

export function InstrumentSetDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const id = useAppStore((s) => s.selectedInstrumentSetId)

  const q = useQuery({ queryKey: setKeys.detail(id ?? ''), queryFn: () => getInstrumentSet(id!), enabled: !!id })
  const invalidate = () => { void queryClient.invalidateQueries({ queryKey: setKeys.detail(id ?? '') }) }

  const [allocOpen, setAllocOpen] = useState(false)
  const [a, setA] = useState<AllocForm>(emptyAlloc)
  const setField = <K extends keyof AllocForm>(k: K, v: AllocForm[K]) => setA((s) => ({ ...s, [k]: v }))

  const retireMut = useMutation({ mutationFn: () => retireInstrumentSet(id!), onSuccess: invalidate })
  const allocMut = useMutation({
    mutationFn: () => {
      const input: AllocateSetInput = {
        interventionLabel: a.interventionLabel.trim(), patientRef: a.patientRef.trim() || null,
        scheduledOn: a.scheduledOn || null, performedBy: a.performedBy.trim() || null, notes: a.notes.trim() || null,
      }
      return allocateSet(id!, input)
    },
    onSuccess: () => { invalidate(); setAllocOpen(false); setA(emptyAlloc) },
  })
  const usageMut = useMutation({
    mutationFn: ({ usageId, action }: { usageId: string; action: UsageAction }) => transitionUsage(usageId, action),
    onSuccess: invalidate,
  })

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('sterilization.setDetail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('sterilization.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('sterilization.loadError')}</div></div>

  const { set: s, usages } = q.data
  const fmt = (v: string | null) => (v ? new Date(v).toLocaleString() : null)
  const fmtDate = (v: string | null) => (v ? new Date(v).toLocaleDateString() : null)

  const rows: FieldRow[] = [
    { label: t('sterilization.fields.code'), value: s.code },
    { label: t('sterilization.fields.name'), value: s.name },
    { label: t('sterilization.fields.status'), value: <Badge appearance="tint" color={setStatusColor(s.status)}>{t(`sterilization.setStatuses.${s.status}`)}</Badge> },
    { label: t('sterilization.fields.category'), value: s.category },
    { label: t('sterilization.fields.items'), value: String(s.itemCount) },
    { label: t('sterilization.fields.description'), value: s.description },
    { label: t('sterilization.fields.sterilizedOn'), value: fmt(s.sterilizedOnUtc) },
    { label: t('sterilization.fields.expiry'), value: fmtDate(s.expiryOn) },
  ]
  const crumbs: Crumb[] = [
    { label: t('nav.infrastructure'), to: '/admin' },
    { label: t('sterilization.title'), to: '/admin/sterilization' },
  ]

  const canAllocate = s.status === 'Sterile'
  const canRetire = s.status !== 'InUse' && s.status !== 'InSterilization' && s.status !== 'Retired'

  return (
    <div className={styles.root}>
      <DetailHeader title={`${s.code} · ${s.name}`} crumbs={crumbs} />

      <div className={styles.actions}>
        <Button appearance="primary" icon={<BoxRegular />} disabled={!canAllocate} onClick={() => { setA(emptyAlloc); setAllocOpen(true) }}>
          {t('sterilization.actions.allocate')}
        </Button>
        {canRetire && <Button icon={<ArchiveRegular />} onClick={() => retireMut.mutate()}>{t('sterilization.actions.retire')}</Button>}
      </div>
      {retireMut.isError && <span className={styles.error}>{(retireMut.error as Error).message}</span>}
      {usageMut.isError && <span className={styles.error}>{(usageMut.error as Error).message}</span>}

      <FieldList rows={rows} />

      <div className={styles.section}>
        <span className={styles.sectionTitle}>{t('sterilization.usages.title')}</span>
        {usages.length === 0 ? (
          <div className={styles.muted}>{t('sterilization.usages.empty')}</div>
        ) : (
          <Table aria-label={t('sterilization.usages.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('sterilization.fields.intervention')}</TableHeaderCell>
                <TableHeaderCell>{t('sterilization.fields.patientRef')}</TableHeaderCell>
                <TableHeaderCell>{t('sterilization.fields.performedBy')}</TableHeaderCell>
                <TableHeaderCell>{t('sterilization.fields.status')}</TableHeaderCell>
                <TableHeaderCell>{t('sterilization.fields.actions')}</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {usages.map((u) => (
                <TableRow key={u.id}>
                  <TableCell>
                    <TableCellLayout description={fmtDate(u.scheduledOn) ?? undefined}>{u.interventionLabel}</TableCellLayout>
                  </TableCell>
                  <TableCell><TableCellLayout>{u.patientRef ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                  <TableCell><TableCellLayout>{u.performedBy ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                  <TableCell><Badge appearance="tint" color={usageStatusColor(u.status)}>{t(`sterilization.usageStatuses.${u.status}`)}</Badge></TableCell>
                  <TableCell>
                    <div className={styles.usageActions}>
                      {u.status === 'Allocated' && (
                        <Button size="small" appearance="subtle" disabled={usageMut.isPending} onClick={() => usageMut.mutate({ usageId: u.id, action: 'use' })}>
                          {t('sterilization.actions.markUsed')}
                        </Button>
                      )}
                      {(u.status === 'Allocated' || u.status === 'Used') && (
                        <Button size="small" appearance="subtle" disabled={usageMut.isPending} onClick={() => usageMut.mutate({ usageId: u.id, action: 'return' })}>
                          {t('sterilization.actions.return')}
                        </Button>
                      )}
                      {u.status === 'Allocated' && (
                        <Button size="small" appearance="subtle" disabled={usageMut.isPending} onClick={() => usageMut.mutate({ usageId: u.id, action: 'cancel' })}>
                          {t('actions.cancel')}
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>

      <Dialog open={allocOpen} onOpenChange={(_, d) => setAllocOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('sterilization.actions.allocate')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <Field label={t('sterilization.fields.intervention')} required>
                  <Input value={a.interventionLabel} onChange={(_, d) => setField('interventionLabel', d.value)} placeholder={t('sterilization.form.interventionPlaceholder')} />
                </Field>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('sterilization.fields.patientRef')}>
                    <Input value={a.patientRef} onChange={(_, d) => setField('patientRef', d.value)} />
                  </Field>
                  <Field className={styles.grow} label={t('sterilization.fields.scheduledOn')}>
                    <Input type="date" value={a.scheduledOn} onChange={(_, d) => setField('scheduledOn', d.value)} />
                  </Field>
                </div>
                <Field label={t('sterilization.fields.performedBy')}>
                  <Input value={a.performedBy} onChange={(_, d) => setField('performedBy', d.value)} />
                </Field>
                <Field label={t('sterilization.fields.notes')}>
                  <Textarea value={a.notes} onChange={(_, d) => setField('notes', d.value)} resize="vertical" />
                </Field>
                {allocMut.isError && <span className={styles.error}>{(allocMut.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setAllocOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={a.interventionLabel.trim() === '' || allocMut.isPending} onClick={() => allocMut.mutate()}>
                {t('sterilization.actions.allocate')}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
