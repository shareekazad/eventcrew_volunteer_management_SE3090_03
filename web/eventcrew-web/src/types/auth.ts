export type LoginRequest = {
  email: string
  password: string
}

export type AuthenticatedUser = {
  id: string
  fullName: string
  email: string
  role: string
}

export type LoginResponse = {
  accessToken: string
  expiresAt: string
  user: AuthenticatedUser
}
