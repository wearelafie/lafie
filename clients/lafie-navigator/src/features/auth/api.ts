import { getJson, postJson } from '@shared/api/http'

// Stockage du jeton d'accès (JWT).
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

export type Me = {
  id: string
  username: string
  email: string
  displayName: string | null
  firstName: string | null
  lastName: string | null
  title: string | null
  userType: string
  preferredLanguage: string | null
  avatarUrl: string | null
  practitionerId: string | null
  roles: string[]
  permissions: string[]
}

export type LoginInput = { login: string; password: string }
export type LoginResult = { token: string; expiresAtUtc: string; user: Me }

/** Authentifie (e-mail ou nom d'utilisateur) et renvoie un jeton + le profil. */
export function login(input: LoginInput): Promise<LoginResult> {
  return postJson<LoginResult>('/api/auth/login', input)
}

/** Profil de l'utilisateur connecté. */
export function getMe(): Promise<Me> {
  return getJson<Me>('/api/me')
}

/** Vrai si l'utilisateur possède la permission (ou le joker '*'). */
export function hasPermission(me: Me | undefined | null, code: string): boolean {
  if (!me) return false
  return me.permissions.includes('*') || me.permissions.includes(code)
}
