import React from 'react';
import { useLocation } from 'react-router-dom';
import { ChevronRight } from 'lucide-react';
import { useAuthStore } from '../stores/authStore';

/** Extract up-to-2-letter initials from a full name */
const getInitials = (name: string): string => {
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
};

export const Header: React.FC = () => {
  const location = useLocation();
  const user = useAuthStore((s) => s.user);

  const initials = user ? getInitials(user.fullName) : '?';
  const displayName = user?.fullName ?? '';

  // Dynamic breadcrumbs based on current route
  const getBreadcrumbs = () => {
    switch (location.pathname) {
      case '/applicants':
        return { section: 'Workspace', current: 'Volunteer Applicants' };
      case '/ai-matching':
        return { section: 'AI Staffing', current: 'Affinity Matching' };
      case '/shift-swaps':
        return { section: 'Shifts', current: 'Shift Swaps' };
      default:
        return { section: 'Workspace', current: 'Volunteer Management' };
    }
  };

  const { section, current } = getBreadcrumbs();

  return (
    <header className="h-14 bg-white border-b border-slate-200/80 px-8 flex items-center justify-between sticky top-0 z-30 shadow-none">
      {/* Breadcrumb Navigation */}
      <nav className="flex items-center space-x-2 text-xs text-slate-500">
        <span className="hover:text-slate-800 transition-colors cursor-pointer font-medium">
          {section}
        </span>
        <ChevronRight className="w-3.5 h-3.5 text-slate-400" />
        <span className="text-slate-900 font-semibold">{current}</span>
      </nav>

      {/* Right User Indicator badge */}
      <div className="flex items-center space-x-3">
        {displayName && (
          <span className="hidden sm:block text-xs text-slate-500 font-medium">
            {displayName}
          </span>
        )}
        <div
          className="w-8 h-8 rounded-full bg-[#E0F2FE] text-[#0284C7] font-bold text-xs flex items-center justify-center border border-sky-200 shadow-sm cursor-pointer hover:bg-sky-200/60 transition-colors"
          title={displayName}
        >
          {initials}
        </div>
      </div>
    </header>
  );
};
