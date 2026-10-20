import { beforeEach, expect, test } from 'vitest'
import { useAppStore } from './appStore'

beforeEach(() => useAppStore.setState({ selectedFacilityId: null }))

test('selectedFacilityId par défaut est null', () => {
  expect(useAppStore.getState().selectedFacilityId).toBeNull()
})

test('setSelectedFacility met à jour la sélection', () => {
  useAppStore.getState().setSelectedFacility('abc-123')
  expect(useAppStore.getState().selectedFacilityId).toBe('abc-123')
  useAppStore.getState().setSelectedFacility(null)
  expect(useAppStore.getState().selectedFacilityId).toBeNull()
})
