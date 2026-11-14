import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Dropdown, Field, Input, Option, Spinner, makeStyles, tokens,
} from '@fluentui/react-components'
import {
  GEO_LEVELS, createGeoDivision, geoParentLevel, getGeoDivisions, referenceKeys, updateGeoDivision,
  type GeoDivision, type GeoLevel,
} from '@features/reference/api'

const useStyles = makeStyles({
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, minWidth: '440px' },
  row: { display: 'flex', columnGap: tokens.spacingHorizontalM },
  grow: { flex: 1 },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200 },
})

type Props = {
  open: boolean
  onOpenChange: (open: boolean) => void
  editing: GeoDivision | null
  defaultLevel: GeoLevel
}

export function GeoDivisionDialog({ open, onOpenChange, editing, defaultLevel }: Props) {
  const styles = useStyles()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const isEdit = editing != null

  const [level, setLevel] = useState<GeoLevel>(defaultLevel)
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [parentCode, setParentCode] = useState<string>('')
  const [displayOrder, setDisplayOrder] = useState('0')

  useEffect(() => {
    if (!open) return
    setLevel((editing?.level as GeoLevel) ?? defaultLevel)
    setCode(editing?.code ?? '')
    setName(editing?.name ?? '')
    setParentCode(editing?.parentCode ?? '')
    setDisplayOrder(String(editing?.displayOrder ?? 0))
  }, [open, editing, defaultLevel])

  const parentLevel = geoParentLevel(level)
  const parents = useQuery({
    queryKey: referenceKeys.geo(parentLevel ?? 'none'),
    queryFn: () => getGeoDivisions(parentLevel!),
    enabled: open && parentLevel != null,
  })

  const mutation = useMutation({
    mutationFn: async () => {
      const order = Number.parseInt(displayOrder, 10) || 0
      const parent = parentLevel ? parentCode || null : null
      if (isEdit) await updateGeoDivision(editing!.id, { name, parentCode: parent, displayOrder: order })
      else await createGeoDivision({ level, code, name, parentCode: parent, displayOrder: order })
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['reference', 'geo'] })
      onOpenChange(false)
    },
  })

  const needsParent = parentLevel != null
  const canSubmit = code.trim() !== '' && name.trim() !== '' && (!needsParent || parentCode !== '') && !mutation.isPending

  return (
    <Dialog open={open} onOpenChange={(_, d) => onOpenChange(d.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{t(isEdit ? 'adminGeo.edit' : 'adminGeo.new')}</DialogTitle>
          <DialogContent>
            <div className={styles.form}>
              <div className={styles.row}>
                <Field className={styles.grow} label={t('adminGeo.fields.level')} required>
                  <Dropdown
                    disabled={isEdit}
                    value={t(`adminGeo.levels.${level}`)}
                    selectedOptions={[level]}
                    onOptionSelect={(_, d) => { setLevel(d.optionValue as GeoLevel); setParentCode('') }}>
                    {GEO_LEVELS.map((l) => <Option key={l} value={l}>{t(`adminGeo.levels.${l}`)}</Option>)}
                  </Dropdown>
                </Field>
                <Field label={t('adminGeo.fields.code')} required>
                  <Input value={code} disabled={isEdit} onChange={(_, d) => setCode(d.value)} placeholder="EX: TG-M" style={{ width: '140px' }} />
                </Field>
              </div>

              {needsParent && (
                <Field label={t('adminGeo.fields.parentCode', { level: t(`adminGeo.levels.${parentLevel}`) })} required>
                  <Dropdown
                    value={parents.data?.find((p) => p.code === parentCode)?.name ?? ''}
                    selectedOptions={parentCode ? [parentCode] : []}
                    placeholder={t('adminGeo.pickParent')}
                    onOptionSelect={(_, d) => setParentCode(d.optionValue ?? '')}>
                    {(parents.data ?? []).map((p) => <Option key={p.id} value={p.code}>{`${p.name} (${p.code})`}</Option>)}
                  </Dropdown>
                </Field>
              )}

              <div className={styles.row}>
                <Field className={styles.grow} label={t('adminGeo.fields.name')} required>
                  <Input value={name} onChange={(_, d) => setName(d.value)} />
                </Field>
                <Field label={t('adminRef.fields.displayOrder')}>
                  <Input type="number" value={displayOrder} onChange={(_, d) => setDisplayOrder(d.value)} style={{ width: '110px' }} />
                </Field>
              </div>

              {mutation.isError && <span className={styles.error}>{(mutation.error as Error).message}</span>}
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={() => onOpenChange(false)}>{t('actions.cancel')}</Button>
            <Button appearance="primary" disabled={!canSubmit} icon={mutation.isPending ? <Spinner size="tiny" /> : undefined} onClick={() => mutation.mutate()}>
              {t('actions.save')}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
