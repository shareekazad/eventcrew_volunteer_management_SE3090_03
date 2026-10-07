import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import {
  Calendar,
  LayoutGrid,
  Clock,
  Users,
  ArrowLeftRight,
  ClipboardList,
  Sparkles,
  Radio,
  LogOut,
} from 'lucide-react';
import { useAuthStore } from '../stores/authStore';

/** Extract up-to-2-letter initials from a full name */
const getInitials = (name: string): string => {
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
};

export const Sidebar: React.FC = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  const navItemClass = (isActive: boolean) =>
    `flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 ${
      isActive
        ? 'bg-[#1F483C] text-white border-l-4 border-mint pl-3'
        : 'text-[#9BB8AC] hover:text-white hover:bg-[#163D30]'
    }`;

  const displayName = user?.fullName ?? 'Organizer';
  const displayRole = user?.role ?? 'Organizer';
  const initials = getInitials(displayName);

  return (
    <aside className="w-64 min-h-screen bg-[#0F382C] text-white flex flex-col shrink-0 select-none border-r border-[#0D3025]">
      {/* Brand Header */}
      <div className="p-6 pb-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-mint flex items-center justify-center text-[#0F382C] shadow-sm shrink-0">
            <Calendar className="w-5 h-5" />
          </div>
          <div className="flex items-center">
            <span className="font-bold text-xl tracking-tight text-white">eventcrew.</span>
          </div>
        </div>
      </div>

      {/* Nav section */}
      <div className="flex-1 px-4 py-2 space-y-6 overflow-y-auto">
        <div>
          <div className="px-3 mb-3 text-[11px] font-semibold tracking-wider uppercase text-[#7AA493]">
            Workspace
          </div>
          <nav className="space-y-1.5">
            {/* Core Links */}
            <NavLink
              to="/dashboard"
              className={({ isActive }) => navItemClass(isActive)}
            >
              <LayoutGrid className="w-4 h-4 shrink-0" />
              <span>Dashboard</span>
            </NavLink>

            <NavLink
              to="/shifts"
              className={({ isActive }) => navItemClass(isActive)}
            >
              <Clock className="w-4 h-4 shrink-0" />
              <span>Shift management</span>
            </NavLink>

            <NavLink
              to="/assignments"
              className={({ isActive }) => navItemClass(isActive)}
            >
              <Users className="w-4 h-4 shrink-0" />
              <span>Assignments</span>
            </NavLink>

            <NavLink
              to="/shift-swaps"
              className={({ isActive }) => navItemClass(isActive)}
            >
              <ArrowLeftRight className="w-4 h-4 shrink-0" />
              <span>Shift Swaps</span>
            </NavLink>

            {/* Student 2 Module Links */}
            <NavLink
              to="/applicants"
              className={({ isActive }) => navItemClass(isActive)}
            >
              <ClipboardList className="w-4 h-4 shrink-0" />
              <span>Volunteer Applicants</span>
            </NavLink>

            <NavLink
              to="/ai-matching"
              className={({ isActive }) => navItemClass(isActive)}
            >
              <Sparkles className="w-4 h-4 shrink-0 text-mint" />
              <span>AI Staffing Matcher</span>
            </NavLink>
          </nav>
        </div>

        {/* Backend status widget */}
        <div className="mx-1 px-3 py-2.5 rounded-xl bg-[#143E31] border border-[#1C4B3D] text-xs text-[#9BB8AC] space-y-1.5">
          <div className="flex items-center justify-between">
            <span className="flex items-center gap-1.5 text-white font-medium text-[11px]">
              <Radio className="w-3 h-3 text-mint animate-pulse" />
              API: 5100
            </span>
          </div>
          <div className="text-[10px] text-[#7AA493] truncate">
            Event: TechFest 2026
          </div>
        </div>
      </div>

      {/* User Profile Card at bottom */}
      <div className="p-4 border-t border-[#1C4638]">
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-3 min-w-0">
            <div className="w-9 h-9 rounded-full bg-mint text-[#0F382C] font-bold text-xs flex items-center justify-center shrink-0">
              {initials}
            </div>
            <div className="overflow-hidden">
              <div className="text-sm font-semibold text-white leading-tight truncate" title={displayName}>
                {displayName}
              </div>
              <div className="text-xs text-[#7AA493] leading-tight">
                {displayRole}
              </div>
            </div>
          </div>

          {/* Logout Button */}
          <button
            id="sidebar-logout-btn"
            onClick={handleLogout}
            title="Log out"
            aria-label="Log out"
            className="shrink-0 p-2 rounded-lg text-[#7AA493] hover:text-red-400 hover:bg-red-900/20 transition-all duration-150"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </div>
    </aside>
  );
};
