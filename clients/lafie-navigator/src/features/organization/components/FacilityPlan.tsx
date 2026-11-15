import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { useQueries, useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Text, Tooltip, makeStyles, shorthands, tokens } from '@fluentui/react-components'
import {
  Building24Regular, BuildingMultiple20Regular, Layer20Regular, Door20Regular, Bed20Regular,
  Bed16Regular, People16Regular, Ruler16Regular, Accessibility16Regular,
  CheckmarkCircle16Filled, Wrench16Filled, SubtractCircle16Filled,
  Stethoscope20Regular, Syringe20Regular, Alert20Regular, Cut20Regular, HeartPulse20Regular,
  Pulse20Regular, Beaker20Regular, ScanType20Regular, Pill20Regular, Briefcase20Regular,
  People20Regular, Wrench20Regular, BoxMultiple20Regular, Square20Regular,
} from '@fluentui/react-icons'
import { getBuildings } from '@features/building/api'
import { getFloors, type Floor } from '@features/floor/api'
import { getSpacesByFacility, type Space, type SpaceType } from '@features/space/api'

// Types d'espaces comptant des lits (on affiche la capacité en « lits »).
const BED_TYPES = new Set<SpaceType>(['PatientRoom', 'IntensiveCareRoom', 'RecoveryRoom'])

// Icône par type d'espace (rend le plan plus expressif qu'un simple libellé).
const TYPE_ICON: Record<SpaceType, ReactElement> = {
  PatientRoom: <Bed20Regular />,
  ConsultationRoom: <Stethoscope20Regular />,
  TreatmentRoom: <Syringe20Regular />,
  EmergencyRoom: <Alert20Regular />,
  OperatingRoom: <Cut20Regular />,
  RecoveryRoom: <HeartPulse20Regular />,
  IntensiveCareRoom: <Pulse20Regular />,
  Laboratory: <Beaker20Regular />,
  ImagingRoom: <ScanType20Regular />,
  Pharmacy: <Pill20Regular />,
  Office: <Briefcase20Regular />,
  WaitingRoom: <People20Regular />,
  TechnicalRoom: <Wrench20Regular />,
  Storage: <BoxMultiple20Regular />,
  Other: <Square20Regular />,
}

type BadgeColor = 'success' | 'warning' | 'subtle'
const statusColor = (s: string): BadgeColor => (s === 'Active' ? 'success' : s === 'Maintenance' ? 'warning' : 'subtle')
const statusIcon = (s: string): ReactElement =>
  s === 'Active' ? <CheckmarkCircle16Filled /> : s === 'Maintenance' ? <Wrench16Filled /> : <SubtractCircle16Filled />

const useStyles = makeStyles({
  root: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },

  // KPIs
  kpis: { display: 'flex', flexWrap: 'wrap', gap: tokens.spacingHorizontalM },
  kpi: {
    display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalM,
    minWidth: '150px', ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalL),
    ...shorthands.border('1px', 'solid', tokens.colorNeutralStroke2),
    ...shorthands.borderRadius(tokens.borderRadiusLarge),
    backgroundColor: tokens.colorNeutralBackground1,
  },
  kpiIcon: {
    display: 'flex', alignItems: 'center', justifyContent: 'center', width: '36px', height: '36px', flexShrink: 0,
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    backgroundColor: tokens.colorBrandBackground2, color: tokens.colorBrandForeground1,
    '& svg': { width: '20px', height: '20px' },
  },
  kpiText: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXXS },
  kpiValue: { fontSize: tokens.fontSizeHero700, fontWeight: tokens.fontWeightSemibold, lineHeight: tokens.lineHeightHero700 },
  kpiLabel: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },

  // Légende
  legend: { display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: tokens.spacingHorizontalL, color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200 },
  legendItem: { display: 'inline-flex', alignItems: 'center', columnGap: tokens.spacingHorizontalXS },
  schematic: { color: tokens.colorNeutralForeground4, fontSize: tokens.fontSizeBase200 },

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
  buildingTitle: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalS, flexWrap: 'wrap' },
  buildingIcon: { display: 'flex', alignItems: 'center', color: tokens.colorBrandForeground1, '& svg': { width: '24px', height: '24px' } },
  buildingTags: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalXS, flexWrap: 'wrap' },
  code: { color: tokens.colorNeutralForeground3, fontSize: tokens.fontSizeBase200, fontFamily: tokens.fontFamilyMonospace },

  // Étages (empilés, plus haut niveau en haut)
  floors: { display: 'flex', flexDirection: 'column' },
  floor: {
    display: 'grid', gridTemplateColumns: '160px 1fr', columnGap: tokens.spacingHorizontalL, alignItems: 'start',
    ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalL),
    ...shorthands.borderBottom('1px', 'dashed', tokens.colorNeutralStroke3),
  },
  floorLabel: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXS, position: 'sticky' },
  floorNameRow: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalXS },
  floorIcon: { display: 'flex', alignItems: 'center', color: tokens.colorNeutralForeground3, '& svg': { width: '18px', height: '18px' } },
  floorName: { fontWeight: tokens.fontWeightSemibold },
  floorTags: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalXS, flexWrap: 'wrap' },

  // Chambres / espaces
  rooms: { display: 'flex', flexWrap: 'wrap', gap: tokens.spacingHorizontalS },
  room: {
    display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXXS, minWidth: '132px', maxWidth: '180px',
    ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingHorizontalS),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.border('1px', 'solid', tokens.colorTransparentStroke),
    ...shorthands.borderLeft('4px', 'solid', tokens.colorNeutralStroke1),
  },
  roomActive: { backgroundColor: tokens.colorPaletteGreenBackground1, borderLeftColor: tokens.colorPaletteGreenBorderActive },
  roomMaint: { backgroundColor: tokens.colorPaletteYellowBackground1, borderLeftColor: tokens.colorPaletteYellowBorderActive },
  roomInactive: { backgroundColor: tokens.colorNeutralBackground3, borderLeftColor: tokens.colorNeutralStroke1 },
  roomHead: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalXS },
  roomTypeIcon: { display: 'flex', alignItems: 'center', color: tokens.colorNeutralForeground2, '& svg': { width: '18px', height: '18px' } },
  roomCode: { fontWeight: tokens.fontWeightSemibold, fontSize: tokens.fontSizeBase300 },
  roomStatus: { marginLeft: 'auto', display: 'flex', alignItems: 'center' },
  roomType: { color: tokens.colorNeutralForeground2, fontSize: tokens.fontSizeBase200 },
  roomTags: { display: 'flex', alignItems: 'center', columnGap: '4px', rowGap: '4px', flexWrap: 'wrap', marginTop: '2px' },

  // Badge « tag » : un peu d'air interne (le small Fluent colle le contenu aux bords).
  tag: {
    height: 'auto', minHeight: '22px',
    ...shorthands.padding('2px', tokens.spacingHorizontalS),
    columnGap: tokens.spacingHorizontalXS,
  },

  empty: { color: tokens.colorNeutralForeground4, fontSize: tokens.fontSizeBase200, fontStyle: 'italic', ...shorthands.padding(tokens.spacingVerticalXS, '0') },
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
  const kpis: { key: string; value: number; icon: ReactElement }[] = [
    { key: 'buildings', value: buildings.length, icon: <BuildingMultiple20Regular /> },
    { key: 'floors', value: floorsAll.length, icon: <Layer20Regular /> },
    { key: 'rooms', value: spaces.length, icon: <Door20Regular /> },
    { key: 'beds', value: bedsTotal, icon: <Bed20Regular /> },
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
            <span className={styles.kpiIcon}>{k.icon}</span>
            <span className={styles.kpiText}>
              <span className={styles.kpiValue}>{k.value}</span>
              <span className={styles.kpiLabel}>{t(`organization.plan.summary.${k.key}`)}</span>
            </span>
          </div>
        ))}
      </div>

      <div className={styles.legend}>
        <strong>{t('organization.plan.legend')} :</strong>
        <span className={styles.legendItem}><Badge appearance="filled" color="success" size="small" icon={<CheckmarkCircle16Filled />} />{t('space.status.Active')}</span>
        <span className={styles.legendItem}><Badge appearance="filled" color="warning" size="small" icon={<Wrench16Filled />} />{t('space.status.Maintenance')}</span>
        <span className={styles.legendItem}><Badge appearance="filled" color="subtle" size="small" icon={<SubtractCircle16Filled />} />{t('space.status.Inactive')}</span>
        <span className={styles.schematic}>· {t('organization.plan.schematic')}</span>
      </div>

      <div className={styles.buildings}>
        {buildings.map((b, i) => {
          const fq = floorQueries[i]
          const floors = sortFloors(fq?.data?.items ?? [])
          const roomCount = floors.reduce((n, f) => n + (roomsByFloor.get(f.id)?.length ?? 0), 0)
          return (
            <section key={b.id} className={styles.building}>
              <header className={styles.buildingHead}>
                <div className={styles.buildingTitle}>
                  <span className={styles.buildingIcon}><Building24Regular /></span>
                  <Text weight="semibold" size={400}>{b.name}</Text>
                  <span className={styles.code}>{b.code}</span>
                </div>
                <div className={styles.buildingTags}>
                  <Badge className={styles.tag} appearance="tint" color="informative" size="small" icon={<Layer20Regular />}>{t('organization.plan.floorsCount', { count: floors.length })}</Badge>
                  <Badge className={styles.tag} appearance="tint" color="informative" size="small" icon={<Door20Regular />}>{t('organization.plan.roomsCount', { count: roomCount })}</Badge>
                  <Badge className={styles.tag} appearance="filled" color={statusColor(b.status)} icon={statusIcon(b.status)}>{t(`space.status.${b.status}`)}</Badge>
                </div>
              </header>

              <div className={styles.floors}>
                {fq?.isPending && <div className={styles.floor}><Spinner size="tiny" /></div>}
                {fq?.isSuccess && floors.length === 0 && <div className={styles.floor}><span className={styles.empty}>{t('organization.plan.noFloors')}</span></div>}
                {floors.map((f) => {
                  const rooms = sortRooms(roomsByFloor.get(f.id) ?? [])
                  return (
                    <div key={f.id} className={styles.floor}>
                      <div className={styles.floorLabel}>
                        <div className={styles.floorNameRow}>
                          <span className={styles.floorIcon}><Layer20Regular /></span>
                          <span className={styles.floorName}>{f.name}</span>
                        </div>
                        <div className={styles.floorTags}>
                          <Badge className={styles.tag} appearance="outline" size="small">{f.level != null ? t('organization.plan.level', { level: f.level }) : f.code}</Badge>
                          <Badge className={styles.tag} appearance="ghost" size="small" color={statusColor(f.status)} icon={statusIcon(f.status)}>{t(`space.status.${f.status}`)}</Badge>
                        </div>
                      </div>
                      {rooms.length === 0 ? (
                        <span className={styles.empty}>{t('organization.plan.noRooms')}</span>
                      ) : (
                        <div className={styles.rooms}>
                          {rooms.map((r) => {
                            const isBed = BED_TYPES.has(r.type)
                            return (
                              <Tooltip key={r.id} relationship="description" content={`${r.name} · ${typeLabel(r.type)} · ${t(`space.status.${r.status}`)}`}>
                                <div className={`${styles.room} ${roomClass(r.status)}`}>
                                  <div className={styles.roomHead}>
                                    <span className={styles.roomTypeIcon}>{TYPE_ICON[r.type]}</span>
                                    <span className={styles.roomCode}>{r.code}</span>
                                    <span className={styles.roomStatus}>
                                      <Badge appearance="ghost" size="small" color={statusColor(r.status)} icon={statusIcon(r.status)} aria-label={t(`space.status.${r.status}`)} />
                                    </span>
                                  </div>
                                  <span className={styles.roomType}>{typeLabel(r.type)}</span>
                                  <div className={styles.roomTags}>
                                    {r.maxCapacity != null && (
                                      <Badge className={styles.tag} appearance="tint" color="brand" size="small" icon={isBed ? <Bed16Regular /> : <People16Regular />}>
                                        {isBed ? t('organization.plan.beds', { count: r.maxCapacity }) : t('organization.plan.capacity', { count: r.maxCapacity })}
                                      </Badge>
                                    )}
                                    {r.area != null && (
                                      <Badge className={styles.tag} appearance="outline" size="small" icon={<Ruler16Regular />}>{t('organization.plan.area', { area: r.area })}</Badge>
                                    )}
                                    {r.accessible && (
                                      <Badge className={styles.tag} appearance="tint" color="success" size="small" icon={<Accessibility16Regular />}>{t('organization.plan.accessibleTag')}</Badge>
                                    )}
                                  </div>
                                </div>
                              </Tooltip>
                            )
                          })}
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
