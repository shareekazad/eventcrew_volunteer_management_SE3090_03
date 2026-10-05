import { useEffect, useState } from 'react'
import AssignmentManagementPage from './pages/organizer/AssignmentManagementPage'
import ShiftManagementPage from './pages/organizer/ShiftManagementPage'
import AttendanceManagementPage from './pages/organizer/AttendanceManagementPage'
import ShiftSwapsPage from './pages/organizer/ShiftSwapsPage'
import LoginPage from './pages/LoginPage'
import { AuthProvider } from './auth/AuthProvider'
import { useAuth } from './auth/useAuth'
import { CalendarDays, LogOut } from 'lucide-react'
import './eventcrew.css'

function App() {
  return <AuthProvider><AppContent /></AuthProvider>
}

function AppContent() {
  const [activePage, setActivePage] = useState(() => getPage(window.location.hash))
  const { user, isInitializing, logout } = useAuth()

  useEffect(() => {
    const updatePage = () => setActivePage(getPage(window.location.hash))
    window.addEventListener('hashchange', updatePage)
    return () => window.removeEventListener('hashchange', updatePage)
  }, [])

  if (isInitializing) return <main className="auth-loading" aria-label="Loading session"><span className="brand-mark"><CalendarDays /></span></main>
  if (!user) return <LoginPage />
  const page = activePage === 'login' ? 'shifts' : activePage
  return <>
    {page === 'assignments' ? (
      <AssignmentManagementPage />
    ) : page === 'attendance' ? (
      <AttendanceManagementPage />
    ) : page === 'swaps' ? (
      <ShiftSwapsPage />
    ) : (
      <ShiftManagementPage />
    )}
    <button className="mobile-logout" type="button" aria-label="Sign out" title="Sign out" onClick={logout}><LogOut size={17} /></button>
  </>
}

function getPage(hash: string): 'shifts' | 'assignments' | 'attendance' | 'swaps' | 'login' {
  if (hash === '#login') return 'login'
  if (hash === '#assignments') return 'assignments'
  if (hash === '#attendance') return 'attendance'
  if (hash === '#swaps') return 'swaps'
  return 'shifts'
}

export default App
