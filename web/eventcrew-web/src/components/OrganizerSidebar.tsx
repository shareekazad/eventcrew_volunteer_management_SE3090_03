import { CalendarDays, ChevronDown, ClipboardList, Clock3, LayoutDashboard, Settings, Users, ClipboardCheck } from 'lucide-react'

type OrganizerSidebarProps = { activePage: 'shifts' | 'assignments' | 'attendance' }

export default function OrganizerSidebar({ activePage }: OrganizerSidebarProps) {
  return (
    <aside className="sidebar">
      <a className="brand" href="#shifts" aria-label="EventCrew organizer home"><span className="brand-mark"><CalendarDays size={18} /></span><span>eventcrew<span className="brand-period">.</span></span></a>
      <div className="workspace-label">ORGANIZER WORKSPACE</div>
      <nav className="side-nav" aria-label="Organizer navigation">
        <a href="#overview"><LayoutDashboard size={18} />Overview</a>
        <a href="#events"><CalendarDays size={18} />My events</a>
        <a className={activePage === 'shifts' ? 'active' : undefined} href="#shifts" aria-current={activePage === 'shifts' ? 'page' : undefined}><Clock3 size={18} />Shift management</a>
        <a className={activePage === 'assignments' ? 'active' : undefined} href="#assignments" aria-current={activePage === 'assignments' ? 'page' : undefined}><Users size={18} />Assignments</a>
        <a className={activePage === 'attendance' ? 'active' : undefined} href="#attendance" aria-current={activePage === 'attendance' ? 'page' : undefined}><ClipboardCheck size={18} />Attendance</a>
        <a href="#requirements"><ClipboardList size={18} />Requirements</a>
      </nav>
      <div className="sidebar-bottom"><a href="#settings"><Settings size={18} />Settings</a><div className="organizer-profile"><div className="avatar">JM</div><div><strong>Jordan Miller</strong><span>Event organizer</span></div><ChevronDown size={15} /></div></div>
    </aside>
  )
}
