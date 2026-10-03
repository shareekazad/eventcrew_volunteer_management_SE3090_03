import { createContext } from 'react'
import type { AuthenticatedUser } from '../types/auth'

export type AuthContextValue = {
  user: AuthenticatedUser | null
  isInitializing: boolean
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
