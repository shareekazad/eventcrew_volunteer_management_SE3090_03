import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { DemoRoleContext } from './demoRoleContext'
import type { DemoRole } from './types/demoRole'

const DEMO_ROLE_KEY = 'eventcrew_demo_role'

function readRole(): DemoRole | null {
  const role = window.localStorage.getItem(DEMO_ROLE_KEY)
  return role === 'Organizer' || role === 'Volunteer' ? role : null
}

export function DemoRoleProvider({ children }: { children: ReactNode }) {
  const [role, setRole] = useState<DemoRole | null>(readRole)

  const selectRole = useCallback((selectedRole: DemoRole) => {
    window.localStorage.setItem(DEMO_ROLE_KEY, selectedRole)
    setRole(selectedRole)
  }, [])

  const logout = useCallback(() => {
    window.localStorage.removeItem(DEMO_ROLE_KEY)
    setRole(null)
    window.location.hash = '#login'
  }, [])

  const value = useMemo(() => ({ role, selectRole, logout }), [role, selectRole, logout])

  useEffect(() => {
    if (role === 'Volunteer') window.location.hash = '#volunteer'
    if (role === 'Organizer' && (!window.location.hash || window.location.hash === '#login' || window.location.hash === '#volunteer')) {
      window.location.hash = '#shifts'
    }
  }, [role])

  return <DemoRoleContext.Provider value={value}>{children}</DemoRoleContext.Provider>
}
