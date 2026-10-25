import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Button, Checkbox, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Input, Option, Subtitle2, Textarea, makeStyles, tokens,
} from '@fluentui/react-components'
import { Dismiss24Regular } from '@fluentui/react-icons'
import {
  FACILITY_TYPES, OWNERSHIP_TYPES, createOrganization, organizationKeys, updateOrganization, type Organization,
} from '../api'

const useStyles = makeStyles({
  surface: { width: '92vw', maxWidth: '820px', maxHeight: '88vh' },
  body: { maxHeight: '70vh', overflowY: 'auto' },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, paddingTop: tokens.spacingVerticalS },
  section: { marginTop: tokens.spacingVerticalS, color: tokens.colorBrandForeground1 },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM, flexWrap: 'wrap' },
  grow: { flex: 1, minWidth: '200px' },
  error: { color: tokens.colorPaletteRedForeground1 },
})

const opt = z.string().trim().optional().or(z.literal(''))
const schema = z.object({
  code: z.string().trim().min(1).max(50),
  officialName: z.string().trim().min(1).max(200),
  facilityTypeCode: z.string().trim().min(1),
  ownershipTypeCode: z.string().trim().min(1),
  countryCode: z.string().trim().length(2),
  displayName: opt, abbreviation: opt, description: opt, websiteUrl: opt, establishedOn: opt,
  legalName: opt, legalEntityTypeCode: opt, registrationNumber: opt, nationalFacilityCode: opt,
  licenseNumber: opt, licenseIssuedOn: opt, licenseExpiresOn: opt, accreditationStatusCode: opt, taxIdentificationNumber: opt,
  regionCode: opt, prefectureCode: opt, communeCode: opt, locality: opt, districtOrNeighborhood: opt,
  streetAddress: opt, postalCode: opt,
  latitude: z.string().trim().regex(/^-?\d+([.,]\d+)?$/).optional().or(z.literal('')),
  longitude: z.string().trim().regex(/^-?\d+([.,]\d+)?$/).optional().or(z.literal('')),
  timeZoneId: opt, openingDate: opt, defaultCurrencyCode: opt, defaultLanguageCode: opt,
  emergencyServicesAvailable: z.boolean(), publicFacing: z.boolean(), phone: opt, email: opt,
})
type FormValues = z.infer<typeof schema>

const empty: FormValues = {
  code: '', officialName: '', facilityTypeCode: 'HOSPITAL', ownershipTypeCode: 'PUBLIC', countryCode: 'TG',
  displayName: '', abbreviation: '', description: '', websiteUrl: '', establishedOn: '',
  legalName: '', legalEntityTypeCode: '', registrationNumber: '', nationalFacilityCode: '',
  licenseNumber: '', licenseIssuedOn: '', licenseExpiresOn: '', accreditationStatusCode: '', taxIdentificationNumber: '',
  regionCode: '', prefectureCode: '', communeCode: '', locality: '', districtOrNeighborhood: '', streetAddress: '', postalCode: '',
  latitude: '', longitude: '', timeZoneId: 'Africa/Lome', openingDate: '', defaultCurrencyCode: 'XOF', defaultLanguageCode: 'fr',
  emergencyServicesAvailable: false, publicFacing: false, phone: '', email: '',
}

const num = (s: string) => (s ? Number(s.replace(',', '.')) : null)
const blank = (s: string) => (s ? s : null)

export function OrganizationFormDialog({
  open, onOpenChange, organization,
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
    resolver: zodResolver(schema), defaultValues: empty,
  })

  useEffect(() => {
    if (!open) return
    reset(organization ? fromOrg(organization) : empty)
  }, [open, organization, reset])

  const mutation = useMutation({
    mutationFn: (v: FormValues) => {
      const payload = {
        code: v.code, officialName: v.officialName, facilityTypeCode: v.facilityTypeCode,
        ownershipTypeCode: v.ownershipTypeCode, countryCode: v.countryCode.toUpperCase(),
        displayName: blank(v.displayName!), abbreviation: blank(v.abbreviation!), description: blank(v.description!),
        websiteUrl: blank(v.websiteUrl!), establishedOn: blank(v.establishedOn!),
        legalName: blank(v.legalName!), legalEntityTypeCode: blank(v.legalEntityTypeCode!),
        registrationNumber: blank(v.registrationNumber!), nationalFacilityCode: blank(v.nationalFacilityCode!),
        licenseNumber: blank(v.licenseNumber!), licenseIssuedOn: blank(v.licenseIssuedOn!), licenseExpiresOn: blank(v.licenseExpiresOn!),
        accreditationStatusCode: blank(v.accreditationStatusCode!), taxIdentificationNumber: blank(v.taxIdentificationNumber!),
        regionCode: blank(v.regionCode!), prefectureCode: blank(v.prefectureCode!), communeCode: blank(v.communeCode!),
        locality: blank(v.locality!), districtOrNeighborhood: blank(v.districtOrNeighborhood!),
        streetAddress: blank(v.streetAddress!), postalCode: blank(v.postalCode!),
        latitude: num(v.latitude!), longitude: num(v.longitude!), timeZoneId: v.timeZoneId || 'Africa/Lome',
        openingDate: blank(v.openingDate!), defaultCurrencyCode: v.defaultCurrencyCode || 'XOF', defaultLanguageCode: v.defaultLanguageCode || 'fr',
        emergencyServicesAvailable: v.emergencyServicesAvailable, publicFacing: v.publicFacing,
        phone: blank(v.phone!), email: blank(v.email!),
      }
      return isEdit ? updateOrganization(organization!.id, payload) : createOrganization(payload).then(() => undefined)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: organizationKeys.all })
      onOpenChange(false)
    },
  })

  const v = watch()
  const txt = (key: keyof FormValues) => ({
    value: (v[key] ?? '') as string,
    onChange: (_: unknown, d: { value: string }) => setValue(key, d.value, { shouldValidate: true }),
  })
  const onSubmit = handleSubmit((values) => mutation.mutate(values))

  return (
    <Dialog open={open} onOpenChange={(_, d) => onOpenChange(d.open)}>
      <DialogSurface className={styles.surface}>
        <form onSubmit={onSubmit}>
          <DialogBody>
            <DialogTitle action={<Button appearance="subtle" icon={<Dismiss24Regular />} aria-label={t('settings.close')} onClick={() => onOpenChange(false)} />}>
              {isEdit ? t('organization.edit') : t('organization.new')}
            </DialogTitle>
            <DialogContent className={styles.body}>
              <div className={styles.form}>
                <Subtitle2 className={styles.section}>{t('organization.sections.identity')}</Subtitle2>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.code')} required
                    validationMessage={formState.errors.code ? t('organization.validation.code') : undefined}>
                    <Input {...txt('code')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.officialName')} required
                    validationMessage={formState.errors.officialName ? t('organization.validation.officialName') : undefined}>
                    <Input {...txt('officialName')} /></Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.displayName')}><Input {...txt('displayName')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.abbreviation')}><Input {...txt('abbreviation')} /></Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.facilityType')} required>
                    <Dropdown value={typeLabel(t, v.facilityTypeCode)} selectedOptions={[v.facilityTypeCode]}
                      onOptionSelect={(_, d) => setValue('facilityTypeCode', d.optionValue!)}>
                      {FACILITY_TYPES.map((c) => <Option key={c} value={c}>{t(`organization.facilityTypes.${c}`)}</Option>)}
                    </Dropdown></Field>
                  <Field className={styles.grow} label={t('organization.fields.ownershipType')} required>
                    <Dropdown value={ownLabel(t, v.ownershipTypeCode)} selectedOptions={[v.ownershipTypeCode]}
                      onOptionSelect={(_, d) => setValue('ownershipTypeCode', d.optionValue!)}>
                      {OWNERSHIP_TYPES.map((c) => <Option key={c} value={c}>{t(`organization.ownershipTypes.${c}`)}</Option>)}
                    </Dropdown></Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.websiteUrl')}><Input {...txt('websiteUrl')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.establishedOn')}><Input {...txt('establishedOn')} type="date" /></Field>
                </div>
                <Field label={t('organization.fields.description')}><Textarea {...txt('description')} resize="vertical" /></Field>

                <Subtitle2 className={styles.section}>{t('organization.sections.location')}</Subtitle2>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.countryCode')} required
                    validationMessage={formState.errors.countryCode ? t('organization.validation.countryCode') : undefined}>
                    <Input {...txt('countryCode')} maxLength={2} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.timeZoneId')}><Input {...txt('timeZoneId')} /></Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.regionCode')}><Input {...txt('regionCode')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.prefectureCode')}><Input {...txt('prefectureCode')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.communeCode')}><Input {...txt('communeCode')} /></Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.locality')}><Input {...txt('locality')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.district')}><Input {...txt('districtOrNeighborhood')} /></Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.streetAddress')}><Input {...txt('streetAddress')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.postalCode')}><Input {...txt('postalCode')} /></Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.latitude')}
                    validationMessage={formState.errors.latitude ? t('organization.validation.coord') : undefined}><Input {...txt('latitude')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.longitude')}
                    validationMessage={formState.errors.longitude ? t('organization.validation.coord') : undefined}><Input {...txt('longitude')} /></Field>
                </div>

                <Subtitle2 className={styles.section}>{t('organization.sections.legal')}</Subtitle2>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.legalName')}><Input {...txt('legalName')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.legalEntityType')}><Input {...txt('legalEntityTypeCode')} /></Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.registrationNumber')}><Input {...txt('registrationNumber')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.nationalFacilityCode')}><Input {...txt('nationalFacilityCode')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.taxId')}><Input {...txt('taxIdentificationNumber')} /></Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.licenseNumber')}><Input {...txt('licenseNumber')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.licenseIssuedOn')}><Input {...txt('licenseIssuedOn')} type="date" /></Field>
                  <Field className={styles.grow} label={t('organization.fields.licenseExpiresOn')}><Input {...txt('licenseExpiresOn')} type="date" /></Field>
                </div>
                <Field label={t('organization.fields.accreditationStatus')}><Input {...txt('accreditationStatusCode')} /></Field>

                <Subtitle2 className={styles.section}>{t('organization.sections.operations')}</Subtitle2>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.openingDate')}><Input {...txt('openingDate')} type="date" /></Field>
                  <Field className={styles.grow} label={t('organization.fields.currency')}><Input {...txt('defaultCurrencyCode')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.language')}><Input {...txt('defaultLanguageCode')} /></Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('organization.fields.phone')}><Input {...txt('phone')} /></Field>
                  <Field className={styles.grow} label={t('organization.fields.email')}><Input {...txt('email')} /></Field>
                </div>
                <Checkbox checked={v.emergencyServicesAvailable} label={t('organization.fields.emergencyServices')}
                  onChange={(_, d) => setValue('emergencyServicesAvailable', !!d.checked)} />
                <Checkbox checked={v.publicFacing} label={t('organization.fields.publicFacing')}
                  onChange={(_, d) => setValue('publicFacing', !!d.checked)} />

                {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" type="button" onClick={() => onOpenChange(false)}>{t('organization.cancel')}</Button>
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

function typeLabel(t: (k: string) => string, code: string) {
  const l = t(`organization.facilityTypes.${code}`)
  return l.startsWith('organization.') ? code : l
}
function ownLabel(t: (k: string) => string, code: string) {
  const l = t(`organization.ownershipTypes.${code}`)
  return l.startsWith('organization.') ? code : l
}

function fromOrg(o: Organization): FormValues {
  const s = (x: string | null) => x ?? ''
  const d = (x: string | null) => (x ? x.slice(0, 10) : '')
  return {
    code: o.code, officialName: o.officialName, facilityTypeCode: o.facilityTypeCode, ownershipTypeCode: o.ownershipTypeCode, countryCode: o.countryCode,
    displayName: s(o.displayName), abbreviation: s(o.abbreviation), description: s(o.description), websiteUrl: s(o.websiteUrl), establishedOn: d(o.establishedOn),
    legalName: s(o.legalName), legalEntityTypeCode: s(o.legalEntityTypeCode), registrationNumber: s(o.registrationNumber), nationalFacilityCode: s(o.nationalFacilityCode),
    licenseNumber: s(o.licenseNumber), licenseIssuedOn: d(o.licenseIssuedOn), licenseExpiresOn: d(o.licenseExpiresOn),
    accreditationStatusCode: s(o.accreditationStatusCode), taxIdentificationNumber: s(o.taxIdentificationNumber),
    regionCode: s(o.regionCode), prefectureCode: s(o.prefectureCode), communeCode: s(o.communeCode), locality: s(o.locality),
    districtOrNeighborhood: s(o.districtOrNeighborhood), streetAddress: s(o.streetAddress), postalCode: s(o.postalCode),
    latitude: o.latitude === null ? '' : String(o.latitude), longitude: o.longitude === null ? '' : String(o.longitude),
    timeZoneId: o.timeZoneId, openingDate: d(o.openingDate), defaultCurrencyCode: o.defaultCurrencyCode, defaultLanguageCode: o.defaultLanguageCode,
    emergencyServicesAvailable: o.emergencyServicesAvailable, publicFacing: o.publicFacing, phone: s(o.phone), email: s(o.email),
  }
}
