import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { AUTH_SESSION_EXPIRED_EVENT } from '../api/client'
import { getCurrentUser, login as loginRequest } from '../api/authService'
import { clearAccessToken, getAccessToken, storeAccessToken } from './tokenStorage'
import { AuthContext } from './authContext'
import type { AuthenticatedUser } from '../types/auth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthenticatedUser | null>(null)
  const [isInitializing, setIsInitializing] = useState(() => Boolean(getAccessToken()))

  useEffect(() => {
    if (!getAccessToken()) return
    let cancelled = false
    getCurrentUser()
      .then((currentUser) => {
        if (!cancelled) setUser(currentUser)
      })
      .catch(() => {
        if (!cancelled) setUser(null)
      })
      .finally(() => {
        if (!cancelled) setIsInitializing(false)
      })
    return () => { cancelled = true }
  }, [])

  const logout = useCallback(() => {
    clearAccessToken()
    setUser(null)
    if (window.location.hash !== '#login') window.location.hash = '#login'
  }, [])

  useEffect(() => {
    window.addEventListener(AUTH_SESSION_EXPIRED_EVENT, logout)
    return () => window.removeEventListener(AUTH_SESSION_EXPIRED_EVENT, logout)
  }, [logout])

  const login = useCallback(async (email: string, password: string) => {
    const result = await loginRequest({ email, password })
    storeAccessToken(result.accessToken)
    try {
      setUser(await getCurrentUser())
    } catch (error) {
      clearAccessToken()
      setUser(null)
      throw error
    }
  }, [])

  const value = useMemo(() => ({ user, isInitializing, isAuthenticated: user !== null, login, logout }), [user, isInitializing, login, logout])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
