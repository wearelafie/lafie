import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import {
  Badge,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableCellLayout,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Title3,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { getOrganizations, organizationKeys } from '../api'
import { CreateOrganizationDialog } from '../components/CreateOrganizationDialog'

const useStyles = makeStyles({
  root: { padding: tokens.spacingVerticalXL, display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalL },
  header: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', columnGap: tokens.spacingHorizontalM },
  state: { padding: tokens.spacingVerticalXXL, textAlign: 'center', color: tokens.colorNeutralForeground3 },
  error: { color: tokens.colorPaletteRedForeground1 },
})

export function OrganizationsPage() {
  const styles = useStyles()
  const { t, i18n } = useTranslation()
  const query = useQuery({ queryKey: organizationKeys.all, queryFn: getOrganizations })

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <Title3>{t('organization.title')}</Title3>
        <CreateOrganizationDialog />
      </div>

      {query.isPending && (
        <div className={styles.state}>
          <Spinner label={t('organization.loading')} />
        </div>
      )}

      {query.isError && (
        <div className={styles.state}>
          <span className={styles.error}>{t('organization.loadError')}</span>
        </div>
      )}

      {query.isSuccess && query.data.length === 0 && (
        <div className={styles.state}>{t('organization.empty')}</div>
      )}

      {query.isSuccess && query.data.length > 0 && (
        <Table aria-label={t('organization.title')}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>{t('organization.fields.name')}</TableHeaderCell>
              <TableHeaderCell>{t('organization.fields.type')}</TableHeaderCell>
              <TableHeaderCell>{t('organization.fields.code')}</TableHeaderCell>
              <TableHeaderCell>{t('organization.fields.status')}</TableHeaderCell>
              <TableHeaderCell>{t('organization.fields.created')}</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {query.data.map((org) => (
              <TableRow key={org.id}>
                <TableCell>
                  <TableCellLayout>{org.name}</TableCellLayout>
                </TableCell>
                <TableCell>{t(`organization.types.${org.type}`)}</TableCell>
                <TableCell>{org.code ?? '—'}</TableCell>
                <TableCell>
                  <Badge appearance="filled" color={org.active ? 'success' : 'danger'}>
                    {org.active ? t('organization.status.active') : t('organization.status.inactive')}
                  </Badge>
                </TableCell>
                <TableCell>
                  {new Date(org.createdOnUtc).toLocaleDateString(i18n.language)}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  )
}
