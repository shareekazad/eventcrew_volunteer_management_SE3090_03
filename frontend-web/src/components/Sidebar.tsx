import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import {
  Calendar,
  CalendarPlus,
  LayoutGrid,
  Clock,
  Users,
  ArrowLeftRight,
  ClipboardList,
  Sparkles,
  Shield,
  Radio,
  Check,
  CalendarDays,
} from 'lucide-react';
import { getDevOrganizerToken } from '../services/api';
import { useApplicationStore } from '../stores/applicationStore';

export const Sidebar: React.FC = () => {
  const [tokenCopied, setTokenCopied] = useState(false);
  const fetchApplicants = useApplicationStore((state) => state.fetchApplicants);

  const copyToken = () => {
    const token = getDevOrganizerToken();
    localStorage.setItem('token', token);
    navigator.clipboard.writeText(token);
    setTokenCopied(true);
    setTimeout(() => setTokenCopied(false), 2000);
    fetchApplicants();
  };

  const navItemClass = (isActive: boolean) =>
    `flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 ${
      isActive
        ? 'bg-[#1F483C] text-white border-l-4 border-mint pl-3'
        : 'text-[#9BB8AC] hover:text-white hover:bg-[#163D30]'
    }`;

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
            Development Preview
          </div>
          <nav className="space-y-1.5">
            {/* Core Links */}
            <NavLink
              to="/dashboard"
              className={({ isActive }) => navItemClass(isActive)}
              onClick={(e) => {
                // If not implemented, navigate to applicants
                if (window.location.pathname === '/dashboard') e.preventDefault();
              }}
            >
              <LayoutGrid className="w-4 h-4 shrink-0" />
              <span>Dashboard</span>
            </NavLink>

            {/* ── Student 1 — Events Module ── */}
            <NavLink
              to="/events"
              end
              className={({ isActive }) => navItemClass(isActive)}
            >
              <CalendarDays className="w-4 h-4 shrink-0" />
              <span>Events</span>
            </NavLink>

            <NavLink
              to="/events/new"
              className={({ isActive }) => navItemClass(isActive)}
            >
              <CalendarPlus className="w-4 h-4 shrink-0" />
              <span>Create Event</span>
            </NavLink>

            {/* ── Shift / Assignment links (Student 3) ── */}
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

            {/* ── Student 2 Module Links ── */}
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
            <button
              onClick={copyToken}
              className="flex items-center gap-1 px-2 py-0.5 rounded-md bg-[#1F4E3E] hover:bg-[#275F4C] text-mint text-[10px] font-semibold transition-colors"
              title="Copy Dev Organizer JWT"
            >
              {tokenCopied ? (
                <>
                  <Check className="w-2.5 h-2.5" />
                  Copied
                </>
              ) : (
                <>
                  <Shield className="w-2.5 h-2.5" />
                  JWT
                </>
              )}
            </button>
          </div>
          <div className="text-[10px] text-[#7AA493] truncate">
            Event: TechFest 2026
          </div>
        </div>
      </div>

      {/* User Profile Card at bottom */}
      <div className="p-4 border-t border-[#1C4638] flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-full bg-mint text-[#0F382C] font-bold text-xs flex items-center justify-center shrink-0">
            PO
          </div>
          <div className="overflow-hidden">
            <div className="text-sm font-semibold text-white leading-tight truncate">
              Preview Organizer
            </div>
            <div className="text-xs text-[#7AA493] leading-tight">
              Organizer
            </div>
          </div>
        </div>
      </div>
    </aside>
  );
};