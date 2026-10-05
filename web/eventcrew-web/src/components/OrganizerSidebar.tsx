import { CalendarDays, ClipboardList, Clock3, LayoutDashboard, LogOut, Settings, Users, ClipboardCheck, ArrowRightLeft } from 'lucide-react'
import { useDemoRole } from '../useDemoRole'

type OrganizerSidebarProps = { activePage: 'shifts' | 'assignments' | 'attendance' | 'swaps'; previewMode?: boolean }

export default function OrganizerSidebar({ activePage, previewMode = false }: OrganizerSidebarProps) {
  const { role, logout } = useDemoRole()
  const initials = role === 'Organizer' ? 'OR' : 'VO'
  return (
    <aside className="sidebar">
      <a className="brand" href="#shifts" aria-label="EventCrew organizer home"><span className="brand-mark"><CalendarDays size={18} /></span><span>eventcrew<span className="brand-period">.</span></span></a>
      <div className="workspace-label">{previewMode ? 'DEVELOPMENT PREVIEW' : 'ORGANIZER WORKSPACE'}</div>
      <nav className="side-nav" aria-label="Organizer navigation">
        {previewMode && <a className={activePage === 'shifts' && window.location.hash === '#overview' ? 'active' : undefined} href="#overview"><LayoutDashboard size={18} />Dashboard</a>}
        {!previewMode && <><a href="#overview"><LayoutDashboard size={18} />Overview</a><a href="#events"><CalendarDays size={18} />My events</a></>}
        <a className={activePage === 'shifts' && (!previewMode || window.location.hash !== '#overview') ? 'active' : undefined} href="#shifts" aria-current={activePage === 'shifts' && (!previewMode || window.location.hash !== '#overview') ? 'page' : undefined}><Clock3 size={18} />Shift management</a>
        <a className={activePage === 'assignments' ? 'active' : undefined} href="#assignments" aria-current={activePage === 'assignments' ? 'page' : undefined}><Users size={18} />Assignments</a>
        <a className={activePage === 'swaps' ? 'active' : undefined} href="#swaps" aria-current={activePage === 'swaps' ? 'page' : undefined}><ArrowRightLeft size={18} />Shift Swaps</a>
        {!previewMode && <><a className={activePage === 'attendance' ? 'active' : undefined} href="#attendance" aria-current={activePage === 'attendance' ? 'page' : undefined}><ClipboardCheck size={18} />Attendance</a><a href="#requirements"><ClipboardList size={18} />Requirements</a></>}
      </nav>
      <div className="sidebar-bottom">{!previewMode && <a href="#settings"><Settings size={18} />Settings</a>}<div className="organizer-profile"><div className="avatar">{initials}</div><div><strong>Demo Organizer</strong><span>{role ?? 'Organizer'}</span></div>{!previewMode && <button className="sidebar-logout" type="button" aria-label="Switch role" title="Switch role" onClick={logout}><LogOut size={17} /></button>}</div></div>
    </aside>
  )
}
