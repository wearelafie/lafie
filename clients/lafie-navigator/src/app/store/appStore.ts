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
    }),
    { name: 'lafie.app' },
  ),
)
