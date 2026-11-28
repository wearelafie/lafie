import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Badge, Button, Checkbox, Dropdown, Field, Input, Option, SearchBox, Spinner, Textarea, Title3,
  Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow,
  makeStyles, mergeClasses, tokens,
} from '@fluentui/react-components'
import { Add20Regular, Edit20Regular, Search20Regular, CheckmarkCircle20Regular, ProhibitedNote20Regular } from '@fluentui/react-icons'
import { useActiveFacility, useFacilitySelector } from '@features/auth'
import { EstablishmentField, FormDrawer, RowActions, type RowAction } from '@shared/components'
import {
  MEDICATION_FORMS, MEDICATION_STATUSES, medicationKeys,
  createMedication, getMedications, setMedicationStatus, updateMedication,
  type CreateMedicationInput, type Medication, type MedicationFilter, type MedicationForm,
} from '../api'

const PAGE_SIZE = 10
const ANY = '__any__'

const statusColor = (s: string): 'success' | 'subtle' => (s === 'Active' ? 'success' : 'subtle')

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  header: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM },
  subtitle: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  filters: { display: 'flex', alignItems: 'flex-end', columnGap: tokens.spacingHorizontalM, rowGap: tokens.spacingVerticalS, flexWrap: 'wrap' },
  filterField: { display: 'flex', flexDirection: 'column', rowGap: '2px' },
  label: { fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
  muted: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  pager: { display: 'flex', alignItems: 'center', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '480px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1, minWidth: 0 },
})

type MedForm = {
  code: string; name: string; inn: string; form: MedicationForm; strength: string; unit: string
  atcCode: string; therapeuticClass: string; controlled: boolean; notes: string
}
const emptyForm: MedForm = {
  code: '', name: '', inn: '', form: 'Tablet', strength: '', unit: '', atcCode: '', therapeuticClass: '', controlled: false, notes: '',
}

export function PharmacyCatalogPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const facilityId = useActiveFacility()
  // Établissement du formulaire : figé pour un admin d'établissement, choisi par l'admin système.
  const { facilityId: formFacilityId, setFacilityId: onPickFacility, reset: resetFacility, globalAdmin, optionsLoading, options, establishmentName } = useFacilitySelector()

  const [draft, setDraft] = useState<{ search: string; form: string; status: string }>({ search: '', form: ANY, status: ANY })
  const [applied, setApplied] = useState<MedicationFilter>({ page: 1, pageSize: PAGE_SIZE })
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<Medication | null>(null)
  const [f, setF] = useState<MedForm>(emptyForm)
  const set = <K extends keyof MedForm>(k: K, v: MedForm[K]) => setF((s) => ({ ...s, [k]: v }))

  const query = useQuery({
    queryKey: medicationKeys.list(facilityId ?? '', applied),
    queryFn: () => getMedications(facilityId!, applied),
    enabled: !!facilityId,
  })

  const invalidate = () => { void queryClient.invalidateQueries({ queryKey: medicationKeys.all(facilityId ?? '') }) }

  const mutation = useMutation({
    mutationFn: async () => {
      const base = {
        name: f.name.trim(), inn: f.inn.trim() || null, form: f.form, strength: f.strength.trim() || null,
        unit: f.unit.trim(), atcCode: f.atcCode.trim() || null, therapeuticClass: f.therapeuticClass.trim() || null,
        controlled: f.controlled, notes: f.notes.trim() || null,
      }
      if (editing) { await updateMedication(editing.id, base); return }
      const input: CreateMedicationInput = { ...base, code: f.code.trim() }
      await createMedication(formFacilityId!, input)
    },
    onSuccess: () => { invalidate(); setOpen(false); setF(emptyForm); setEditing(null) },
  })

  const statusMut = useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) => setMedicationStatus(id, active),
    onSuccess: invalidate,
  })

  const openNew = () => { resetFacility(); setEditing(null); setF(emptyForm); setOpen(true) }
  const openEdit = (m: Medication) => {
    setEditing(m)
    setF({
      code: m.code, name: m.name, inn: m.inn ?? '', form: m.form, strength: m.strength ?? '', unit: m.unit,
      atcCode: m.atcCode ?? '', therapeuticClass: m.therapeuticClass ?? '', controlled: m.controlled, notes: m.notes ?? '',
    })
    setOpen(true)
  }

  const runSearch = () => setApplied({
    page: 1, pageSize: PAGE_SIZE,
    search: draft.search.trim() || undefined,
    form: draft.form === ANY ? undefined : draft.form,
    status: draft.status === ANY ? undefined : draft.status,
  })

  const total = query.data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const page = applied.page ?? 1
  const go = (p: number) => setApplied((a) => ({ ...a, page: Math.min(Math.max(1, p), totalPages) }))

  // En édition l'établissement est celui de la fiche (immuable) ; en création il faut l'avoir choisi.
  const canSubmit = (editing ? true : !!formFacilityId && f.code.trim() !== '')
    && f.name.trim() !== '' && f.unit.trim() !== '' && !mutation.isPending

  const rowActions = (m: Medication): RowAction[] => [
    { key: 'edit', label: t('actions.edit'), icon: <Edit20Regular />, onClick: () => openEdit(m) },
  ]
  const rowMenu = (m: Medication): RowAction[] => [
    m.status === 'Active'
      ? { key: 'deactivate', label: t('pharmacy.actions.deactivate'), icon: <ProhibitedNote20Regular />, onClick: () => statusMut.mutate({ id: m.id, active: false }) }
      : { key: 'reactivate', label: t('pharmacy.actions.reactivate'), icon: <CheckmarkCircle20Regular />, onClick: () => statusMut.mutate({ id: m.id, active: true }) },
  ]

  if (!facilityId) return <div className={styles.root}><div className={styles.state}>{t('pharmacy.noFacility')}</div></div>

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>{t('pharmacy.title')}</Title3>
          <div className={styles.subtitle}>{t('pharmacy.subtitle')}</div>
        </div>
      </div>

      <div className={styles.header}>
        <div className={styles.filters}>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('pharmacy.filters.search')}</span>
            <SearchBox value={draft.search} placeholder={t('pharmacy.filters.searchPlaceholder')}
              onChange={(_, d) => setDraft((s) => ({ ...s, search: d.value }))}
              onKeyDown={(e) => { if (e.key === 'Enter') runSearch() }} />
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('pharmacy.filters.form')}</span>
            <Dropdown value={draft.form === ANY ? t('pharmacy.filters.allForms') : t(`pharmacy.forms.${draft.form}`)}
              selectedOptions={[draft.form]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, form: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('pharmacy.filters.allForms')}</Option>
              {MEDICATION_FORMS.map((fm) => <Option key={fm} value={fm}>{t(`pharmacy.forms.${fm}`)}</Option>)}
            </Dropdown>
          </div>
          <div className={styles.filterField}>
            <span className={styles.label}>{t('pharmacy.filters.status')}</span>
            <Dropdown value={draft.status === ANY ? t('pharmacy.filters.allStatuses') : t(`pharmacy.statuses.${draft.status}`)}
              selectedOptions={[draft.status]} onOptionSelect={(_, d) => setDraft((s) => ({ ...s, status: d.optionValue ?? ANY }))}>
              <Option value={ANY}>{t('pharmacy.filters.allStatuses')}</Option>
              {MEDICATION_STATUSES.map((s) => <Option key={s} value={s}>{t(`pharmacy.statuses.${s}`)}</Option>)}
            </Dropdown>
          </div>
          <Button appearance="primary" icon={<Search20Regular />} onClick={runSearch}>{t('pharmacy.filters.apply')}</Button>
        </div>
        <Button appearance="primary" icon={<Add20Regular />} onClick={openNew}>{t('pharmacy.create')}</Button>
      </div>

      {query.isLoading ? (
        <div className={styles.state}><Spinner size="small" label={t('pharmacy.loading')} /></div>
      ) : query.isError ? (
        <div className={mergeClasses(styles.state, styles.error)}>{t('pharmacy.loadError')}</div>
      ) : (query.data?.items.length ?? 0) === 0 ? (
        <div className={styles.state}>{t('pharmacy.empty')}</div>
      ) : (
        <Table aria-label={t('pharmacy.title')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('pharmacy.fields.code')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.name')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.form')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.strength')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.unit')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('pharmacy.fields.actions')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(query.data?.items ?? []).map((m) => (
              <TableRow key={m.id}>
                <TableCell><TableCellLayout>{m.code}</TableCellLayout></TableCell>
                <TableCell>
                  <TableCellLayout description={m.inn ?? undefined}>
                    {m.name}{m.controlled && <> <Badge appearance="tint" color="danger" size="small">{t('pharmacy.fields.controlled')}</Badge></>}
                  </TableCellLayout>
                </TableCell>
                <TableCell><TableCellLayout>{t(`pharmacy.forms.${m.form}`)}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{m.strength ?? <span className={styles.muted}>—</span>}</TableCellLayout></TableCell>
                <TableCell><TableCellLayout>{m.unit}</TableCellLayout></TableCell>
                <TableCell><Badge appearance="tint" color={statusColor(m.status)}>{t(`pharmacy.statuses.${m.status}`)}</Badge></TableCell>
                <TableCell><RowActions primary={rowActions(m)} menu={rowMenu(m)} /></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <div className={styles.pager}>
        <span className={styles.muted}>{t('pharmacy.pager', { page, totalPages, total })}</span>
        <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => go(page - 1)}>{t('pharmacy.prev')}</Button>
        <Button size="small" appearance="subtle" disabled={page >= totalPages} onClick={() => go(page + 1)}>{t('pharmacy.next')}</Button>
      </div>

      <FormDrawer
        open={open}
        onOpenChange={(v) => { setOpen(v); if (!v) setEditing(null) }}
        title={t(editing ? 'pharmacy.edit' : 'pharmacy.create')}
        onSubmit={() => { if (canSubmit) mutation.mutate() }}
        footer={<>
          <Button appearance="secondary" type="button" onClick={() => setOpen(false)}>{t('actions.cancel')}</Button>
          <Button appearance="primary" type="submit" disabled={!canSubmit} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined}>
            {t(editing ? 'actions.save' : 'actions.create')}
          </Button>
        </>}>
              <div className={styles.form}>
                <EstablishmentField
                  globalAdmin={globalAdmin}
                  facilityId={editing ? editing.facilityId : formFacilityId}
                  establishmentName={editing ? '' : establishmentName}
                  options={options}
                  loading={optionsLoading}
                  onPick={onPickFacility}
                  readOnly={!!editing}
                />
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('pharmacy.fields.code')} required>
                    <Input value={f.code} disabled={!!editing} onChange={(_, d) => set('code', d.value)} placeholder={t('pharmacy.form.codePlaceholder')} />
                  </Field>
                  <Field className={styles.grow} label={t('pharmacy.fields.form')} required>
                    <Dropdown value={t(`pharmacy.forms.${f.form}`)} selectedOptions={[f.form]}
                      onOptionSelect={(_, d) => set('form', (d.optionValue as MedicationForm) ?? 'Tablet')}>
                      {MEDICATION_FORMS.map((fm) => <Option key={fm} value={fm}>{t(`pharmacy.forms.${fm}`)}</Option>)}
                    </Dropdown>
                  </Field>
                </div>
                <Field label={t('pharmacy.fields.name')} required>
                  <Input value={f.name} onChange={(_, d) => set('name', d.value)} />
                </Field>
                <Field label={t('pharmacy.fields.inn')}>
                  <Input value={f.inn} onChange={(_, d) => set('inn', d.value)} />
                </Field>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('pharmacy.fields.strength')}>
                    <Input value={f.strength} onChange={(_, d) => set('strength', d.value)} placeholder={t('pharmacy.form.strengthPlaceholder')} />
                  </Field>
                  <Field className={styles.grow} label={t('pharmacy.fields.unit')} required>
                    <Input value={f.unit} onChange={(_, d) => set('unit', d.value)} placeholder={t('pharmacy.form.unitPlaceholder')} />
                  </Field>
                </div>
                <div className={styles.row}>
                  <Field className={styles.grow} label={t('pharmacy.fields.atcCode')}>
                    <Input value={f.atcCode} onChange={(_, d) => set('atcCode', d.value)} placeholder={t('pharmacy.form.atcPlaceholder')} />
                  </Field>
                  <Field className={styles.grow} label={t('pharmacy.fields.therapeuticClass')}>
                    <Input value={f.therapeuticClass} onChange={(_, d) => set('therapeuticClass', d.value)} placeholder={t('pharmacy.form.classPlaceholder')} />
                  </Field>
                </div>
                <Checkbox label={t('pharmacy.fields.controlled')} checked={f.controlled} onChange={(_, d) => set('controlled', !!d.checked)} />
                <Field label={t('pharmacy.fields.notes')}>
                  <Textarea value={f.notes} onChange={(_, d) => set('notes', d.value)} resize="vertical" />
                </Field>
                {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
              </div>
      </FormDrawer>
    </div>
  )
}
