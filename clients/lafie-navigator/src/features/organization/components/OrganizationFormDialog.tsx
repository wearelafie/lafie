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
  Dropdown,
  Field,
  Input,
  Option,
  Textarea,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { Dismiss24Regular } from '@fluentui/react-icons'
import {
  ORGANIZATION_TYPES,
  createOrganization,
  organizationKeys,
  updateOrganization,
  type Organization,
  type OrganizationType,
} from '../api'

const useStyles = makeStyles({
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, paddingTop: tokens.spacingVerticalS },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM, '@media (max-width: 520px)': { flexDirection: 'column' } },
  grow: { flex: 1, minWidth: 0 },
  error: { color: tokens.colorPaletteRedForeground1 },
})

const schema = z.object({
  name: z.string().trim().min(1).max(200),
  type: z.enum(ORGANIZATION_TYPES),
  code: z.string().trim().max(50).optional().or(z.literal('')),
  shortName: z.string().trim().max(100).optional().or(z.literal('')),
  phone: z.string().trim().max(50).optional().or(z.literal('')),
  email: z.string().trim().email().optional().or(z.literal('')),
  city: z.string().trim().max(100).optional().or(z.literal('')),
  country: z.string().trim().max(100).optional().or(z.literal('')),
  description: z.string().trim().max(1000).optional().or(z.literal('')),
})

type FormValues = z.infer<typeof schema>

const empty: FormValues = {
  name: '', type: 'Hospital', code: '', shortName: '', phone: '', email: '', city: '', country: '', description: '',
}

export function OrganizationFormDialog({
  open,
  onOpenChange,
  organization,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  organization: Organization | null
}) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const isEdit = organization !== null

  const { handleSubmit, setValue, watch, reset, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: empty,
  })

  // (Ré)initialise le formulaire à chaque ouverture (création ou édition).
  useEffect(() => {
    if (!open) return
    reset(
      organization
        ? {
            name: organization.name,
            type: organization.type,
            code: organization.code ?? '',
            shortName: organization.shortName ?? '',
            phone: organization.phone ?? '',
            email: organization.email ?? '',
            city: organization.city ?? '',
            country: organization.country ?? '',
            description: organization.description ?? '',
          }
        : empty,
    )
  }, [open, organization, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const payload = {
        name: values.name,
        type: values.type,
        code: values.code || null,
        shortName: values.shortName || null,
        phone: values.phone || null,
        email: values.email || null,
        city: values.city || null,
        country: values.country || null,
        description: values.description || null,
      }
      return isEdit ? updateOrganization(organization!.id, payload) : createOrganization(payload).then(() => undefined)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: organizationKeys.all })
      onOpenChange(false)
    },
  })

  const v = watch()
  const onSubmit = handleSubmit((values) => mutation.mutate(values))
  // Champs contrôlés : le ref de l'Input Fluent ne pointe pas l'<input>, donc RHF non-contrôlé
  // ne pré-remplit pas via reset → on pilote value/onChange explicitement.
  const set = (key: keyof FormValues) => ({
    value: (v[key] ?? '') as string,
    onChange: (_: unknown, d: { value: string }) => setValue(key, d.value, { shouldValidate: true }),
  })

  return (
    <Dialog open={open} onOpenChange={(_, d) => onOpenChange(d.open)}>
      <DialogSurface>
        <form onSubmit={onSubmit}>
          <DialogBody>
            <DialogTitle
              action={
                <Button appearance="subtle" icon={<Dismiss24Regular />} aria-label={t('settings.close')} onClick={() => onOpenChange(false)} />
              }
            >
              {isEdit ? t('organization.edit') : t('organization.new')}
            </DialogTitle>
            <DialogContent className={styles.form}>
              <Field label={t('organization.fields.name')} required
                validationMessage={formState.errors.name ? t('organization.validation.name') : undefined}>
                <Input {...set('name')} placeholder={t('organization.fields.namePlaceholder')} />
              </Field>

              <div className={styles.row}>
                <Field className={styles.grow} label={t('organization.fields.type')} required>
                  <Dropdown
                    value={t(`organization.types.${v.type}`)}
                    selectedOptions={[v.type]}
                    onOptionSelect={(_, d) => setValue('type', d.optionValue as OrganizationType)}
                  >
                    {ORGANIZATION_TYPES.map((ty) => (
                      <Option key={ty} value={ty}>{t(`organization.types.${ty}`)}</Option>
                    ))}
                  </Dropdown>
                </Field>
                <Field className={styles.grow} label={t('organization.fields.code')}>
                  <Input {...set('code')} placeholder={t('organization.fields.codePlaceholder')} />
                </Field>
              </div>

              <div className={styles.row}>
                <Field className={styles.grow} label={t('organization.fields.shortName')}>
                  <Input {...set('shortName')} />
                </Field>
                <Field className={styles.grow} label={t('organization.fields.phone')}>
                  <Input {...set('phone')} />
                </Field>
              </div>

              <div className={styles.row}>
                <Field className={styles.grow} label={t('organization.fields.email')}
                  validationMessage={formState.errors.email ? t('organization.validation.email') : undefined}>
                  <Input {...set('email')} />
                </Field>
                <Field className={styles.grow} label={t('organization.fields.city')}>
                  <Input {...set('city')} />
                </Field>
                <Field className={styles.grow} label={t('organization.fields.country')}>
                  <Input {...set('country')} />
                </Field>
              </div>

              <Field label={t('organization.fields.description')}>
                <Textarea {...set('description')} resize="vertical" />
              </Field>

              {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" type="button" onClick={() => onOpenChange(false)}>
                {t('organization.cancel')}
              </Button>
              <Button appearance="primary" type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('organization.saving') : t('organization.save')}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
