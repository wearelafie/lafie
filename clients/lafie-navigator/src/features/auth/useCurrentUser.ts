import { useQuery } from '@tanstack/react-query'
import { getMe, getToken } from './api'

/** Profil de l'utilisateur connecté (via /me), chargé si un jeton est présent. */
export function useCurrentUser() {
  const query = useQuery({
    queryKey: ['me'],
    queryFn: getMe,
    enabled: !!getToken(),
    staleTime: 5 * 60 * 1000,
    retry: false,
  })
  return { me: query.data, isLoading: query.isLoading, isError: query.isError }
}
