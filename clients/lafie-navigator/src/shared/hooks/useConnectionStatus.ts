import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getJson } from '@shared/api/http'

export type ConnectionStatus = 'online' | 'offline' | 'connecting'

// Statut de connexion de l'app : combine l'état réseau du navigateur
// et un ping périodique de l'API (/api/health).
export function useConnectionStatus(): ConnectionStatus {
  const [browserOnline, setBrowserOnline] = useState(() =>
    typeof navigator === 'undefined' ? true : navigator.onLine,
  )

  useEffect(() => {
    const on = () => setBrowserOnline(true)
    const off = () => setBrowserOnline(false)
    window.addEventListener('online', on)
    window.addEventListener('offline', off)
    return () => {
      window.removeEventListener('online', on)
      window.removeEventListener('offline', off)
    }
  }, [])

  const health = useQuery({
    queryKey: ['connection-health'],
    queryFn: () => getJson<{ status: string }>('/api/health'),
    refetchInterval: 30_000,
    staleTime: 15_000,
    retry: false,
  })

  if (!browserOnline || health.isError) return 'offline'
  if (health.isSuccess) return 'online'
  return 'connecting'
}
