import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Button, Checkbox, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Field, Input, Spinner, makeStyles, tokens,
} from '@fluentui/react-components'
import {
  createReferenceValue, updateReferenceValue, type ReferenceValueAdmin,
} from '@features/reference/api'

const useStyles = makeStyles({
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '420px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
})

type Props = {
  open: boolean
  onOpenChange: (open: boolean) => void
  category: string
  editing: ReferenceValueAdmin | null
}

export function ReferenceValueDialog({ open, onOpenChange, category, editing }: Props) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const isEdit = editing != null

  const [code, setCode] = useState('')
  const [labelFr, setLabelFr] = useState('')
  const [labelEn, setLabelEn] = useState('')
  const [displayOrder, setDisplayOrder] = useState('0')
  const [active, setActive] = useState(true)

  useEffect(() => {
    if (!open) return
    setCode(editing?.code ?? '')
    setLabelFr(editing?.labelFr ?? '')
    setLabelEn(editing?.labelEn ?? '')
    setDisplayOrder(String(editing?.displayOrder ?? 0))
    setActive(editing?.active ?? true)
  }, [open, editing])

  const mutation = useMutation({
    mutationFn: async () => {
      const order = Number.parseInt(displayOrder, 10) || 0
      if (isEdit) await updateReferenceValue(category, editing!.code, { labelFr, labelEn, displayOrder: order, active })
      else await createReferenceValue({ category, code, labelFr, labelEn, displayOrder: order, active })
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['reference', 'admin'] })
      void queryClient.invalidateQueries({ queryKey: ['reference', 'values', category] })
      onOpenChange(false)
    },
  })

  const canSubmit = code.trim() !== '' && labelFr.trim() !== '' && labelEn.trim() !== '' && !mutation.isPending

  return (
    <Dialog open={open} onOpenChange={(_, d) => onOpenChange(d.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{t(isEdit ? 'adminRef.values.edit' : 'adminRef.values.new')} · {category}</DialogTitle>
          <DialogContent>
            <div className={styles.form}>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('adminRef.fields.code')} required>
                  <Input value={code} disabled={isEdit} onChange={(_, d) => setCode(d.value)} placeholder="EX: HOSPITAL" />
                </Field>
                <Field label={t('adminRef.fields.displayOrder')}>
                  <Input type="number" value={displayOrder} onChange={(_, d) => setDisplayOrder(d.value)} style={{ width: '110px' }} />
                </Field>
              </div>
              <Field label={t('adminRef.fields.labelFr')} required>
                <Input value={labelFr} onChange={(_, d) => setLabelFr(d.value)} />
              </Field>
              <Field label={t('adminRef.fields.labelEn')} required>
                <Input value={labelEn} onChange={(_, d) => setLabelEn(d.value)} />
              </Field>
              <Checkbox label={t('adminRef.fields.active')} checked={active} onChange={(_, d) => setActive(!!d.checked)} />
              {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={() => onOpenChange(false)}>{t('actions.cancel')}</Button>
            <Button appearance="primary" disabled={!canSubmit} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined} onClick={() => mutation.mutate()}>
              {t('actions.save')}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
