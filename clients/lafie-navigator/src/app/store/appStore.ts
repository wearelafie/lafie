import { create } from 'zustand'
import { persist } from 'zustand/middleware'

// État client global de l'app (Zustand). NE contient PAS de données serveur
// (celles-ci restent gérées par TanStack Query). Uniquement de l'état UI partagé.
type AppState = {
  /** Établissement actif (sélection multi-établissement). */
  selectedFacilityId: string | null
  setSelectedFacility: (id: string | null) => void
}

export const useAppStore = create<AppState>()(
  persist(
    (set) => ({
      selectedFacilityId: null,
      setSelectedFacility: (id) => set({ selectedFacilityId: id }),
    }),
    { name: 'lafie.app' },
  ),
)
