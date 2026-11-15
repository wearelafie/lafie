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
  /** Équipement actif (navigation vers sa fiche : déplacements, maintenance). */
  selectedEquipmentId: string | null
  setSelectedEquipment: (id: string | null) => void
  /** Onglet à ouvrir sur la prochaine fiche infra (drill vers les enfants). Consommé puis remis à null. */
  infraTab: string | null
  setInfraTab: (tab: string | null) => void
  /** Cible du formulaire établissement (page) : null = création, id = édition. */
  facilityFormId: string | null
  setFacilityFormId: (id: string | null) => void
  /** Cible du formulaire équipement (page) : null = création, id = édition. */
  equipmentFormId: string | null
  setEquipmentFormId: (id: string | null) => void
  /** Ticket de maintenance (GMAO) actif : navigation vers sa fiche détail. */
  selectedTicketId: string | null
  setSelectedTicket: (id: string | null) => void
  /** Tâche de bio-nettoyage (hygiène) active : navigation vers sa fiche détail. */
  selectedCleaningTaskId: string | null
  setSelectedCleaningTask: (id: string | null) => void
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
      selectedEquipmentId: null,
      setSelectedEquipment: (id) => set({ selectedEquipmentId: id }),
      infraTab: null,
      setInfraTab: (tab) => set({ infraTab: tab }),
      facilityFormId: null,
      setFacilityFormId: (id) => set({ facilityFormId: id }),
      equipmentFormId: null,
      setEquipmentFormId: (id) => set({ equipmentFormId: id }),
      selectedTicketId: null,
      setSelectedTicket: (id) => set({ selectedTicketId: id }),
      selectedCleaningTaskId: null,
      setSelectedCleaningTask: (id) => set({ selectedCleaningTaskId: id }),
    }),
    { name: 'lafie.app' },
  ),
)
