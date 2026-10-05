import { useEffect, useState } from 'react'
import { DemoRoleContext } from '../../demoRoleContext'
import type { DemoRoleContextValue } from '../../demoRoleContext'
import AssignmentManagementPage from '../organizer/AssignmentManagementPage'
import ShiftManagementPage from '../organizer/ShiftManagementPage'
import ShiftSwapsPage from '../organizer/ShiftSwapsPage'

const previewRole: DemoRoleContextValue = {
  role: 'Organizer',
  selectRole: () => {},
  logout: () => {},
}

export default function OrganizerDashboardPreview() {
  const [activePage, setActivePage] = useState(() => getPreviewPage(window.location.hash))

  useEffect(() => {
    const updatePage = () => setActivePage(getPreviewPage(window.location.hash))
    window.addEventListener('hashchange', updatePage)
    return () => window.removeEventListener('hashchange', updatePage)
  }, [])

  return (
    <DemoRoleContext.Provider value={previewRole}>
      {activePage === 'assignments' ? (
        <AssignmentManagementPage previewMode />
      ) : activePage === 'swaps' ? (
        <ShiftSwapsPage previewMode />
      ) : (
        <ShiftManagementPage previewMode />
      )}
    </DemoRoleContext.Provider>
  )
}

function getPreviewPage(hash: string): 'shifts' | 'assignments' | 'swaps' {
  if (hash === '#assignments') return 'assignments'
  if (hash === '#swaps') return 'swaps'
  return 'shifts'
}
