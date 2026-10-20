// Wrapper HTTP minimal. À enrichir (intercepteur JWT, ProblemDetails, base URL)
// et/ou remplacer par le client OpenAPI généré (voir docs/frontend/README.md §À venir).
export async function getJson<T>(url: string): Promise<T> {
  const res = await fetch(url)
  if (!res.ok) throw new Error(`HTTP ${res.status}`)
  return res.json() as Promise<T>
}

export async function postJson<T>(url: string, body: unknown): Promise<T> {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
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
