import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Input, Option, Spinner, Textarea,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { Add20Regular } from '@fluentui/react-icons'
import { RowActions } from '@shared/components'
import { getSpacesByFacility } from '@features/space/api'
import {
  createMovement, deleteMovement, getMovements, movementKeys, type MovementInput,
} from '../api'

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM },
  toolbar: { display: 'flex', justifyContent: 'flex-end' },
  state: { padding: tokens.spacingVerticalXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  muted: { color: tokens.colorNeutralForeground3 },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '460px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1, minWidth: 0 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
})

const NONE = '__none__'
const today = () => new Date().toISOString().slice(0, 10)

export function MovementsTab({ equipmentId, facilityId, currentSpaceId }: { equipmentId: string; facilityId: string; currentSpaceId: string | null }) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)

  const list = useQuery({ queryKey: movementKeys.all(equipmentId), queryFn: () => getMovements(equipmentId) })
  const spaces = useQuery({ queryKey: ['spaces', 'byFacility', facilityId], queryFn: () => getSpacesByFacility(facilityId), enabled: open && !!facilityId })
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: movementKeys.all(equipmentId) })
    void queryClient.invalidateQueries({ queryKey: ['equipment', 'item', equipmentId] })
  }

  const [movedOn, setMovedOn] = useState(today())
  const [fromSpaceId, setFromSpaceId] = useState('')
  const [toSpaceId, setToSpaceId] = useState('')
  const [reason, setReason] = useState('')
  const [performedBy, setPerformedBy] = useState('')
  const [notes, setNotes] = useState('')

  useEffect(() => {
    if (!open) return
    setMovedOn(today()); setFromSpaceId(currentSpaceId ?? ''); setToSpaceId(''); setReason(''); setPerformedBy(''); setNotes('')
  }, [open, currentSpaceId])

  const spaceName = (id: string) => spaces.data?.items.find((s) => s.id === id)?.name ?? ''

  const mutation = useMutation({
    mutationFn: () => {
      const input: MovementInput = {
        movedOn,
        fromSpaceId: fromSpaceId || null,
        toSpaceId: toSpaceId || null,
        reason: reason.trim() || null,
        performedBy: performedBy.trim() || null,
        notes: notes.trim() || null,
      }
      return createMovement(equipmentId, input)
    },
    onSuccess: () => { invalidate(); setOpen(false) },
  })

  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <Button appearance="primary" icon={<Add20Regular />} onClick={() => setOpen(true)}>{t('equipment.movements.new')}</Button>
      </div>

      {list.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('equipment.loading')} /></div>
      ) : (list.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('equipment.movements.empty')}</div>
      ) : (
        <Table aria-label={t('equipment.tabs.movements')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('equipment.movements.fields.movedOn')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.movements.fields.from')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.movements.fields.to')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.movements.fields.reason')}</TableHeaderCell>
              <TableHeaderCell>{t('equipment.movements.fields.performedBy')}</TableHeaderCell>
              <TableHeaderCell>{t('adminRef.fields.actions')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(list.data?.items ?? []).map((m) => (
              <TableRow key={m.id}>
                <TableCell><TableCellLayout>{m.movedOn}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{m.fromSpaceName ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{m.toSpaceName ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{m.reason ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{m.performedBy ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell>
                  <RowActions
                    primary={[]}
                    onDelete={() => deleteMovement(m.id).then(invalidate)}
                    confirmTitle={t('actions.confirmDeleteTitle')}
                    confirmMessage={t('equipment.movements.confirmDelete')}
                  />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <Dialog open={open} onOpenChange={(_, d) => setOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('equipment.movements.new')}</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <Field label={t('equipment.movements.fields.movedOn')} required>
                  <Input type="date" value={movedOn} onChange={(_, d) => setMovedOn(d.value)} />
                </Field>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('equipment.movements.fields.from')}>
                    <Dropdown value={fromSpaceId ? spaceName(fromSpaceId) : t('equipment.form.unassigned')}
                      selectedOptions={fromSpaceId ? [fromSpaceId] : [NONE]}
                      onOptionSelect={(_, d) => setFromSpaceId(d.optionValue === NONE ? '' : (d.optionValue ?? ''))}>
                      <Option value={NONE}>{t('equipment.form.unassigned')}</Option>
                      {(spaces.data?.items ?? []).map((s) => <Option key={s.id} value={s.id}>{`${s.name} (${s.code})`}</Option>)}
                    </Dropdown>
                  </Field>
                  <Field className={styles.grow} label={t('equipment.movements.fields.to')}>
                    <Dropdown value={toSpaceId ? spaceName(toSpaceId) : t('equipment.form.unassigned')}
                      selectedOptions={toSpaceId ? [toSpaceId] : [NONE]}
                      onOptionSelect={(_, d) => setToSpaceId(d.optionValue === NONE ? '' : (d.optionValue ?? ''))}>
                      <Option value={NONE}>{t('equipment.form.unassigned')}</Option>
                      {(spaces.data?.items ?? []).map((s) => <Option key={s.id} value={s.id}>{`${s.name} (${s.code})`}</Option>)}
                    </Dropdown>
                  </Field>
                </div>
                <Field label={t('equipment.movements.fields.reason')}>
                  <Input value={reason} onChange={(_, d) => setReason(d.value)} />
                </Field>
                <Field label={t('equipment.movements.fields.performedBy')}>
                  <Input value={performedBy} onChange={(_, d) => setPerformedBy(d.value)} />
                </Field>
                <Field label={t('equipment.fields.notes')}>
                  <Textarea value={notes} onChange={(_, d) => setNotes(d.value)} resize="vertical" />
                </Field>
                {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={mutation.isPending} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined} onClick={() => mutation.mutate()}>
                {t('actions.save')}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
