import { render, screen } from '@testing-library/react'
import { FluentProvider, webLightTheme } from '@fluentui/react-components'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider, createRootRoute, createRouter, createMemoryHistory } from '@tanstack/react-router'
import { beforeEach, expect, test, vi } from 'vitest'
import '@shared/i18n'
import { OrganizationsPage } from '@features/organization'

const paged = {
  items: [
    {
      id: '1', code: 'CHU-SO', officialName: 'CHU Sylvanus Olympio', displayName: null, abbreviation: null,
      description: null, facilityTypeCode: 'HOSPITAL', ownershipTypeCode: 'PUBLIC', logoUrl: null, websiteUrl: null,
      establishedOn: null, parentOrganizationId: null, legalName: null, legalEntityTypeCode: null, registrationNumber: null,
      nationalFacilityCode: null, licenseNumber: null, licenseIssuedOn: null, licenseExpiresOn: null, accreditationStatusCode: null,
      taxIdentificationNumber: null, countryCode: 'TG', regionCode: null, prefectureCode: null, communeCode: null,
      locality: 'Lomé', districtOrNeighborhood: null, streetAddress: null, postalCode: null, latitude: null, longitude: null,
      altitudeMeters: null, locationAccuracy: null, timeZoneId: 'Africa/Lome', openingDate: null, defaultCurrencyCode: 'XOF',
      defaultLanguageCode: 'fr', emergencyServicesAvailable: false, publicFacing: false, operationalStatus: 'Active',
      configurationStatus: 'Draft', phone: null, email: null, createdOnUtc: '2026-01-01T00:00:00Z', updatedOnUtc: null,
    },
  ],
  page: 1, pageSize: 10, total: 1, totalPages: 1,
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn(async () => ({ ok: true, json: async () => paged })) as unknown as typeof fetch)
})

function renderPage() {
  const queryClient = new QueryClient()
  const rootRoute = createRootRoute({ component: OrganizationsPage })
  const router = createRouter({ routeTree: rootRoute, history: createMemoryHistory({ initialEntries: ['/'] }) })
  return render(
    <FluentProvider theme={webLightTheme}>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </FluentProvider>,
  )
}

test('affiche les établissements chargés', async () => {
  renderPage()
  expect(await screen.findByText('CHU Sylvanus Olympio')).toBeInTheDocument()
  expect(await screen.findByText('Lomé')).toBeInTheDocument()
})
