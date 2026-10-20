import { render, screen } from '@testing-library/react'
import { FluentProvider, webLightTheme } from '@fluentui/react-components'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { beforeEach, expect, test, vi } from 'vitest'
import '@shared/i18n'
import { OrganizationsPage } from '@features/organization'

const paged = {
  items: [
    {
      id: '1', name: 'CHU Sylvanus Olympio', type: 'Hospital', code: 'CHU-SO', status: 'Active',
      shortName: null, description: null, phone: null, email: null, timezone: 'Africa/Lome',
      city: 'Lomé', country: 'Togo', parentId: null, createdOnUtc: '2026-01-01T00:00:00Z', updatedOnUtc: null,
    },
  ],
  page: 1, pageSize: 10, total: 1, totalPages: 1,
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn(async () => ({ ok: true, json: async () => paged })) as unknown as typeof fetch)
})

function renderPage() {
  const queryClient = new QueryClient()
  return render(
    <FluentProvider theme={webLightTheme}>
      <QueryClientProvider client={queryClient}>
        <OrganizationsPage />
      </QueryClientProvider>
    </FluentProvider>,
  )
}

test('affiche les établissements chargés', async () => {
  renderPage()
  expect(await screen.findByText('CHU Sylvanus Olympio')).toBeInTheDocument()
  expect(await screen.findByText('Lomé')).toBeInTheDocument()
})
