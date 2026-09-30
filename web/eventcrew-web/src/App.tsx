import { useEffect, useState } from 'react'
import AssignmentManagementPage from './pages/organizer/AssignmentManagementPage'
import ShiftManagementPage from './pages/organizer/ShiftManagementPage'
import AttendanceManagementPage from './pages/organizer/AttendanceManagementPage'
import './eventcrew.css'

function App() {
  const [activePage, setActivePage] = useState(() => getPage(window.location.hash))

  useEffect(() => {
    const updatePage = () => setActivePage(getPage(window.location.hash))
    window.addEventListener('hashchange', updatePage)
    return () => window.removeEventListener('hashchange', updatePage)
  }, [])

  if (activePage === 'assignments') return <AssignmentManagementPage />
  if (activePage === 'attendance') return <AttendanceManagementPage />
  return <ShiftManagementPage />
}

function getPage(hash: string): 'shifts' | 'assignments' | 'attendance' {
  if (hash === '#assignments') return 'assignments'
  if (hash === '#attendance') return 'attendance'
  return 'shifts'
}

export default App
