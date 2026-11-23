import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getOrganizations, organizationKeys } from '@features/organization/api'
import { isGlobalAdmin } from './api'
import { useActiveFacility } from './useActiveFacility'
import { useCurrentUser } from './useCurrentUser'

export type FacilityOption = { id: string; label: string }

/**
 * Sélection d'établissement pour les formulaires de création des ressources rattachées à un
 * établissement (équipement, GMAO, hygiène, stérilisation, déchets). L'admin d'établissement est
 * figé sur son établissement actif ; l'admin système peut choisir n'importe lequel (les cibles —
 * espace, équipement — doivent alors être re-scopées sur l'établissement choisi par l'appelant).
 *
 * `facilityId` suit l'établissement actif tant que l'admin système n'a pas choisi autrement ;
 * `reset()` le réaligne (à appeler à l'ouverture d'un dialog pour repartir de l'actif).
 */
export function useFacilitySelector() {
  const { me } = useCurrentUser()
  const activeFacilityId = useActiveFacility()
  const globalAdmin = isGlobalAdmin(me)
  const [facilityId, setFacilityId] = useState<string | null>(activeFacilityId)

  // Rester aligné sur l'établissement actif (switcher TopBar) tant qu'aucun choix local n'est fait.
  useEffect(() => { setFacilityId(activeFacilityId) }, [activeFacilityId])

  const orgs = useQuery({
    queryKey: organizationKeys.list({ page: 1, pageSize: 100 }),
    queryFn: () => getOrganizations({ page: 1, pageSize: 100 }),
    enabled: globalAdmin,
    staleTime: 5 * 60 * 1000,
  })

  const options: FacilityOption[] = (orgs.data?.items ?? []).map((o) => ({
    id: o.id,
    label: o.displayName ?? o.officialName,
  }))
  const establishmentName = options.find((o) => o.id === facilityId)?.label
    ?? me?.activeEstablishment?.name
    ?? ''

  return {
    facilityId,
    setFacilityId,
    reset: () => setFacilityId(activeFacilityId),
    globalAdmin,
    optionsLoading: orgs.isLoading,
    options,
    establishmentName,
  }
}
