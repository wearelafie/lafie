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
  // Réponse sans corps (204 No Content, ou corps vide) : rien à parser.
  if (res.status === 204) return undefined as T
  const text = await res.text()
  return (text ? JSON.parse(text) : undefined) as T
}

// Erreur enrichie pour les écritures (extrait le message { error } du backend).
async function ensureOk(res: Response): Promise<void> {
  if (res.status === 401) { onUnauthorized(); throw new Error('HTTP 401') }
  if (res.ok) return
  let message = `HTTP ${res.status}`
  try { const d = (await res.json()) as { error?: string; title?: string }; message = d.error ?? d.title ?? message } catch { /* garde le code */ }
  throw new Error(message)
}

/** PUT JSON authentifié (réponse sans corps attendue). */
export async function putJson(url: string, body: unknown): Promise<void> {
  const res = await fetch(url, { method: 'PUT', headers: { 'Content-Type': 'application/json', ...authHeader() }, body: JSON.stringify(body) })
  await ensureOk(res)
}

/** DELETE authentifié. */
export async function del(url: string): Promise<void> {
  const res = await fetch(url, { method: 'DELETE', headers: { ...authHeader() } })
  await ensureOk(res)
}
