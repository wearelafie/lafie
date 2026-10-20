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
import { buildingKeys, createBuilding, updateBuilding, type Building } from '../api'

const useStyles = makeStyles({
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, paddingTop: tokens.spacingVerticalS },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM, '@media (max-width: 520px)': { flexDirection: 'column' } },
  grow: { flex: 1, minWidth: 0 },
  error: { color: tokens.colorPaletteRedForeground1 },
})

const schema = z.object({
  code: z.string().trim().min(1).max(50),
  name: z.string().trim().min(1).max(200),
  location: z.string().trim().max(200).optional().or(z.literal('')),
  description: z.string().trim().max(1000).optional().or(z.literal('')),
})
type FormValues = z.infer<typeof schema>
const empty: FormValues = { code: '', name: '', location: '', description: '' }

export function BuildingFormDialog({
  open,
  onOpenChange,
  facilityId,
  building,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  facilityId: string
  building: Building | null
}) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const isEdit = building !== null

  const { handleSubmit, setValue, watch, reset, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: empty,
  })

  useEffect(() => {
    if (!open) return
    reset(building
      ? { code: building.code, name: building.name, location: building.location ?? '', description: building.description ?? '' }
      : empty)
  }, [open, building, reset])

  const mutation = useMutation({
    mutationFn: (v: FormValues) => {
      const payload = { code: v.code, name: v.name, location: v.location || null, description: v.description || null }
      return isEdit ? updateBuilding(building!.id, payload) : createBuilding(facilityId, payload).then(() => undefined)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: buildingKeys.all(facilityId) })
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
              {isEdit ? t('building.edit') : t('building.new')}
            </DialogTitle>
            <DialogContent className={styles.form}>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('building.fields.code')} required
                  validationMessage={formState.errors.code ? t('building.validation.code') : undefined}>
                  <Input {...set('code')} />
                </Field>
                <Field className={styles.grow} label={t('building.fields.name')} required
                  validationMessage={formState.errors.name ? t('building.validation.name') : undefined}>
                  <Input {...set('name')} />
                </Field>
              </div>
              <Field label={t('building.fields.location')}>
                <Input {...set('location')} />
              </Field>
              <Field label={t('building.fields.description')}>
                <Textarea {...set('description')} resize="vertical" />
              </Field>
              {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" type="button" onClick={() => onOpenChange(false)}>{t('building.cancel')}</Button>
              <Button appearance="primary" type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('building.saving') : t('building.save')}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
