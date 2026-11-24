import { useTranslation } from 'react-i18next'
import { Dropdown, Field, Input, Option } from '@fluentui/react-components'
import type { FacilityOption } from '@features/auth'

type Props = {
  globalAdmin: boolean
  facilityId: string | null
  establishmentName: string
  options: FacilityOption[]
  loading: boolean
  onPick: (id: string) => void
  /** Désactive la sélection même pour l'admin système (ex. édition : on ne déplace pas la ressource). */
  readOnly?: boolean
}

/**
 * Champ « Établissement d'appartenance » des formulaires de création. Admin système → liste
 * déroulante de tous les établissements ; sinon (ou en lecture seule) → libellé figé.
 */
export function EstablishmentField({ globalAdmin, facilityId, establishmentName, options, loading, onPick, readOnly }: Props) {
  const { t } = useTranslation()
  return (
    <Field label={t('auth.establishment')} required>
      {globalAdmin && !readOnly ? (
        <Dropdown
          value={establishmentName}
          selectedOptions={facilityId ? [facilityId] : []}
          placeholder={loading ? t('equipment.loading') : t('auth.establishment')}
          onOptionSelect={(_, d) => d.optionValue && onPick(d.optionValue)}>
          {options.map((o) => <Option key={o.id} value={o.id}>{o.label}</Option>)}
        </Dropdown>
      ) : (
        <Input value={establishmentName} disabled />
      )}
    </Field>
  )
}
