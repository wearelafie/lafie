import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Badge, Spinner, Subtitle2, Tab, TabList, makeStyles, tokens } from '@fluentui/react-components'
import { useAppStore } from '@app/store/appStore'
import { BuildingsPage } from '@features/building'
import { ServicesPage } from '@features/service'
import { DetailHeader, FieldList, type Crumb, type FieldRow } from '@shared/components'
import { getOrganization } from '../api'

const TABS = ['overview', 'buildings', 'services'] as const
type TabKey = (typeof TABS)[number]

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  tabContent: { paddingTop: tokens.spacingVerticalM },
  sections: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalXL },
  section: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalS },
})

const statusColor = (s: string) => (s === 'Active' ? 'success' : s === 'Setup' ? 'brand' : s === 'PermanentlyClosed' ? 'danger' : 'warning')

export function FacilityDetailPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const id = useAppStore((s) => s.selectedFacilityId)
  const pendingTab = useAppStore((s) => s.infraTab)
  const [tab, setTab] = useState<TabKey>(() => (TABS.includes(pendingTab as TabKey) ? (pendingTab as TabKey) : 'overview'))
  const typeLabel = (c: string) => { const l = t(`organization.facilityTypes.${c}`); return l.startsWith('organization.') ? c : l }
  const ownLabel = (c: string) => { const l = t(`organization.ownershipTypes.${c}`); return l.startsWith('organization.') ? c : l }
  const b = (v: boolean) => (v ? t('common.yes') : t('common.no'))
  const d = (v: string | null) => (v ? v.slice(0, 10) : '—')

  const q = useQuery({ queryKey: ['organization', id], queryFn: () => getOrganization(id!), enabled: !!id })

  if (!id) return <div className={styles.root}><div className={styles.state}>{t('organization.detail.none')}</div></div>
  if (q.isPending) return <div className={styles.root}><div className={styles.state}><Spinner label={t('organization.loading')} /></div></div>
  if (q.isError || !q.data) return <div className={styles.root}><div className={styles.state}>{t('organization.loadError')}</div></div>

  const o = q.data
  const identity: FieldRow[] = [
    { label: t('organization.fields.code'), value: o.code },
    { label: t('organization.fields.officialName'), value: o.officialName },
    { label: t('organization.fields.displayName'), value: o.displayName },
    { label: t('organization.fields.abbreviation'), value: o.abbreviation },
    { label: t('organization.fields.facilityType'), value: typeLabel(o.facilityTypeCode) },
    { label: t('organization.fields.ownershipType'), value: ownLabel(o.ownershipTypeCode) },
    { label: t('organization.fields.status'), value: <Badge appearance="filled" color={statusColor(o.operationalStatus)}>{t(`organization.operationalStatus.${o.operationalStatus}`)}</Badge> },
    { label: t('organization.fields.websiteUrl'), value: o.websiteUrl },
    { label: t('organization.fields.establishedOn'), value: d(o.establishedOn) },
    { label: t('organization.fields.description'), value: o.description },
  ]
  const location: FieldRow[] = [
    { label: t('organization.fields.countryCode'), value: o.countryCode },
    { label: t('organization.fields.regionCode'), value: o.regionCode },
    { label: t('organization.fields.prefectureCode'), value: o.prefectureCode },
    { label: t('organization.fields.communeCode'), value: o.communeCode },
    { label: t('organization.fields.locality'), value: o.locality },
    { label: t('organization.fields.district'), value: o.districtOrNeighborhood },
    { label: t('organization.fields.streetAddress'), value: o.streetAddress },
    { label: t('organization.fields.postalCode'), value: o.postalCode },
    { label: t('organization.fields.latitude'), value: o.latitude },
    { label: t('organization.fields.longitude'), value: o.longitude },
    { label: t('organization.fields.timeZoneId'), value: o.timeZoneId },
  ]
  const legal: FieldRow[] = [
    { label: t('organization.fields.legalName'), value: o.legalName },
    { label: t('organization.fields.legalEntityType'), value: o.legalEntityTypeCode },
    { label: t('organization.fields.registrationNumber'), value: o.registrationNumber },
    { label: t('organization.fields.nationalFacilityCode'), value: o.nationalFacilityCode },
    { label: t('organization.fields.taxId'), value: o.taxIdentificationNumber },
    { label: t('organization.fields.licenseNumber'), value: o.licenseNumber },
    { label: t('organization.fields.licenseIssuedOn'), value: d(o.licenseIssuedOn) },
    { label: t('organization.fields.licenseExpiresOn'), value: d(o.licenseExpiresOn) },
    { label: t('organization.fields.accreditationStatus'), value: o.accreditationStatusCode },
  ]
  const operations: FieldRow[] = [
    { label: t('organization.fields.openingDate'), value: d(o.openingDate) },
    { label: t('organization.fields.currency'), value: o.defaultCurrencyCode },
    { label: t('organization.fields.language'), value: o.defaultLanguageCode },
    { label: t('organization.fields.phone'), value: o.phone },
    { label: t('organization.fields.email'), value: o.email },
    { label: t('organization.fields.emergencyServices'), value: b(o.emergencyServicesAvailable) },
    { label: t('organization.fields.publicFacing'), value: b(o.publicFacing) },
  ]
  const crumbs: Crumb[] = [{ label: t('organization.title'), to: '/admin' }]
  const sections: { key: string; rows: FieldRow[] }[] = [
    { key: 'identity', rows: identity },
    { key: 'location', rows: location },
    { key: 'legal', rows: legal },
    { key: 'operations', rows: operations },
  ]

  return (
    <div className={styles.root}>
      <DetailHeader title={o.officialName} crumbs={crumbs} />
      <TabList selectedValue={tab} onTabSelect={(_, dt) => setTab(dt.value as TabKey)}>
        <Tab value="overview">{t('organization.tabs.overview')}</Tab>
        <Tab value="buildings">{t('organization.tabs.buildings')}</Tab>
        <Tab value="services">{t('organization.tabs.services')}</Tab>
      </TabList>
      <div className={styles.tabContent}>
        {tab === 'overview' && (
          <div className={styles.sections}>
            {sections.map((s) => (
              <div key={s.key} className={styles.section}>
                <Subtitle2>{t(`organization.sections.${s.key}`)}</Subtitle2>
                <FieldList rows={s.rows} />
              </div>
            ))}
          </div>
        )}
        {tab === 'buildings' && <BuildingsPage />}
        {tab === 'services' && <ServicesPage />}
      </div>
    </div>
  )
}
