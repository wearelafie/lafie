import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  Textarea,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { Dismiss24Regular } from '@fluentui/react-icons'
import { createFloor, floorKeys, updateFloor, type Floor } from '../api'

const useStyles = makeStyles({
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, paddingTop: tokens.spacingVerticalS },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM, '@media (max-width: 520px)': { flexDirection: 'column' } },
  grow: { flex: 1, minWidth: 0 },
  error: { color: tokens.colorPaletteRedForeground1 },
})

const schema = z.object({
  code: z.string().trim().min(1).max(50),
  name: z.string().trim().min(1).max(200),
  level: z.string().trim().regex(/^-?\d+$/).optional().or(z.literal('')),
  displayOrder: z.string().trim().regex(/^\d+$/).optional().or(z.literal('')),
  description: z.string().trim().max(1000).optional().or(z.literal('')),
})
type FormValues = z.infer<typeof schema>
const empty: FormValues = { code: '', name: '', level: '', displayOrder: '0', description: '' }

export function FloorFormDialog({
  open,
  onOpenChange,
  buildingId,
  floor,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  buildingId: string
  floor: Floor | null
}) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const isEdit = floor !== null

  const { handleSubmit, setValue, watch, reset, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: empty,
  })

  useEffect(() => {
    if (!open) return
    reset(floor
      ? {
          code: floor.code,
          name: floor.name,
          level: floor.level === null ? '' : String(floor.level),
          displayOrder: String(floor.displayOrder),
          description: floor.description ?? '',
        }
      : empty)
  }, [open, floor, reset])

  const mutation = useMutation({
    mutationFn: (v: FormValues) => {
      const payload = {
        code: v.code,
        name: v.name,
        level: v.level === '' ? null : Number(v.level),
        displayOrder: v.displayOrder === '' ? 0 : Number(v.displayOrder),
        description: v.description || null,
      }
      return isEdit ? updateFloor(floor!.id, payload) : createFloor(buildingId, payload).then(() => undefined)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: floorKeys.all(buildingId) })
      onOpenChange(false)
    },
  })

  const v = watch()
  const set = (key: keyof FormValues) => ({
    value: (v[key] ?? '') as string,
    onChange: (_: unknown, d: { value: string }) => setValue(key, d.value, { shouldValidate: true }),
  })
  const onSubmit = handleSubmit((values) => mutation.mutate(values))

  return (
    <Dialog open={open} onOpenChange={(_, d) => onOpenChange(d.open)}>
      <DialogSurface>
        <form onSubmit={onSubmit}>
          <DialogBody>
            <DialogTitle
              action={<Button appearance="subtle" icon={<Dismiss24Regular />} aria-label={t('settings.close')} onClick={() => onOpenChange(false)} />}
            >
              {isEdit ? t('floor.edit') : t('floor.new')}
            </DialogTitle>
            <DialogContent className={styles.form}>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('floor.fields.code')} required
                  validationMessage={formState.errors.code ? t('floor.validation.code') : undefined}>
                  <Input {...set('code')} />
                </Field>
                <Field className={styles.grow} label={t('floor.fields.name')} required
                  validationMessage={formState.errors.name ? t('floor.validation.name') : undefined}>
                  <Input {...set('name')} />
                </Field>
              </div>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('floor.fields.level')}
                  validationMessage={formState.errors.level ? t('floor.validation.level') : undefined}
                  hint={t('floor.fields.levelHint')}>
                  <Input {...set('level')} type="number" />
                </Field>
                <Field className={styles.grow} label={t('floor.fields.displayOrder')}>
                  <Input {...set('displayOrder')} type="number" />
                </Field>
              </div>
              <Field label={t('floor.fields.description')}>
                <Textarea {...set('description')} resize="vertical" />
              </Field>
              {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" type="button" onClick={() => onOpenChange(false)}>{t('floor.cancel')}</Button>
              <Button appearance="primary" type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('floor.saving') : t('floor.save')}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
