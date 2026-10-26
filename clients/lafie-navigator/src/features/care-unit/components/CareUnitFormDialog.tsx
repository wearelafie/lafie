import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Input, Option, Textarea, makeStyles, tokens,
} from '@fluentui/react-components'
import { Dismiss24Regular } from '@fluentui/react-icons'
import { CARE_UNIT_TYPES, careUnitKeys, createCareUnit, updateCareUnit, type CareUnit } from '../api'

const useStyles = makeStyles({
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, paddingTop: tokens.spacingVerticalS },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM, '@media (max-width: 520px)': { flexDirection: 'column' } },
  grow: { flex: 1, minWidth: 0 },
  error: { color: tokens.colorPaletteRedForeground1 },
})

const schema = z.object({
  code: z.string().trim().min(1).max(50),
  name: z.string().trim().min(1).max(200),
  type: z.enum(CARE_UNIT_TYPES),
  bedCapacity: z.string().trim().regex(/^\d+$/).optional().or(z.literal('')),
  displayOrder: z.string().trim().regex(/^\d+$/).optional().or(z.literal('')),
  description: z.string().trim().max(1000).optional().or(z.literal('')),
})
type FormValues = z.infer<typeof schema>
const empty: FormValues = { code: '', name: '', type: 'Hospitalization', bedCapacity: '', displayOrder: '0', description: '' }

export function CareUnitFormDialog({
  open, onOpenChange, serviceId, careUnit,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  serviceId: string
  careUnit: CareUnit | null
}) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const isEdit = careUnit !== null

  const { handleSubmit, setValue, watch, reset, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: empty,
  })

  useEffect(() => {
    if (!open) return
    reset(careUnit
      ? {
          code: careUnit.code,
          name: careUnit.name,
          type: careUnit.type,
          bedCapacity: careUnit.bedCapacity === null ? '' : String(careUnit.bedCapacity),
          displayOrder: String(careUnit.displayOrder),
          description: careUnit.description ?? '',
        }
      : empty)
  }, [open, careUnit, reset])

  const mutation = useMutation({
    mutationFn: (vals: FormValues) => {
      const payload = {
        code: vals.code,
        name: vals.name,
        type: vals.type,
        bedCapacity: vals.bedCapacity === '' ? null : Number(vals.bedCapacity),
        displayOrder: vals.displayOrder === '' ? 0 : Number(vals.displayOrder),
        description: vals.description || null,
      }
      return isEdit ? updateCareUnit(careUnit!.id, payload) : createCareUnit(serviceId, payload).then(() => undefined)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: careUnitKeys.all(serviceId) })
      onOpenChange(false)
    },
  })

  const v = watch()
  const set = (key: 'code' | 'name' | 'bedCapacity' | 'displayOrder' | 'description') => ({
    value: (v[key] ?? '') as string,
    onChange: (_: unknown, d: { value: string }) => setValue(key, d.value, { shouldValidate: true }),
  })
  const onSubmit = handleSubmit((values) => mutation.mutate(values))

  return (
    <Dialog open={open} onOpenChange={(_, d) => onOpenChange(d.open)}>
      <DialogSurface>
        <form onSubmit={onSubmit}>
          <DialogBody>
            <DialogTitle action={<Button appearance="subtle" icon={<Dismiss24Regular />} aria-label={t('settings.close')} onClick={() => onOpenChange(false)} />}>
              {isEdit ? t('careUnit.edit') : t('careUnit.new')}
            </DialogTitle>
            <DialogContent className={styles.form}>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('careUnit.fields.code')} required
                  validationMessage={formState.errors.code ? t('careUnit.validation.code') : undefined}>
                  <Input {...set('code')} />
                </Field>
                <Field className={styles.grow} label={t('careUnit.fields.name')} required
                  validationMessage={formState.errors.name ? t('careUnit.validation.name') : undefined}>
                  <Input {...set('name')} />
                </Field>
              </div>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('careUnit.fields.type')} required>
                  <Dropdown
                    value={t(`careUnit.types.${v.type}`)}
                    selectedOptions={[v.type]}
                    onOptionSelect={(_, d) => setValue('type', d.optionValue as FormValues['type'], { shouldValidate: true })}>
                    {CARE_UNIT_TYPES.map((ty) => <Option key={ty} value={ty}>{t(`careUnit.types.${ty}`)}</Option>)}
                  </Dropdown>
                </Field>
                <Field className={styles.grow} label={t('careUnit.fields.bedCapacity')}>
                  <Input {...set('bedCapacity')} type="number" />
                </Field>
                <Field className={styles.grow} label={t('careUnit.fields.displayOrder')}>
                  <Input {...set('displayOrder')} type="number" />
                </Field>
              </div>
              <Field label={t('careUnit.fields.description')}>
                <Textarea {...set('description')} resize="vertical" />
              </Field>
              {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" type="button" onClick={() => onOpenChange(false)}>{t('careUnit.cancel')}</Button>
              <Button appearance="primary" type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('careUnit.saving') : t('careUnit.save')}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
