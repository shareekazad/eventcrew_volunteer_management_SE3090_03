import { CalendarDays, ClipboardList, Clock3, LayoutDashboard, LogOut, Settings, Users, ClipboardCheck, ArrowRightLeft } from 'lucide-react'
import { useAuth } from '../auth/useAuth'

type OrganizerSidebarProps = { activePage: 'shifts' | 'assignments' | 'attendance' | 'swaps' }

export default function OrganizerSidebar({ activePage }: OrganizerSidebarProps) {
  const { user, logout } = useAuth()
  const initials = user?.fullName.split(/\s+/).map((part) => part[0]).slice(0, 2).join('').toUpperCase() ?? 'EC'
  return (
    <aside className="sidebar">
      <a className="brand" href="#shifts" aria-label="EventCrew organizer home"><span className="brand-mark"><CalendarDays size={18} /></span><span>eventcrew<span className="brand-period">.</span></span></a>
      <div className="workspace-label">ORGANIZER WORKSPACE</div>
      <nav className="side-nav" aria-label="Organizer navigation">
        <a href="#overview"><LayoutDashboard size={18} />Overview</a>
        <a href="#events"><CalendarDays size={18} />My events</a>
        <a className={activePage === 'shifts' ? 'active' : undefined} href="#shifts" aria-current={activePage === 'shifts' ? 'page' : undefined}><Clock3 size={18} />Shift management</a>
        <a className={activePage === 'assignments' ? 'active' : undefined} href="#assignments" aria-current={activePage === 'assignments' ? 'page' : undefined}><Users size={18} />Assignments</a>
        <a className={activePage === 'swaps' ? 'active' : undefined} href="#swaps" aria-current={activePage === 'swaps' ? 'page' : undefined}><ArrowRightLeft size={18} />Shift Swaps</a>
        <a className={activePage === 'attendance' ? 'active' : undefined} href="#attendance" aria-current={activePage === 'attendance' ? 'page' : undefined}><ClipboardCheck size={18} />Attendance</a>
        <a href="#requirements"><ClipboardList size={18} />Requirements</a>
      </nav>
      <div className="sidebar-bottom"><a href="#settings"><Settings size={18} />Settings</a><div className="organizer-profile"><div className="avatar">{initials}</div><div><strong>{user?.fullName}</strong><span>{user?.role}</span></div><button className="sidebar-logout" type="button" aria-label="Sign out" title="Sign out" onClick={logout}><LogOut size={17} /></button></div></div>
    </aside>
  )
}
