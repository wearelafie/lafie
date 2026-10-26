import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Input, Option, Textarea, makeStyles, tokens,
} from '@fluentui/react-components'
import { Dismiss24Regular } from '@fluentui/react-icons'
import {
  SERVICE_CATEGORIES, createService, getServices, serviceKeys, updateService, type Service,
} from '../api'

const useStyles = makeStyles({
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, paddingTop: tokens.spacingVerticalS },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM, '@media (max-width: 520px)': { flexDirection: 'column' } },
  grow: { flex: 1, minWidth: 0 },
  error: { color: tokens.colorPaletteRedForeground1 },
})

const NONE = '__none__'
const schema = z.object({
  code: z.string().trim().min(1).max(50),
  name: z.string().trim().min(1).max(200),
  category: z.enum(SERVICE_CATEGORIES),
  parentServiceId: z.string(),
  head: z.string().trim().max(200).optional().or(z.literal('')),
  displayOrder: z.string().trim().regex(/^\d+$/).optional().or(z.literal('')),
  description: z.string().trim().max(1000).optional().or(z.literal('')),
})
type FormValues = z.infer<typeof schema>
const empty: FormValues = { code: '', name: '', category: 'Clinical', parentServiceId: NONE, head: '', displayOrder: '0', description: '' }

export function ServiceFormDialog({
  open, onOpenChange, facilityId, service,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  facilityId: string
  service: Service | null
}) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const isEdit = service !== null

  const parents = useQuery({
    queryKey: serviceKeys.list(facilityId, { pageSize: 100 }),
    queryFn: () => getServices(facilityId, { pageSize: 100 }),
    enabled: open,
  })

  const { handleSubmit, setValue, watch, reset, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: empty,
  })

  useEffect(() => {
    if (!open) return
    reset(service
      ? {
          code: service.code,
          name: service.name,
          category: service.category,
          parentServiceId: service.parentServiceId ?? NONE,
          head: service.head ?? '',
          displayOrder: String(service.displayOrder),
          description: service.description ?? '',
        }
      : empty)
  }, [open, service, reset])

  const mutation = useMutation({
    mutationFn: (vals: FormValues) => {
      const payload = {
        code: vals.code,
        name: vals.name,
        category: vals.category,
        parentServiceId: vals.parentServiceId === NONE ? null : vals.parentServiceId,
        head: vals.head || null,
        displayOrder: vals.displayOrder === '' ? 0 : Number(vals.displayOrder),
        description: vals.description || null,
      }
      return isEdit ? updateService(service!.id, payload) : createService(facilityId, payload).then(() => undefined)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: serviceKeys.all(facilityId) })
      onOpenChange(false)
    },
  })

  const v = watch()
  const set = (key: 'code' | 'name' | 'head' | 'displayOrder' | 'description') => ({
    value: (v[key] ?? '') as string,
    onChange: (_: unknown, d: { value: string }) => setValue(key, d.value, { shouldValidate: true }),
  })
  const onSubmit = handleSubmit((values) => mutation.mutate(values))

  // Candidats parents : services de l'établissement, sauf le service en cours d'édition.
  const parentOptions = (parents.data?.items ?? []).filter((p) => p.id !== service?.id)

  return (
    <Dialog open={open} onOpenChange={(_, d) => onOpenChange(d.open)}>
      <DialogSurface>
        <form onSubmit={onSubmit}>
          <DialogBody>
            <DialogTitle action={<Button appearance="subtle" icon={<Dismiss24Regular />} aria-label={t('settings.close')} onClick={() => onOpenChange(false)} />}>
              {isEdit ? t('service.edit') : t('service.new')}
            </DialogTitle>
            <DialogContent className={styles.form}>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('service.fields.code')} required
                  validationMessage={formState.errors.code ? t('service.validation.code') : undefined}>
                  <Input {...set('code')} />
                </Field>
                <Field className={styles.grow} label={t('service.fields.name')} required
                  validationMessage={formState.errors.name ? t('service.validation.name') : undefined}>
                  <Input {...set('name')} />
                </Field>
              </div>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('service.fields.category')} required>
                  <Dropdown
                    value={t(`service.categories.${v.category}`)}
                    selectedOptions={[v.category]}
                    onOptionSelect={(_, d) => setValue('category', d.optionValue as FormValues['category'], { shouldValidate: true })}>
                    {SERVICE_CATEGORIES.map((c) => <Option key={c} value={c}>{t(`service.categories.${c}`)}</Option>)}
                  </Dropdown>
                </Field>
                <Field className={styles.grow} label={t('service.fields.parent')}>
                  <Dropdown
                    value={v.parentServiceId === NONE ? t('service.noParent') : (parentOptions.find((p) => p.id === v.parentServiceId)?.name ?? '')}
                    selectedOptions={[v.parentServiceId]}
                    onOptionSelect={(_, d) => setValue('parentServiceId', d.optionValue!, { shouldValidate: true })}>
                    <Option value={NONE}>{t('service.noParent')}</Option>
                    {parentOptions.map((p) => <Option key={p.id} value={p.id}>{p.name}</Option>)}
                  </Dropdown>
                </Field>
              </div>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('service.fields.head')}>
                  <Input {...set('head')} />
                </Field>
                <Field className={styles.grow} label={t('service.fields.displayOrder')}>
                  <Input {...set('displayOrder')} type="number" />
                </Field>
              </div>
              <Field label={t('service.fields.description')}>
                <Textarea {...set('description')} resize="vertical" />
              </Field>
              {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" type="button" onClick={() => onOpenChange(false)}>{t('service.cancel')}</Button>
              <Button appearance="primary" type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('service.saving') : t('service.save')}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
