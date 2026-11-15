import { useAppStore } from '@app/store/appStore'
import { useCurrentUser } from './useCurrentUser'

/**
 * Établissement actif courant — source unique de vérité pour tout le scoping multi-tenant côté client.
 *
 * - Utilisateur **scoped** (admin/agent d'établissement) : l'établissement actif provient du profil
 *   (jeton scopé émis au login / au changement d'établissement). Il prime toujours.
 * - **Admin système** (permission `*`, aucune appartenance) : l'établissement est choisi localement
 *   via le sélecteur de la TopBar (`selectedFacilityId` du store), le backend l'autorisant via le bypass `*`.
 *
 * Retourne `null` tant qu'aucun établissement n'est déterminé (profil non chargé, ou admin système
 * n'ayant pas encore choisi) — les pages affichent alors leur invite « sélectionnez un établissement ».
 */
export function useActiveFacility(): string | null {
  const { me } = useCurrentUser()
  const storeFacilityId = useAppStore((s) => s.selectedFacilityId)
  return me?.activeEstablishment?.facilityId ?? storeFacilityId
}
