import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Checkbox, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Input, Option, Spinner, Textarea,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Delete20Regular, PlayCircle20Regular, CheckmarkCircle20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import {
  addSetToCycle, closeCycle, getCycle, getInstrumentSets, removeSetFromCycle, startCycle, cycleKeys,
  type CloseCycleInput,
} from '../api'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  actions: { display: 'flex', columnGap: tokens.spacingHorizontalS, flexWrap: 'wrap' },
  section: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM },
  toolbar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', columnGap: tokens.spacingHorizontalM },
  sectionTitle: { fontWeight: tokens.fontWeightSemibold, fontSize: tokens.fontSizeBase300 },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '440px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1, minWidth: 0 },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
})

const setStatusColor = (s: string): 'success' | 'warning' | 'informative' | 'danger' | 'subtle' =>
  s === 'Sterile' ? 'success' : s === 'InSterilization' ? 'informative' : s === 'InUse' ? 'warning'
    : s === 'Quarantine' ? 'danger' : s === 'Retired' ? 'subtle' : 'informative'
const cycleStatusColor = (s: string): 'informative' | 'warning' | 'success' | 'danger' =>
  s === 'Planned' ? 'informative' : s === 'Running' ? 'warning' : s === 'Passed' ? 'success' : 'danger'

type CloseForm = { passed: boolean; temperatureC: string; pressureBar: string; durationMin: string; resultNotes: string }
const emptyClose: CloseForm = { passed: true, temperatureC: '', pressureBar: '', durationMin: '', resultNotes: '' }

export function SterilizationCycleDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const id = useAppStore((s) => s.selectedCycleId)

  const q = useQuery({ queryKey: cycleKeys.detail(id ?? ''), queryFn: () => getCycle(id!), enabled: !!id })
  const invalidate = () => { void queryClient.invalidateQueries({ queryKey: cycleKeys.detail(id ?? '') }) }

  const [addOpen, setAddOpen] = useState(false)
  const [pickSetId, setPickSetId] = useState('')
  const [closeOpen, setCloseOpen] = useState(false)
  const [c, setC] = useState<CloseForm>(emptyClose)
  const setField = <K extends keyof CloseForm>(k: K, v: CloseForm[K]) => setC((s) => ({ ...s, [k]: v }))

  const facilityId = q.data?.cycle.facilityId
  const eligible = useQuery({
    queryKey: ['instrument-sets', facilityId, 'eligible'],
    queryFn: () => getInstrumentSets(facilityId!, { page: 1, pageSize: 100 }),
    enabled: !!facilityId && addOpen,
  })
  const eligibleOptions = (eligible.data?.items ?? [])
    .filter((s) => s.status === 'Soiled' || s.status === 'Quarantine')
    .map((s) => ({ id: s.id, label: `${s.code} · ${s.name}` }))
  const pickLabel = eligibleOptions.find((o) => o.id === pickSetId)?.label ?? ''

  const addMut = useMutation({
    mutationFn: () => addSetToCycle(id!, pickSetId),
    onSuccess: () => { invalidate(); setAddOpen(false); setPickSetId('') },
  })
  const removeMut = useMutation({ mutationFn: (setId: string) => removeSetFromCycle(id!, setId), onSuccess: invalidate })
  const startMut = useMutation({ mutationFn: () => startCycle(id!), onSuccess: invalidate })
  const closeMut = useMutation({
    mutationFn: () => {
      const input: CloseCycleInput = {
        passed: c.passed,
        temperatureC: c.temperatureC ? Number.parseFloat(c.temperatureC) : null,
        pressureBar: c.pressureBar ? Number.parseFloat(c.pressureBar) : null,
        durationMin: c.durationMin ? Number.parseInt(c.durationMin, 10) : null,
        resultNotes: c.resultNotes.trim() || null,
      }
      return closeCycle(id!, input)
    },
    onSuccess: () => { invalidate(); setCloseOpen(false); setC(emptyClose) },
  })

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('sterilization.cycleDetail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('sterilization.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('sterilization.loadError')}</div></div>

  const { cycle: cy, load } = q.data
  const fmt = (v: string | null) => (v ? new Date(v).toLocaleString() : null)
  const open = cy.status === 'Planned' || cy.status === 'Running'
  const isPlanned = cy.status === 'Planned'

  const rows: FieldRow[] = [
    { label: t('sterilization.fields.autoclave'), value: cy.autoclave },
    { label: t('sterilization.fields.cycleNumber'), value: cy.cycleNumber },
    { label: t('sterilization.fields.program'), value: cy.program },
    { label: t('sterilization.fields.status'), value: <Badge appearance="tint" color={cycleStatusColor(cy.status)}>{t(`sterilization.cycleStatuses.${cy.status}`)}</Badge> },
    { label: t('sterilization.fields.operator'), value: cy.operator },
    { label: t('sterilization.fields.temperature'), value: cy.temperatureC != null ? `${cy.temperatureC} °C` : null },
    { label: t('sterilization.fields.pressure'), value: cy.pressureBar != null ? `${cy.pressureBar} bar` : null },
    { label: t('sterilization.fields.duration'), value: cy.durationMin != null ? `${cy.durationMin} min` : null },
    { label: t('sterilization.fields.startedOn'), value: fmt(cy.startedOnUtc) },
    { label: t('sterilization.fields.endedOn'), value: fmt(cy.endedOnUtc) },
    { label: t('sterilization.fields.resultNotes'), value: cy.resultNotes },
  ]
  const crumbs: Crumb[] = [
    { label: t('nav.infrastructure'), to: '/admin' },
    { label: t('sterilization.title'), to: '/admin/sterilization' },
  ]

  return (
    <div className={styles.root}>
      <DetailHeader title={`${cy.autoclave} · ${cy.program}`} crumbs={crumbs} />

      <div className={styles.actions}>
        {isPlanned && <Button appearance="primary" icon={<PlayCircle20Regular />} disabled={load.length === 0 || startMut.isPending} onClick={() => startMut.mutate()}>{t('sterilization.actions.start')}</Button>}
        {open && <Button appearance="primary" icon={<CheckmarkCircle20Regular />} onClick={() => { setC(emptyClose); setCloseOpen(true) }}>{t('sterilization.actions.close')}</Button>}
      </div>
      {startMut.isError && <span className={styles.error}>{(startMut.error as Error).message}</span>}

      <FieldList rows={rows} />

      <div className={styles.section}>
        <div className={styles.toolbar}>
          <span className={styles.sectionTitle}>{t('sterilization.load.title')}</span>
          {isPlanned && <Button size="small" icon={<Add20Regular />} onClick={() => { setPickSetId(''); setAddOpen(true) }}>{t('sterilization.load.add')}</Button>}
        </div>
        {removeMut.isError && <span className={styles.error}>{(removeMut.error as Error).message}</span>}
        {load.length === 0 ? (
          <div className={styles.muted}>{t('sterilization.load.empty')}</div>
        ) : (
          <Table aria-label={t('sterilization.load.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('sterilization.fields.code')}</TableHeaderCell>
                <TableHeaderCell>{t('sterilization.fields.name')}</TableHeaderCell>
                <TableHeaderCell>{t('sterilization.fields.status')}</TableHeaderCell>
                {isPlanned && <TableHeaderCell>{t('sterilization.fields.actions')}</TableHeaderCell>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {load.map((ls) => (
                <TableRow key={ls.setId}>
                  <TableCell><TableCellLayout>{ls.code}</TableCellLayout></TableCell>
                  <TableCell><TableCellLayout>{ls.name}</TableCellLayout></TableCell>
                  <TableCell><Badge appearance="tint" color={setStatusColor(ls.status)}>{t(`sterilization.setStatuses.${ls.status}`)}</Badge></TableCell>
                  {isPlanned && (
                    <TableCell>
                      <Button size="small" appearance="subtle" icon={<Delete20Regular />} aria-label={t('sterilization.load.remove')}
                        disabled={removeMut.isPending} onClick={() => removeMut.mutate(ls.setId)} />
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>

      <Dialog open={addOpen} onOpenChange={(_, d) => setAddOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('sterilization.load.add')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <Field label={t('sterilization.load.pickSet')} required>
                  <Dropdown value={pickLabel} selectedOptions={[pickSetId]} placeholder={t('sterilization.load.pickPlaceholder')}
                    onOptionSelect={(_, d) => setPickSetId(d.optionValue ?? '')}>
                    {eligible.isLoading
                      ? <Option value="" disabled>{t('sterilization.loading')}</Option>
                      : eligibleOptions.length === 0
                        ? <Option value="" disabled>{t('sterilization.load.noneEligible')}</Option>
                        : eligibleOptions.map((o) => <Option key={o.id} value={o.id}>{o.label}</Option>)}
                  </Dropdown>
                </Field>
                <div className={styles.muted}>{t('sterilization.load.eligibleHint')}</div>
                {addMut.isError && <span className={styles.error}>{(addMut.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setAddOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={pickSetId === '' || addMut.isPending} onClick={() => addMut.mutate()}>{t('sterilization.load.add')}</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <Dialog open={closeOpen} onOpenChange={(_, d) => setCloseOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('sterilization.close.title')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <Checkbox label={t('sterilization.close.passed')} checked={c.passed} onChange={(_, d) => setField('passed', !!d.checked)} />
                <div className={styles.muted}>{c.passed ? t('sterilization.close.passedHint') : t('sterilization.close.failedHint')}</div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('sterilization.fields.temperature')}>
                    <Input type="number" value={c.temperatureC} onChange={(_, d) => setField('temperatureC', d.value)} contentAfter="°C" />
                  </Field>
                  <Field className={styles.grow} label={t('sterilization.fields.pressure')}>
                    <Input type="number" value={c.pressureBar} onChange={(_, d) => setField('pressureBar', d.value)} contentAfter="bar" />
                  </Field>
                  <Field className={styles.grow} label={t('sterilization.fields.duration')}>
                    <Input type="number" value={c.durationMin} onChange={(_, d) => setField('durationMin', d.value)} contentAfter="min" />
                  </Field>
                </div>
                <Field label={t('sterilization.fields.resultNotes')}>
                  <Textarea value={c.resultNotes} onChange={(_, d) => setField('resultNotes', d.value)} resize="vertical" />
                </Field>
                {closeMut.isError && <span className={styles.error}>{(closeMut.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setCloseOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={closeMut.isPending} onClick={() => closeMut.mutate()}>{t('sterilization.actions.close')}</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
