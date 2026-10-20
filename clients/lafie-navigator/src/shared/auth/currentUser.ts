// Profil utilisateur — STUB en attendant l'auth OIDC/SMART (module Identity).
// À remplacer par le contexte d'auth réel (token/claims) une fois branché.
export type CurrentUser = {
  name: string
  username: string
  role: string
}

export const currentUser: CurrentUser = {
  name: 'Horacio sKrp',
  username: '@horacioskrp',
  role: 'Administrateur',
}
