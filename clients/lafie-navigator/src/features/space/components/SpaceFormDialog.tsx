import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Dropdown,
  Field,
  Input,
  Option,
  Textarea,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { Dismiss24Regular } from '@fluentui/react-icons'
import { SPACE_TYPES, createSpace, spaceKeys, updateSpace, type Space, type SpaceType } from '../api'

const useStyles = makeStyles({
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, paddingTop: tokens.spacingVerticalS },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM, '@media (max-width: 520px)': { flexDirection: 'column' } },
  grow: { flex: 1, minWidth: 0 },
  error: { color: tokens.colorPaletteRedForeground1 },
})

const schema = z.object({
  code: z.string().trim().min(1).max(50),
  name: z.string().trim().min(1).max(200),
  type: z.enum(SPACE_TYPES),
  area: z.string().trim().regex(/^\d+([.,]\d+)?$/).optional().or(z.literal('')),
  maxCapacity: z.string().trim().regex(/^\d+$/).optional().or(z.literal('')),
  accessible: z.boolean(),
  description: z.string().trim().max(1000).optional().or(z.literal('')),
})
type FormValues = z.infer<typeof schema>
const empty: FormValues = { code: '', name: '', type: 'PatientRoom', area: '', maxCapacity: '', accessible: false, description: '' }

export function SpaceFormDialog({
  open,
  onOpenChange,
  floorId,
  space,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  floorId: string
  space: Space | null
}) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const isEdit = space !== null

  const { handleSubmit, setValue, watch, reset, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: empty,
  })

  useEffect(() => {
    if (!open) return
    reset(space
      ? {
          code: space.code, name: space.name, type: space.type,
          area: space.area === null ? '' : String(space.area),
          maxCapacity: space.maxCapacity === null ? '' : String(space.maxCapacity),
          accessible: space.accessible, description: space.description ?? '',
        }
      : empty)
  }, [open, space, reset])

  const mutation = useMutation({
    mutationFn: (v: FormValues) => {
      const payload = {
        code: v.code, name: v.name, type: v.type,
        area: v.area ? Number(v.area.replace(',', '.')) : null,
        maxCapacity: v.maxCapacity ? Number(v.maxCapacity) : null,
        accessible: v.accessible,
        description: v.description || null,
      }
      return isEdit ? updateSpace(space!.id, payload) : createSpace(floorId, payload).then(() => undefined)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: spaceKeys.all(floorId) })
      onOpenChange(false)
    },
  })

  const v = watch()
  const set = (key: 'code' | 'name' | 'area' | 'maxCapacity' | 'description') => ({
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
              {isEdit ? t('space.edit') : t('space.new')}
            </DialogTitle>
            <DialogContent className={styles.form}>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('space.fields.code')} required
                  validationMessage={formState.errors.code ? t('space.validation.code') : undefined}>
                  <Input {...set('code')} />
                </Field>
                <Field className={styles.grow} label={t('space.fields.name')} required
                  validationMessage={formState.errors.name ? t('space.validation.name') : undefined}>
                  <Input {...set('name')} />
                </Field>
              </div>
              <Field label={t('space.fields.type')} required>
                <Dropdown
                  value={t(`space.types.${v.type}`)}
                  selectedOptions={[v.type]}
                  onOptionSelect={(_, d) => setValue('type', d.optionValue as SpaceType)}
                >
                  {SPACE_TYPES.map((ty) => <Option key={ty} value={ty}>{t(`space.types.${ty}`)}</Option>)}
                </Dropdown>
              </Field>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('space.fields.area')}
                  validationMessage={formState.errors.area ? t('space.validation.area') : undefined}>
                  <Input {...set('area')} contentAfter="m²" />
                </Field>
                <Field className={styles.grow} label={t('space.fields.maxCapacity')}
                  validationMessage={formState.errors.maxCapacity ? t('space.validation.maxCapacity') : undefined}>
                  <Input {...set('maxCapacity')} type="number" />
                </Field>
              </div>
              <Checkbox checked={v.accessible} label={t('space.fields.accessible')}
                onChange={(_, d) => setValue('accessible', !!d.checked)} />
              <Field label={t('space.fields.description')}>
                <Textarea {...set('description')} resize="vertical" />
              </Field>
              {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" type="button" onClick={() => onOpenChange(false)}>{t('space.cancel')}</Button>
              <Button appearance="primary" type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('space.saving') : t('space.save')}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
