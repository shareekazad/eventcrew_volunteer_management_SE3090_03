import { useContext } from 'react'
import { DemoRoleContext } from './demoRoleContext'

export function useDemoRole() {
  const context = useContext(DemoRoleContext)
  if (!context) throw new Error('useDemoRole must be used within a DemoRoleProvider')
  return context
}
