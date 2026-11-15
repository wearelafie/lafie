import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import {
  Button, Divider, Dropdown, Field, Input, Option, Spinner, Textarea, makeStyles, tokens,
} from '@fluentui/react-components'
import { useAppStore } from '@app/store/appStore'
import { isGlobalAdmin, useActiveFacility, useCurrentUser } from '@features/auth'
import { getOrganizations, organizationKeys } from '@features/organization/api'
import { DetailHeader, type Crumb } from '@shared/components'
import {
  EQUIPMENT_STATUSES, EQUIPMENT_CATEGORY_REFERENCE, createEquipment, equipmentKeys, getEquipmentItem, updateEquipment,
  type EquipmentInput,
} from '../api'
import { getReferenceValues, refLabel, type ReferenceValue } from '@features/reference/api'
import { getSpacesByFacility } from '@features/space/api'
import { getServices } from '@features/service/api'
import { getCareUnits } from '@features/care-unit/api'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, maxWidth: '680px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM, flexWrap: 'wrap' },
  grow: { flex: 1, minWidth: '200px' },
  sectionTitle: { fontSize: tokens.fontSizeBase200, fontWeight: tokens.fontWeightSemibold, color: tokens.colorNeutralForeground3, textTransform: 'uppercase', letterSpacing: '0.04em' },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
  actions: {
    position: 'sticky', bottom: 0, display: 'flex', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM,
    paddingTop: tokens.spacingVerticalM, marginTop: tokens.spacingVerticalS,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`, backgroundColor: tokens.colorNeutralBackground1,
  },
})

const NONE = '__none__'

type FormState = {
  inventoryNumber: string
  serialNumber: string
  name: string
  category: string
  brand: string
  model: string
  acquisitionDate: string
  supplier: string
  warrantyEnd: string
  estimatedLifespanMonths: string
  spaceId: string
  serviceId: string
  careUnitId: string
  status: string
  notes: string
  displayOrder: string
}

const empty: FormState = {
  inventoryNumber: '', serialNumber: '', name: '', category: '', brand: '', model: '',
  acquisitionDate: '', supplier: '', warrantyEnd: '', estimatedLifespanMonths: '',
  spaceId: '', serviceId: '', careUnitId: '', status: 'Operational', notes: '', displayOrder: '0',
}

export function EquipmentFormPage() {
  const styles = useStyles()
  const { t, i18n } = useTranslation()
  const lang = i18n.language
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { me } = useCurrentUser()
  const editId = useAppStore((s) => s.equipmentFormId)
  const activeFacilityId = useActiveFacility()
  const globalAdmin = isGlobalAdmin(me)
  const isEdit = editId != null

  const [f, setF] = useState<FormState>(empty)
  const set = <K extends keyof FormState>(k: K, v: FormState[K]) => setF((s) => ({ ...s, [k]: v }))

  const editing = useQuery({ queryKey: equipmentKeys.item(editId ?? ''), queryFn: () => getEquipmentItem(editId!), enabled: isEdit })

  // Établissement cible : en création, l'établissement actif (modifiable par l'admin système) ;
  // en édition, celui — figé — de l'équipement (on ne déplace pas un actif entre établissements ici).
  const [facilityId, setFacilityId] = useState<string | null>(activeFacilityId)
  useEffect(() => {
    if (isEdit) { if (editing.data) setFacilityId(editing.data.facilityId) }
    else setFacilityId(activeFacilityId)
  }, [isEdit, editing.data, activeFacilityId])

  // Liste des établissements pour le sélecteur (admin système, en création uniquement).
  const orgs = useQuery({
    queryKey: organizationKeys.list({ page: 1, pageSize: 100 }),
    queryFn: () => getOrganizations({ page: 1, pageSize: 100 }),
    enabled: globalAdmin,
    staleTime: 5 * 60 * 1000,
  })
  const selectedOrg = globalAdmin ? orgs.data?.items.find((o) => o.id === facilityId) : undefined
  const establishmentName = selectedOrg
    ? (selectedOrg.displayName ?? selectedOrg.officialName)
    : (me?.activeEstablishment?.name ?? '')
  const onPickFacility = (id: string) => {
    setFacilityId(id)
    setF((s) => ({ ...s, spaceId: '', serviceId: '', careUnitId: '' })) // les cibles sont scopées : on réinitialise
  }

  useEffect(() => {
    if (!isEdit) { setF(empty); return }
    const e = editing.data
    if (!e) return
    setF({
      inventoryNumber: e.inventoryNumber, serialNumber: e.serialNumber ?? '', name: e.name,
      category: e.category, brand: e.brand ?? '', model: e.model ?? '',
      acquisitionDate: e.acquisitionDate ?? '', supplier: e.supplier ?? '', warrantyEnd: e.warrantyEnd ?? '',
      estimatedLifespanMonths: e.estimatedLifespanMonths != null ? String(e.estimatedLifespanMonths) : '',
      spaceId: e.spaceId ?? '', serviceId: e.serviceId ?? '', careUnitId: e.careUnitId ?? '',
      status: e.status, notes: e.notes ?? '', displayOrder: String(e.displayOrder),
    })
  }, [isEdit, editing.data])

  const categories = useQuery({
    queryKey: ['reference', 'values', EQUIPMENT_CATEGORY_REFERENCE],
    queryFn: () => getReferenceValues(EQUIPMENT_CATEGORY_REFERENCE),
  })
  const spaces = useQuery({
    queryKey: ['spaces', 'byFacility', facilityId],
    queryFn: () => getSpacesByFacility(facilityId!),
    enabled: !!facilityId,
  })
  const services = useQuery({
    queryKey: ['services', facilityId, 'all', { pageSize: 200 }],
    queryFn: () => getServices(facilityId!, { pageSize: 200 }),
    enabled: !!facilityId,
  })
  const careUnits = useQuery({
    queryKey: ['careUnits', f.serviceId, 'all', { pageSize: 200 }],
    queryFn: () => getCareUnits(f.serviceId, { pageSize: 200 }),
    enabled: !!f.serviceId,
  })

  const catLabel = (code: string): string => {
    const v = categories.data?.find((c: ReferenceValue) => c.code === code)
    return v ? refLabel(v, lang) : code
  }

  const toInput = (): EquipmentInput => {
    const n = (s: string) => (s.trim() === '' ? null : s.trim())
    const order = Number.parseInt(f.displayOrder, 10)
    const lifespan = f.estimatedLifespanMonths.trim() === '' ? null : Number.parseInt(f.estimatedLifespanMonths, 10)
    return {
      inventoryNumber: f.inventoryNumber.trim(), serialNumber: n(f.serialNumber), name: f.name.trim(), category: f.category,
      brand: n(f.brand), model: n(f.model), acquisitionDate: n(f.acquisitionDate), supplier: n(f.supplier),
      warrantyEnd: n(f.warrantyEnd), estimatedLifespanMonths: Number.isNaN(lifespan as number) ? null : lifespan,
      spaceId: f.spaceId || null, serviceId: f.serviceId || null, careUnitId: f.careUnitId || null,
      status: f.status, notes: n(f.notes), displayOrder: Number.isNaN(order) ? 0 : order,
    }
  }

  const back = () => navigate({ to: '/admin/equipment' as never })

  const mutation = useMutation({
    mutationFn: async () => {
      if (isEdit) await updateEquipment(editId!, toInput())
      else await createEquipment(facilityId!, toInput())
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: equipmentKeys.all(facilityId ?? '') })
      back()
    },
  })

  const canSubmit = !!facilityId && f.inventoryNumber.trim() !== '' && f.name.trim() !== '' && f.category !== '' && !mutation.isPending
  const crumbs: Crumb[] = [{ label: t('equipment.title'), to: '/admin/equipment' }]

  if (!facilityId) {
    return <div className={styles.root}><div className={styles.state}>{t('equipment.noFacility')}</div></div>
  }
  if (isEdit && editing.isPending) {
    return <div className={styles.root}><div className={styles.state}><Spinner label={t('equipment.loading')} /></div></div>
  }
  if (isEdit && (editing.isError || !editing.data)) {
    return <div className={styles.root}><div className={styles.state}>{t('equipment.loadError')}</div></div>
  }

  return (
    <div className={styles.root}>
      <DetailHeader title={t(isEdit ? 'equipment.form.edit' : 'equipment.form.new')} crumbs={crumbs} />
      <div className={styles.form}>
        {/* Établissement d'appartenance */}
        <span className={styles.sectionTitle}>{t('auth.establishment')}</span>
        <Field label={t('auth.establishment')} required>
          {globalAdmin && !isEdit ? (
            <Dropdown
              value={establishmentName}
              selectedOptions={facilityId ? [facilityId] : []}
              placeholder={orgs.isLoading ? t('equipment.loading') : t('auth.establishment')}
              onOptionSelect={(_, d) => d.optionValue && onPickFacility(d.optionValue)}>
              {(orgs.data?.items ?? []).map((o) => (
                <Option key={o.id} value={o.id}>{o.displayName ?? o.officialName}</Option>
              ))}
            </Dropdown>
          ) : (
            <Input value={establishmentName} disabled />
          )}
        </Field>

        <Divider />
        {/* Identification */}
        <span className={styles.sectionTitle}>{t('equipment.form.sections.identification')}</span>
        <div className={styles.row}>
          <Field className={styles.grow} label={t('equipment.fields.inventoryNumber')} required>
            <Input value={f.inventoryNumber} onChange={(_, d) => set('inventoryNumber', d.value)} placeholder="INV-0001" />
          </Field>
          <Field className={styles.grow} label={t('equipment.fields.serialNumber')}>
            <Input value={f.serialNumber} onChange={(_, d) => set('serialNumber', d.value)} />
          </Field>
        </div>
        <Field label={t('equipment.fields.name')} required>
          <Input value={f.name} onChange={(_, d) => set('name', d.value)} />
        </Field>
        <div className={styles.row}>
          <Field className={styles.grow} label={t('equipment.fields.category')} required>
            <Dropdown
              value={f.category ? catLabel(f.category) : ''}
              selectedOptions={f.category ? [f.category] : []}
              placeholder={categories.isLoading ? t('equipment.loading') : t('equipment.form.pickCategory')}
              onOptionSelect={(_, d) => set('category', d.optionValue ?? '')}>
              {(categories.data ?? []).map((c) => <Option key={c.code} value={c.code}>{refLabel(c, lang)}</Option>)}
            </Dropdown>
          </Field>
          <Field className={styles.grow} label={t('equipment.fields.status')}>
            <Dropdown
              value={t(`equipment.statuses.${f.status}`)}
              selectedOptions={[f.status]}
              onOptionSelect={(_, d) => set('status', d.optionValue ?? 'Operational')}>
              {EQUIPMENT_STATUSES.map((s) => <Option key={s} value={s}>{t(`equipment.statuses.${s}`)}</Option>)}
            </Dropdown>
          </Field>
        </div>
        <div className={styles.row}>
          <Field className={styles.grow} label={t('equipment.fields.brand')}>
            <Input value={f.brand} onChange={(_, d) => set('brand', d.value)} />
          </Field>
          <Field className={styles.grow} label={t('equipment.fields.model')}>
            <Input value={f.model} onChange={(_, d) => set('model', d.value)} />
          </Field>
        </div>

        <Divider />
        {/* Acquisition & garantie */}
        <span className={styles.sectionTitle}>{t('equipment.form.sections.acquisition')}</span>
        <div className={styles.row}>
          <Field className={styles.grow} label={t('equipment.fields.acquisitionDate')}>
            <Input type="date" value={f.acquisitionDate} onChange={(_, d) => set('acquisitionDate', d.value)} />
          </Field>
          <Field className={styles.grow} label={t('equipment.fields.warrantyEnd')}>
            <Input type="date" value={f.warrantyEnd} onChange={(_, d) => set('warrantyEnd', d.value)} />
          </Field>
        </div>
        <div className={styles.row}>
          <Field className={styles.grow} label={t('equipment.fields.supplier')}>
            <Input value={f.supplier} onChange={(_, d) => set('supplier', d.value)} />
          </Field>
          <Field label={t('equipment.fields.estimatedLifespanMonths')}>
            <Input type="number" value={f.estimatedLifespanMonths} onChange={(_, d) => set('estimatedLifespanMonths', d.value)} style={{ width: '140px' }} />
          </Field>
        </div>

        <Divider />
        {/* Localisation & affectation */}
        <span className={styles.sectionTitle}>{t('equipment.form.sections.location')}</span>
        <Field label={t('equipment.fields.space')}>
          <Dropdown
            value={f.spaceId ? (spaces.data?.items.find((s) => s.id === f.spaceId)?.name ?? '') : t('equipment.form.unassigned')}
            selectedOptions={f.spaceId ? [f.spaceId] : [NONE]}
            onOptionSelect={(_, d) => set('spaceId', d.optionValue === NONE ? '' : (d.optionValue ?? ''))}>
            <Option value={NONE}>{t('equipment.form.unassigned')}</Option>
            {(spaces.data?.items ?? []).map((s) => <Option key={s.id} value={s.id}>{`${s.name} (${s.code})`}</Option>)}
          </Dropdown>
        </Field>
        <div className={styles.row}>
          <Field className={styles.grow} label={t('equipment.fields.service')}>
            <Dropdown
              value={f.serviceId ? (services.data?.items.find((s) => s.id === f.serviceId)?.name ?? '') : t('equipment.form.unassigned')}
              selectedOptions={f.serviceId ? [f.serviceId] : [NONE]}
              onOptionSelect={(_, d) => { const v = d.optionValue === NONE ? '' : (d.optionValue ?? ''); setF((s) => ({ ...s, serviceId: v, careUnitId: '' })) }}>
              <Option value={NONE}>{t('equipment.form.unassigned')}</Option>
              {(services.data?.items ?? []).map((s) => <Option key={s.id} value={s.id}>{s.name}</Option>)}
            </Dropdown>
          </Field>
          <Field className={styles.grow} label={t('equipment.fields.careUnit')}>
            <Dropdown
              disabled={!f.serviceId}
              value={f.careUnitId ? (careUnits.data?.items.find((c) => c.id === f.careUnitId)?.name ?? '') : t('equipment.form.unassigned')}
              selectedOptions={f.careUnitId ? [f.careUnitId] : [NONE]}
              onOptionSelect={(_, d) => set('careUnitId', d.optionValue === NONE ? '' : (d.optionValue ?? ''))}>
              <Option value={NONE}>{t('equipment.form.unassigned')}</Option>
              {(careUnits.data?.items ?? []).map((c) => <Option key={c.id} value={c.id}>{c.name}</Option>)}
            </Dropdown>
          </Field>
        </div>

        <Divider />
        {/* État & notes */}
        <span className={styles.sectionTitle}>{t('equipment.form.sections.notes')}</span>
        <Field label={t('equipment.fields.notes')}>
          <Textarea value={f.notes} onChange={(_, d) => set('notes', d.value)} resize="vertical" />
        </Field>
        <Field label={t('equipment.fields.displayOrder')}>
          <Input type="number" value={f.displayOrder} onChange={(_, d) => set('displayOrder', d.value)} style={{ width: '110px' }} />
        </Field>

        {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}

        <div className={styles.actions}>
          <Button appearance="secondary" type="button" onClick={back}>{t('actions.cancel')}</Button>
          <Button appearance="primary" disabled={!canSubmit} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined} onClick={() => mutation.mutate()}>
            {t('actions.save')}
          </Button>
        </div>
      </div>
    </div>
  )
}
