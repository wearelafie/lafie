// Wrapper HTTP minimal. À enrichir (ProblemDetails, base URL) et/ou remplacer
// par le client OpenAPI généré (voir docs/frontend/README.md §À venir).

// Jeton JWT (même clé que features/auth/api.ts ; lu ici pour éviter un import circulaire).
function authHeader(): Record<string, string> {
  try {
    const token = localStorage.getItem('lafie.auth.token')
    return token ? { Authorization: `Bearer ${token}` } : {}
  } catch {
    return {}
  }
}

// Jeton expiré/invalide : purge + retour à la connexion (sauf si on y est déjà).
function onUnauthorized(): void {
  try { localStorage.removeItem('lafie.auth.token') } catch { /* indisponible */ }
  if (typeof location !== 'undefined' && !location.pathname.startsWith('/login')) {
    location.assign('/login')
  }
}

export async function getJson<T>(url: string): Promise<T> {
  const res = await fetch(url, { headers: { ...authHeader() } })
  if (res.status === 401) { onUnauthorized(); throw new Error('HTTP 401') }
  if (!res.ok) throw new Error(`HTTP ${res.status}`)
  return res.json() as Promise<T>
}

export async function postJson<T>(url: string, body: unknown): Promise<T> {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeader() },
    body: JSON.stringify(body),
  })
  if (res.status === 401) {
    // Sur la page de connexion, 401 = identifiants invalides (pas une expiration) : on laisse remonter.
    if (typeof location === 'undefined' || !location.pathname.startsWith('/login')) onUnauthorized()
    throw new Error('HTTP 401')
  }
  if (!res.ok) {
    let message = `HTTP ${res.status}`
    try {
      const data = (await res.json()) as { error?: string }
      if (data?.error) message = data.error
    } catch {
      /* corps non JSON : on garde le code HTTP */
    }
    throw new Error(message)
  }
  return res.json() as Promise<T>
}
