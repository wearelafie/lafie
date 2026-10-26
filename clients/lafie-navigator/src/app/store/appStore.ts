import { create } from 'zustand'
import { persist } from 'zustand/middleware'

// État client global de l'app (Zustand). NE contient PAS de données serveur
// (celles-ci restent gérées par TanStack Query). Uniquement de l'état UI partagé.
type AppState = {
  /** Établissement actif (sélection multi-établissement). */
  selectedFacilityId: string | null
  setSelectedFacility: (id: string | null) => void
  /** Bâtiment actif (navigation vers ses étages). */
  selectedBuildingId: string | null
  setSelectedBuilding: (id: string | null) => void
  /** Étage actif (navigation vers ses espaces). */
  selectedFloorId: string | null
  setSelectedFloor: (id: string | null) => void
  /** Espace actif (navigation vers son détail). */
  selectedSpaceId: string | null
  setSelectedSpace: (id: string | null) => void
  /** Service actif (navigation vers ses unités de soins). */
  selectedServiceId: string | null
  setSelectedService: (id: string | null) => void
  /** Unité de soins active (navigation vers son détail / ses espaces). */
  selectedCareUnitId: string | null
  setSelectedCareUnit: (id: string | null) => void
  /** Onglet à ouvrir sur la prochaine fiche infra (drill vers les enfants). Consommé puis remis à null. */
  infraTab: string | null
  setInfraTab: (tab: string | null) => void
}

export const useAppStore = create<AppState>()(
  persist(
    (set) => ({
      selectedFacilityId: null,
      setSelectedFacility: (id) => set({ selectedFacilityId: id }),
      selectedBuildingId: null,
      setSelectedBuilding: (id) => set({ selectedBuildingId: id }),
      selectedFloorId: null,
      setSelectedFloor: (id) => set({ selectedFloorId: id }),
      selectedSpaceId: null,
      setSelectedSpace: (id) => set({ selectedSpaceId: id }),
      selectedServiceId: null,
      setSelectedService: (id) => set({ selectedServiceId: id }),
      selectedCareUnitId: null,
      setSelectedCareUnit: (id) => set({ selectedCareUnitId: id }),
      infraTab: null,
      setInfraTab: (tab) => set({ infraTab: tab }),
    }),
    { name: 'lafie.app' },
  ),
)
