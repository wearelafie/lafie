import { useEffect, useState, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Input, Option, Spinner, Textarea,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Edit20Regular, CheckmarkCircle20Regular, PlayCircle20Regular, DismissCircle20Regular } from '@fluentui/react-icons'
import { RowActions, type RowAction } from '@shared/components'
import {
  MAINTENANCE_STATUSES, MAINTENANCE_TYPES, createMaintenance, deleteMaintenance, getMaintenance,
  maintenanceKeys, setMaintenanceStatus, updateMaintenance,
  type EquipmentMaintenance, type MaintenanceInput, type MaintenanceStatus,
} from '../api'

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM },
  toolbar: { display: 'flex', justifyContent: 'flex-end' },
  state: { padding: tokens.spacingVerticalXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  muted: { color: tokens.colorNeutralForeground3 },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '480px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1, minWidth: 0 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
})

const statusColor = (s: string): 'informative' | 'warning' | 'success' | 'danger' =>
  s === 'Planned' ? 'informative' : s === 'InProgress' ? 'warning' : s === 'Completed' ? 'success' : 'danger'
const typeColor = (ty: string): 'brand' | 'danger' => (ty === 'Preventive' ? 'brand' : 'danger')

type FormState = { type: string; status: string; scheduledOn: string; performedOn: string; provider: string; cost: string; description: string; notes: string }
const empty: FormState = { type: 'Preventive', status: 'Planned', scheduledOn: '', performedOn: '', provider: '', cost: '', description: '', notes: '' }

export function MaintenanceTab({ equipmentId }: { equipmentId: string }) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<EquipmentMaintenance | null>(null)
  const [f, setF] = useState<FormState>(empty)
  const set = <K extends keyof FormState>(k: K, v: FormState[K]) => setF((s) => ({ ...s, [k]: v }))

  const list = useQuery({ queryKey: maintenanceKeys.all(equipmentId), queryFn: () => getMaintenance(equipmentId) })
  const invalidate = () => queryClient.invalidateQueries({ queryKey: maintenanceKeys.all(equipmentId) })
  const statusMut = useMutation({ mutationFn: ({ id, status }: { id: string; status: MaintenanceStatus }) => setMaintenanceStatus(id, status), onSuccess: invalidate })

  const openNew = () => { setEditing(null); setF(empty); setOpen(true) }
  const openEdit = (m: EquipmentMaintenance) => {
    setEditing(m)
    setF({ type: m.type, status: m.status, scheduledOn: m.scheduledOn ?? '', performedOn: m.performedOn ?? '', provider: m.provider ?? '', cost: m.cost != null ? String(m.cost) : '', description: m.description, notes: m.notes ?? '' })
    setOpen(true)
  }

  useEffect(() => { if (!open) setEditing(null) }, [open])

  const mutation = useMutation({
    mutationFn: async () => {
      const cost = f.cost.trim() === '' ? null : Number.parseFloat(f.cost.replace(',', '.'))
      const base: MaintenanceInput = {
        type: f.type,
        scheduledOn: f.scheduledOn || null,
        performedOn: f.performedOn || null,
        provider: f.provider.trim() || null,
        cost: Number.isNaN(cost as number) ? null : cost,
        description: f.description.trim(),
        notes: f.notes.trim() || null,
      }
      if (editing) await updateMaintenance(editing.id, base)
      else await createMaintenance(equipmentId, { ...base, status: f.status })
    },
    onSuccess: () => { invalidate(); setOpen(false) },
  })

  const canSubmit = f.description.trim() !== '' && !mutation.isPending

  const statusIcon: Record<MaintenanceStatus, ReactElement> = {
    Planned: <PlayCircle20Regular />, InProgress: <PlayCircle20Regular />, Completed: <CheckmarkCircle20Regular />, Cancelled: <DismissCircle20Regular />,
  }
  const rowActions = (m: EquipmentMaintenance) => {
    const primary: RowAction[] = [{ key: 'edit', label: t('actions.edit'), icon: <Edit20Regular />, onClick: () => openEdit(m) }]
    const menu: RowAction[] = MAINTENANCE_STATUSES.filter((s) => s !== m.status).map((s) => ({
      key: `st-${s}`, label: t('equipment.maintenance.setStatus', { status: t(`equipment.maintenance.statuses.${s}`) }), icon: statusIcon[s],
      onClick: () => statusMut.mutate({ id: m.id, status: s }),
    }))
    return <RowActions primary={primary} menu={menu} onDelete={() => deleteMaintenance(m.id).then(invalidate)}
      confirmTitle={t('actions.confirmDeleteTitle')} confirmMessage={t('equipment.maintenance.confirmDelete')} />
  }

  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <Button appearance="primary" icon={<Add20Regular />} onClick={openNew}>{t('equipment.maintenance.new')}</Button>
      </div>

      {list.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('equipment.loading')} /></div>
      ) : (list.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('equipment.maintenance.empty')}</div>
      ) : (
        <Table aria-label={t('equipment.tabs.maintenance')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('equipment.maintenance.fields.type')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.maintenance.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.maintenance.fields.scheduledOn')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.maintenance.fields.performedOn')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.maintenance.fields.provider')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.maintenance.fields.description')}</TableHeaderCell>
              <TableHeaderCell>{t('adminRef.fields.actions')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(list.data?.items ?? []).map((m) => (
              <TableRow key={m.id}>
                <TableCell><Badge appearance="tint" color={typeColor(m.type)}>{t(`equipment.maintenance.types.${m.type}`)}</Badge></TableCell>
                <TableCell><Badge appearance="tint" color={statusColor(m.status)}>{t(`equipment.maintenance.statuses.${m.status}`)}</Badge></TableCell>
                <TableCell><TableCellLayout>{m.scheduledOn ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{m.performedOn ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{m.provider ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout truncate>{m.description}</TableCellLayout></TableCell>
                <TableCell>{rowActions(m)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <Dialog open={open} onOpenChange={(_, d) => setOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t(editing ? 'equipment.maintenance.edit' : 'equipment.maintenance.new')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('equipment.maintenance.fields.type')} required>
                    <Dropdown value={t(`equipment.maintenance.types.${f.type}`)} selectedOptions={[f.type]}
                      onOptionSelect={(_, d) => set('type', d.optionValue ?? 'Preventive')}>
                      {MAINTENANCE_TYPES.map((ty) => <Option key={ty} value={ty}>{t(`equipment.maintenance.types.${ty}`)}</Option>)}
                    </Dropdown>
                  </Field>
                  {!editing && (
                    <Field className={styles.grow} label={t('equipment.maintenance.fields.status')}>
                      <Dropdown value={t(`equipment.maintenance.statuses.${f.status}`)} selectedOptions={[f.status]}
                        onOptionSelect={(_, d) => set('status', d.optionValue ?? 'Planned')}>
                        {MAINTENANCE_STATUSES.map((s) => <Option key={s} value={s}>{t(`equipment.maintenance.statuses.${s}`)}</Option>)}
                      </Dropdown>
                    </Field>
                  )}
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('equipment.maintenance.fields.scheduledOn')}>
                    <Input type="date" value={f.scheduledOn} onChange={(_, d) => set('scheduledOn', d.value)} />
                  </Field>
                  <Field className={styles.grow} label={t('equipment.maintenance.fields.performedOn')}>
                    <Input type="date" value={f.performedOn} onChange={(_, d) => set('performedOn', d.value)} />
                  </Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('equipment.maintenance.fields.provider')}>
                    <Input value={f.provider} onChange={(_, d) => set('provider', d.value)} />
                  </Field>
                  <Field label={t('equipment.maintenance.fields.cost')}>
                    <Input type="number" value={f.cost} onChange={(_, d) => set('cost', d.value)} style={{ width: '140px' }} />
                  </Field>
                </div>
                <Field label={t('equipment.maintenance.fields.description')} required>
                  <Textarea value={f.description} onChange={(_, d) => set('description', d.value)} resize="vertical" />
                </Field>
                <Field label={t('equipment.fields.notes')}>
                  <Textarea value={f.notes} onChange={(_, d) => set('notes', d.value)} resize="vertical" />
                </Field>
                {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={!canSubmit} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined} onClick={() => mutation.mutate()}>
                {t('actions.save')}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
