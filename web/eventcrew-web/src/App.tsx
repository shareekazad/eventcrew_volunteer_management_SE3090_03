import { useEffect, useState } from 'react'
import AssignmentManagementPage from './pages/organizer/AssignmentManagementPage'
import ShiftManagementPage from './pages/organizer/ShiftManagementPage'
import './eventcrew.css'

function App() {
  const [activePage, setActivePage] = useState(() => window.location.hash === '#assignments' ? 'assignments' : 'shifts')

  useEffect(() => {
    const updatePage = () => setActivePage(window.location.hash === '#assignments' ? 'assignments' : 'shifts')
    window.addEventListener('hashchange', updatePage)
    return () => window.removeEventListener('hashchange', updatePage)
  }, [])

  return activePage === 'assignments' ? <AssignmentManagementPage /> : <ShiftManagementPage />
}

export default App
