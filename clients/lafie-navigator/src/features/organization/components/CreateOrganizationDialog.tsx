import { useState } from 'react'
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
  DialogTrigger,
  Dropdown,
  Field,
  Input,
  Option,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { Add20Regular, Dismiss24Regular } from '@fluentui/react-icons'
import {
  ORGANIZATION_TYPES,
  createOrganization,
  organizationKeys,
  type OrganizationType,
} from '../api'

const useStyles = makeStyles({
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, paddingTop: tokens.spacingVerticalS },
  error: { color: tokens.colorPaletteRedForeground1 },
})

const schema = z.object({
  name: z.string().trim().min(1).max(200),
  type: z.enum(ORGANIZATION_TYPES),
  code: z.string().trim().max(50).optional().or(z.literal('')),
})

type FormValues = z.infer<typeof schema>

export function CreateOrganizationDialog() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)

  const { register, handleSubmit, setValue, watch, reset, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', type: 'Hospital', code: '' },
  })

  const mutation = useMutation({
    mutationFn: createOrganization,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: organizationKeys.all })
      reset()
      setOpen(false)
    },
  })

  const onSubmit = handleSubmit((values) =>
    mutation.mutate({ name: values.name, type: values.type, code: values.code || null }),
  )

  const selectedType = watch('type')

  return (
    <Dialog open={open} onOpenChange={(_, d) => setOpen(d.open)}>
      <DialogTrigger disableButtonEnhancement>
        <Button appearance="primary" icon={<Add20Regular />}>
          {t('organization.new')}
        </Button>
      </DialogTrigger>
      <DialogSurface>
        <form onSubmit={onSubmit}>
          <DialogBody>
            <DialogTitle
              action={
                <DialogTrigger action="close" disableButtonEnhancement>
                  <Button appearance="subtle" icon={<Dismiss24Regular />} aria-label={t('settings.close')} />
                </DialogTrigger>
              }
            >
              {t('organization.new')}
            </DialogTitle>
            <DialogContent className={styles.form}>
              <Field label={t('organization.fields.name')} required validationMessage={formState.errors.name ? t('organization.validation.name') : undefined}>
                <Input {...register('name')} placeholder={t('organization.fields.namePlaceholder')} />
              </Field>

              <Field label={t('organization.fields.type')} required>
                <Dropdown
                  value={t(`organization.types.${selectedType}`)}
                  selectedOptions={[selectedType]}
                  onOptionSelect={(_, d) => setValue('type', d.optionValue as OrganizationType)}
                >
                  {ORGANIZATION_TYPES.map((ty) => (
                    <Option key={ty} value={ty}>
                      {t(`organization.types.${ty}`)}
                    </Option>
                  ))}
                </Dropdown>
              </Field>

              <Field label={t('organization.fields.code')}>
                <Input {...register('code')} placeholder={t('organization.fields.codePlaceholder')} />
              </Field>

              {mutation.isError && (
                <span className={styles.error}>{(mutation.error as Error).message}</span>
              )}
            </DialogContent>
            <DialogActions>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary" type="button">
                  {t('organization.cancel')}
                </Button>
              </DialogTrigger>
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
