import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Option, Spinner,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { Add20Regular, CheckmarkCircle20Regular, Delete20Regular, DismissCircle20Regular, VehicleTruck20Regular } from '@fluentui/react-icons'
import { useAppStore } from '@app/store/appStore'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import {
  WASTE_TREATMENTS, addCollectionToSlip, cancelWasteSlip, getWasteCollections, getWasteSlip,
  removeCollectionFromSlip, shipWasteSlip, slipKeys, treatWasteSlip,
  type WasteTreatment,
} from '../api'

const NONE = '__none__'

const streamColor = (s: string): 'danger' | 'warning' | 'informative' | 'subtle' =>
  s === 'Dasri' || s === 'Sharps' || s === 'Anatomical' ? 'danger'
    : s === 'Cytotoxic' || s === 'Chemical' || s === 'Pharmaceutical' ? 'warning'
      : 'subtle'
const slipStatusColor = (s: string): 'informative' | 'warning' | 'success' | 'subtle' =>
  s === 'Draft' ? 'informative' : s === 'Shipped' ? 'warning' : s === 'Treated' ? 'success' : 'subtle'
const collStatusColor = (s: string): 'informative' | 'warning' | 'success' | 'subtle' =>
  s === 'Collected' ? 'informative' : s === 'Assigned' ? 'warning' : s === 'Eliminated' ? 'success' : 'subtle'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  actions: { display: 'flex', columnGap: tokens.spacingHorizontalS, flexWrap: 'wrap' },
  section: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM },
  toolbar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', columnGap: tokens.spacingHorizontalM },
  sectionTitle: { fontWeight: tokens.fontWeightSemibold, fontSize: tokens.fontSizeBase300 },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '440px' },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
})

export function WasteSlipDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const id = useAppStore((s) => s.selectedWasteSlipId)

  const q = useQuery({ queryKey: slipKeys.detail(id ?? ''), queryFn: () => getWasteSlip(id!), enabled: !!id })
  const invalidate = () => { void queryClient.invalidateQueries({ queryKey: slipKeys.detail(id ?? '') }) }

  const [addOpen, setAddOpen] = useState(false)
  const [pickId, setPickId] = useState('')
  const [treatOpen, setTreatOpen] = useState(false)
  const [treatment, setTreatment] = useState<string>(NONE)

  const facilityId = q.data?.slip.facilityId
  const stream = q.data?.slip.stream
  const eligible = useQuery({
    queryKey: ['waste-collections', facilityId, 'eligible', stream],
    queryFn: () => getWasteCollections(facilityId!, { stream, unassignedOnly: true, page: 1, pageSize: 100 }),
    enabled: !!facilityId && !!stream && addOpen,
  })
  const eligibleOptions = (eligible.data?.items ?? []).map((c) => ({
    id: c.id, label: `${t(`waste.streams.${c.stream}`)} · ${c.weightKg} kg${c.spaceName ? ` · ${c.spaceName}` : ''}`,
  }))
  const pickLabel = eligibleOptions.find((o) => o.id === pickId)?.label ?? ''

  const addMut = useMutation({ mutationFn: () => addCollectionToSlip(id!, pickId), onSuccess: () => { invalidate(); setAddOpen(false); setPickId('') } })
  const removeMut = useMutation({ mutationFn: (cid: string) => removeCollectionFromSlip(id!, cid), onSuccess: invalidate })
  const shipMut = useMutation({ mutationFn: () => shipWasteSlip(id!), onSuccess: invalidate })
  const treatMut = useMutation({
    mutationFn: () => treatWasteSlip(id!, treatment === NONE ? null : (treatment as WasteTreatment)),
    onSuccess: () => { invalidate(); setTreatOpen(false); setTreatment(NONE) },
  })
  const cancelMut = useMutation({ mutationFn: () => cancelWasteSlip(id!), onSuccess: invalidate })

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('waste.slipDetail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('waste.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('waste.loadError')}</div></div>

  const { slip: sl, collections } = q.data
  const fmt = (v: string | null) => (v ? new Date(v).toLocaleString() : null)
  const isDraft = sl.status === 'Draft'
  const isShipped = sl.status === 'Shipped'
  const closed = sl.status === 'Treated' || sl.status === 'Cancelled'

  const rows: FieldRow[] = [
    { label: t('waste.fields.reference'), value: sl.reference },
    { label: t('waste.fields.stream'), value: <Badge appearance="tint" color={streamColor(sl.stream)}>{t(`waste.streams.${sl.stream}`)}</Badge> },
    { label: t('waste.fields.status'), value: <Badge appearance="tint" color={slipStatusColor(sl.status)}>{t(`waste.slipStatuses.${sl.status}`)}</Badge> },
    { label: t('waste.fields.carrier'), value: sl.carrier },
    { label: t('waste.fields.destination'), value: sl.destination },
    { label: t('waste.fields.treatment'), value: sl.treatment ? t(`waste.treatments.${sl.treatment}`) : null },
    { label: t('waste.fields.totalWeight'), value: `${isDraft ? sl.liveWeightKg : sl.totalWeightKg} kg` },
    { label: t('waste.fields.shippedOn'), value: fmt(sl.shippedOnUtc) },
    { label: t('waste.fields.treatedOn'), value: fmt(sl.treatedOnUtc) },
    { label: t('waste.fields.notes'), value: sl.notes },
  ]
  const crumbs: Crumb[] = [
    { label: t('nav.infrastructure'), to: '/admin' },
    { label: t('waste.title'), to: '/admin/waste' },
  ]

  return (
    <div className={styles.root}>
      <DetailHeader title={`${t('waste.slipDetail.title')} · ${sl.reference}`} crumbs={crumbs} />

      <div className={styles.actions}>
        {isDraft && <Button appearance="primary" icon={<VehicleTruck20Regular />} disabled={collections.length === 0 || shipMut.isPending} onClick={() => shipMut.mutate()}>{t('waste.actions.ship')}</Button>}
        {isShipped && <Button appearance="primary" icon={<CheckmarkCircle20Regular />} onClick={() => { setTreatment(sl.treatment ?? NONE); setTreatOpen(true) }}>{t('waste.actions.treat')}</Button>}
        {!closed && <Button icon={<DismissCircle20Regular />} disabled={cancelMut.isPending} onClick={() => cancelMut.mutate()}>{t('waste.actions.cancelSlip')}</Button>}
      </div>
      {shipMut.isError && <span className={styles.error}>{(shipMut.error as Error).message}</span>}
      {cancelMut.isError && <span className={styles.error}>{(cancelMut.error as Error).message}</span>}

      <FieldList rows={rows} />

      <div className={styles.section}>
        <div className={styles.toolbar}>
          <span className={styles.sectionTitle}>{t('waste.lines.title')}</span>
          {isDraft && <Button size="small" icon={<Add20Regular />} onClick={() => { setPickId(''); setAddOpen(true) }}>{t('waste.lines.add')}</Button>}
        </div>
        {removeMut.isError && <span className={styles.error}>{(removeMut.error as Error).message}</span>}
        {collections.length === 0 ? (
          <div className={styles.muted}>{t('waste.lines.empty')}</div>
        ) : (
          <Table aria-label={t('waste.lines.title')}>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>{t('waste.fields.weight')}</TableHeaderCell>
                <TableHeaderCell>{t('waste.fields.containers')}</TableHeaderCell>
                <TableHeaderCell>{t('waste.fields.source')}</TableHeaderCell>
                <TableHeaderCell>{t('waste.fields.collectedOn')}</TableHeaderCell>
                <TableHeaderCell>{t('waste.fields.status')}</TableHeaderCell>
                {isDraft && <TableHeaderCell>{t('waste.fields.actions')}</TableHeaderCell>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {collections.map((c) => (
                <TableRow key={c.id}>
                  <TableCell><TableCellLayout>{c.weightKg} kg</TableCellLayout></TableCell>
                  <TableCell><TableCellLayout>{c.containerCount}</TableCellLayout></TableCell>
                  <TableCell><TableCellLayout>{c.spaceName ?? c.spaceCode ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                  <TableCell><TableCellLayout>{c.collectedOn ? new Date(c.collectedOn).toLocaleDateString() : <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                  <TableCell><Badge appearance="tint" color={collStatusColor(c.status)}>{t(`waste.collectionStatuses.${c.status}`)}</Badge></TableCell>
                  {isDraft && (
                    <TableCell>
                      <Button size="small" appearance="subtle" icon={<Delete20Regular />} aria-label={t('waste.lines.remove')}
                        disabled={removeMut.isPending} onClick={() => removeMut.mutate(c.id)} />
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
            <DialogTitle>{t('waste.lines.add')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <Field label={t('waste.lines.pickCollection')} required>
                  <Dropdown value={pickLabel} selectedOptions={[pickId]} placeholder={t('waste.lines.pickPlaceholder')}
                    onOptionSelect={(_, d) => setPickId(d.optionValue ?? '')}>
                    {eligible.isLoading
                      ? <Option value="" disabled>{t('waste.loading')}</Option>
                      : eligibleOptions.length === 0
                        ? <Option value="" disabled>{t('waste.lines.noneEligible')}</Option>
                        : eligibleOptions.map((o) => <Option key={o.id} value={o.id}>{o.label}</Option>)}
                  </Dropdown>
                </Field>
                <div className={styles.muted}>{t('waste.lines.eligibleHint')}</div>
                {addMut.isError && <span className={styles.error}>{(addMut.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setAddOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={pickId === '' || addMut.isPending} onClick={() => addMut.mutate()}>{t('waste.lines.add')}</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <Dialog open={treatOpen} onOpenChange={(_, d) => setTreatOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('waste.actions.treat')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <div className={styles.muted}>{t('waste.treat.hint')}</div>
                <Field label={t('waste.fields.treatment')}>
                  <Dropdown value={treatment === NONE ? t('waste.form.noTreatment') : t(`waste.treatments.${treatment}`)} selectedOptions={[treatment]}
                    onOptionSelect={(_, d) => setTreatment(d.optionValue ?? NONE)}>
                    <Option value={NONE}>{t('waste.form.noTreatment')}</Option>
                    {WASTE_TREATMENTS.map((w) => <Option key={w} value={w}>{t(`waste.treatments.${w}`)}</Option>)}
                  </Dropdown>
                </Field>
                {treatMut.isError && <span className={styles.error}>{(treatMut.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setTreatOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={treatMut.isPending} onClick={() => treatMut.mutate()}>{t('waste.actions.treat')}</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
