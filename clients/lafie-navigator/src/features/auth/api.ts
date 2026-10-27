import { postJson } from '@shared/api/http'

// Stockage du jeton d'accès (JWT). Phase 1 : émis par POST /api/auth/login.
const TOKEN_KEY = 'lafie.auth.token'

export function getToken(): string | null {
  try { return localStorage.getItem(TOKEN_KEY) } catch { return null }
}
export function setToken(token: string): void {
  try { localStorage.setItem(TOKEN_KEY, token) } catch { /* stockage indisponible */ }
}
export function clearToken(): void {
  try { localStorage.removeItem(TOKEN_KEY) } catch { /* stockage indisponible */ }
}

export type LoginInput = { email: string; password: string }
export type LoginResult = { token: string }

/** Authentifie l'utilisateur et renvoie un jeton. Backend : module Identity (Phase 1). */
export async function login(input: LoginInput): Promise<LoginResult> {
  return postJson<LoginResult>('/api/auth/login', input)
}
