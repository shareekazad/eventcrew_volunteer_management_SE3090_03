import { createContext } from 'react'
import type { DemoRole } from './types/demoRole'

export type DemoRoleContextValue = {
  role: DemoRole | null
  selectRole: (role: DemoRole) => void
  logout: () => void
}

export const DemoRoleContext = createContext<DemoRoleContextValue | null>(null)
