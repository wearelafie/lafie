import { useTranslation } from 'react-i18next'
import { useQueries, useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Text, Tooltip, makeStyles, shorthands, tokens } from '@fluentui/react-components'
import { getBuildings } from '@features/building/api'
import { getFloors, type Floor } from '@features/floor/api'
import { getSpacesByFacility, type Space } from '@features/space/api'

// Types d'espaces comptant des lits (on affiche la capacité en « lits »).
const BED_TYPES = new Set<Space['type']>(['PatientRoom', 'IntensiveCareRoom', 'RecoveryRoom'])

type BadgeColor = 'success' | 'warning' | 'subtle'
const statusBadge = (s: string): BadgeColor => (s === 'Active' ? 'success' : s === 'Maintenance' ? 'warning' : 'subtle')

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },

  // KPIs
  kpis: { display: 'flex', flexWrap: 'wrap', gap: tokens.spacingHorizontalM },
  kpi: {
    display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXXS,
    minWidth: '110px', ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalL),
    ...shorthands.border('1px', 'solid', tokens.colorNeutralStroke2),
    ...shorthands.borderRadius(tokens.borderRadiusLarge),
    backgroundColor: tokens.colorNeutralBackground1,
  },
  kpiValue: { fontSize: tokens.fontSizeHero700, fontWeight: tokens.fontWeightSemibold, lineHeight: tokens.lineHeightHero700 },
  kpiLabel: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },

  // Légende
  legend: { display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: tokens.spacingHorizontalL, color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  legendItem: { display: 'inline-flex', alignItems: 'center', columnGap: tokens.spacingHorizontalXS },
  dot: { width: '12px', height: '12px', ...shorthands.borderRadius(tokens.borderRadiusSmall), display: 'inline-block' },
  dotActive: { backgroundColor: tokens.colorPaletteGreenBackground3 },
  dotMaint: { backgroundColor: tokens.colorPaletteYellowBackground3 },
  dotInactive: { backgroundColor: tokens.colorNeutralBackground5 },

  // Bâtiments
  buildings: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  building: {
    ...shorthands.border('1px', 'solid', tokens.colorNeutralStroke2),
    ...shorthands.borderRadius(tokens.borderRadiusXLarge),
    backgroundColor: tokens.colorNeutralBackground1,
    overflow: 'hidden',
  },
  buildingHead: {
    display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM,
    ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalL),
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.borderBottom('1px', 'solid', tokens.colorNeutralStroke2),
  },
  buildingTitle: { display: 'flex', alignItems: 'baseline', columnGap: tokens.spacingHorizontalS, flexWrap: 'wrap' },
  code: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200, fontFamily: tokens.fontFamilyMonospace },

  // Étages (empilés, plus haut niveau en haut)
  floors: { display: 'flex', flexDirection: 'column' },
  floor: {
    display: 'grid', gridTemplateColumns: '140px 1fr', columnGap: tokens.spacingHorizontalL, alignItems: 'start',
    ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalL),
    ...shorthands.borderBottom('1px', 'dashed', tokens.colorNeutralStroke3),
  },
  floorLabel: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXXS, position: 'sticky' },
  floorName: { fontWeight: tokens.fontWeightSemibold },
  floorMeta: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },

  // Chambres / espaces
  rooms: { display: 'flex', flexWrap: 'wrap', gap: tokens.spacingHorizontalS },
  room: {
    display: 'flex', flexDirection: 'column', rowGap: '2px', minWidth: '96px', maxWidth: '150px',
    ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingHorizontalS),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.border('1px', 'solid', tokens.colorTransparentStroke),
    ...shorthands.borderLeft('4px', 'solid', tokens.colorNeutralStroke1),
  },
  roomActive: { backgroundColor: tokens.colorPaletteGreenBackground1, borderLeftColor: tokens.colorPaletteGreenBorderActive },
  roomMaint: { backgroundColor: tokens.colorPaletteYellowBackground1, borderLeftColor: tokens.colorPaletteYellowBorderActive },
  roomInactive: { backgroundColor: tokens.colorNeutralBackground3, borderLeftColor: tokens.colorNeutralStroke1 },
  roomCode: { fontWeight: tokens.fontWeightSemibold, fontSize: tokens.fontSizeBase300 },
  roomType: { color: tokens.colorNeutralForeground2, fontSize: tokens.fontSizeBase200 },
  roomFoot: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalXS, marginTop: '2px' },
  roomBeds: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  pmr: {
    fontSize: '10px', fontWeight: tokens.fontWeightSemibold, lineHeight: '1',
    ...shorthands.padding('2px', '4px'), ...shorthands.borderRadius(tokens.borderRadiusSmall),
    backgroundColor: tokens.colorNeutralBackground5, color: tokens.colorNeutralForeground2,
  },

  empty: { color: tokens.colorNeutralForeground4, fontSize: tokens.fontSizeBase200, fontStyle: 'italic', ...shorthands.padding(tokens.spacingVerticalXS, '0') },
  schematic: { color: tokens.colorNeutralForeground4, fontSize: tokens.fontSizeBase200 },
})

export function FacilityPlan({ facilityId }: { facilityId: string }) {
  const styles = useStyles()
  const { t } = useTranslation()
  const typeLabel = (c: string) => { const l = t(`space.types.${c}`); return l.startsWith('space.') ? c : l }

  const buildingsQ = useQuery({ queryKey: ['plan', facilityId, 'buildings'], queryFn: () => getBuildings(facilityId, { pageSize: 200 }) })
  const spacesQ = useQuery({ queryKey: ['plan', facilityId, 'spaces'], queryFn: () => getSpacesByFacility(facilityId, 500) })
  const buildings = buildingsQ.data?.items ?? []

  // Étages de chaque bâtiment, en parallèle.
  const floorQueries = useQueries({
    queries: buildings.map((b) => ({
      queryKey: ['plan', facilityId, 'floors', b.id],
      queryFn: () => getFloors(b.id, { pageSize: 200 }),
    })),
  })

  if (buildingsQ.isPending || spacesQ.isPending) return <div className={styles.state}><Spinner label={t('organization.plan.loading')} /></div>
  if (buildingsQ.isError || spacesQ.isError) return <div className={styles.state}>{t('organization.plan.error')}</div>
  if (buildings.length === 0) return <div className={styles.state}>{t('organization.plan.empty')}</div>

  const spaces = spacesQ.data?.items ?? []
  const roomsByFloor = new Map<string, Space[]>()
  for (const s of spaces) {
    const arr = roomsByFloor.get(s.floorId) ?? []
    arr.push(s)
    roomsByFloor.set(s.floorId, arr)
  }

  // KPIs
  const floorsAll: Floor[] = floorQueries.flatMap((q) => q.data?.items ?? [])
  const bedsTotal = spaces.reduce((n, s) => n + (BED_TYPES.has(s.type) ? (s.maxCapacity ?? 0) : 0), 0)
  const kpis = [
    { key: 'buildings', value: buildings.length },
    { key: 'floors', value: floorsAll.length },
    { key: 'rooms', value: spaces.length },
    { key: 'beds', value: bedsTotal },
  ]

  const roomClass = (s: string) => (s === 'Active' ? styles.roomActive : s === 'Maintenance' ? styles.roomMaint : styles.roomInactive)
  const sortFloors = (fs: Floor[]) =>
    [...fs].sort((a, b) => (b.level ?? 0) - (a.level ?? 0) || a.displayOrder - b.displayOrder || a.name.localeCompare(b.name))
  const sortRooms = (rs: Space[]) => [...rs].sort((a, b) => a.code.localeCompare(b.code, undefined, { numeric: true }))

  return (
    <div className={styles.root}>
      <div className={styles.kpis}>
        {kpis.map((k) => (
          <div key={k.key} className={styles.kpi}>
            <span className={styles.kpiValue}>{k.value}</span>
            <span className={styles.kpiLabel}>{t(`organization.plan.summary.${k.key}`)}</span>
          </div>
        ))}
      </div>

      <div className={styles.legend}>
        <strong>{t('organization.plan.legend')} :</strong>
        <span className={styles.legendItem}><span className={`${styles.dot} ${styles.dotActive}`} />{t('space.status.Active')}</span>
        <span className={styles.legendItem}><span className={`${styles.dot} ${styles.dotMaint}`} />{t('space.status.Maintenance')}</span>
        <span className={styles.legendItem}><span className={`${styles.dot} ${styles.dotInactive}`} />{t('space.status.Inactive')}</span>
        <span className={styles.schematic}>· {t('organization.plan.schematic')}</span>
      </div>

      <div className={styles.buildings}>
        {buildings.map((b, i) => {
          const fq = floorQueries[i]
          const floors = sortFloors(fq?.data?.items ?? [])
          return (
            <section key={b.id} className={styles.building}>
              <header className={styles.buildingHead}>
                <div className={styles.buildingTitle}>
                  <Text weight="semibold" size={400}>{b.name}</Text>
                  <span className={styles.code}>{b.code}</span>
                </div>
                <Badge appearance="filled" color={statusBadge(b.status)}>{t(`space.status.${b.status}`)}</Badge>
              </header>

              <div className={styles.floors}>
                {fq?.isPending && <div className={styles.floor}><Spinner size="tiny" /></div>}
                {fq?.isSuccess && floors.length === 0 && <div className={styles.floor}><span className={styles.empty}>{t('organization.plan.noFloors')}</span></div>}
                {floors.map((f) => {
                  const rooms = sortRooms(roomsByFloor.get(f.id) ?? [])
                  return (
                    <div key={f.id} className={styles.floor}>
                      <div className={styles.floorLabel}>
                        <span className={styles.floorName}>{f.name}</span>
                        <span className={styles.floorMeta}>{f.level != null ? t('organization.plan.level', { level: f.level }) : f.code}</span>
                        <Badge appearance="outline" size="small" color={statusBadge(f.status)}>{t(`space.status.${f.status}`)}</Badge>
                      </div>
                      {rooms.length === 0 ? (
                        <span className={styles.empty}>{t('organization.plan.noRooms')}</span>
                      ) : (
                        <div className={styles.rooms}>
                          {rooms.map((r) => (
                            <Tooltip key={r.id} relationship="description" content={`${r.name} · ${typeLabel(r.type)} · ${t(`space.status.${r.status}`)}`}>
                              <div className={`${styles.room} ${roomClass(r.status)}`}>
                                <span className={styles.roomCode}>{r.code}</span>
                                <span className={styles.roomType}>{typeLabel(r.type)}</span>
                                <div className={styles.roomFoot}>
                                  {BED_TYPES.has(r.type) && r.maxCapacity != null && (
                                    <span className={styles.roomBeds}>{t('organization.plan.beds', { count: r.maxCapacity })}</span>
                                  )}
                                  {r.accessible && <span className={styles.pmr}>PMR</span>}
                                </div>
                              </div>
                            </Tooltip>
                          ))}
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            </section>
          )
        })}
      </div>
    </div>
  )
}
