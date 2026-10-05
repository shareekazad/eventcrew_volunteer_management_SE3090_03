import { useEffect, useState } from 'react'
import AssignmentManagementPage from './pages/organizer/AssignmentManagementPage'
import ShiftManagementPage from './pages/organizer/ShiftManagementPage'
import AttendanceManagementPage from './pages/organizer/AttendanceManagementPage'
import ShiftSwapsPage from './pages/organizer/ShiftSwapsPage'
import LoginPage from './pages/LoginPage'
import { AuthProvider } from './auth/AuthProvider'
import { useAuth } from './auth/useAuth'
import { DemoRoleProvider } from './demoRoleProvider'
import { useDemoRole } from './useDemoRole'
import { ArrowRight, CalendarDays, LogOut } from 'lucide-react'
import OrganizerDashboardPreview from './pages/dev/OrganizerDashboardPreview'
import './eventcrew.css'

function App() {
  if (import.meta.env.DEV && ['/dev/dashboard-preview', '/dev-preview'].includes(window.location.pathname)) {
    return <OrganizerDashboardPreview />
  }

  return <AuthProvider><DemoRoleProvider><AppContent /></DemoRoleProvider></AuthProvider>
}

function AppContent() {
  const [activePage, setActivePage] = useState(() => getPage(window.location.hash))
  const { role, logout: logoutDemo } = useDemoRole()
  const { user, isInitializing, logout: logoutAuth } = useAuth()

  useEffect(() => {
    const updatePage = () => setActivePage(getPage(window.location.hash))
    window.addEventListener('hashchange', updatePage)
    return () => window.removeEventListener('hashchange', updatePage)
  }, [])

  if (isInitializing) return <main className="auth-loading" aria-label="Loading session"><span className="brand-mark"><CalendarDays /></span></main>
  if (!user && !role) return <LoginPage />
  const activeRole = user?.role ?? role
  const logout = user ? logoutAuth : logoutDemo
  if (activeRole === 'Volunteer') {
    return (
      <main className="auth-screen">
        <section className="auth-card" aria-labelledby="volunteer-title">
          <div className="auth-brand"><span className="brand-mark"><CalendarDays size={18} /></span>eventcrew<span className="brand-period">.</span></div>
          <p className="eyebrow">VOLUNTEER DEMO</p>
          <h1 id="volunteer-title">Welcome, Volunteer</h1>
          <p className="auth-subtitle">Use the EventCrew Volunteer mobile app to discover events, view shifts, manage assignments, and request swaps.</p>
          <button className="button button-secondary auth-submit" type="button" onClick={logout}><ArrowRight size={17} /> Switch role</button>
        </section>
      </main>
    )
  }
  const page = activePage === 'login' || activePage === 'volunteer' ? 'shifts' : activePage
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

function getPage(hash: string): 'shifts' | 'assignments' | 'attendance' | 'swaps' | 'login' | 'volunteer' {
  if (hash === '#login') return 'login'
  if (hash === '#volunteer') return 'volunteer'
  if (hash === '#assignments') return 'assignments'
  if (hash === '#attendance') return 'attendance'
  if (hash === '#swaps') return 'swaps'
  return 'shifts'
}

export default App
