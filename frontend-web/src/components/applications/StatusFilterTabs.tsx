import React from 'react';
import { useApplicationStore } from '../../stores/applicationStore';
import type { StatusFilter } from '../../types/application';
import { CheckCircle2, Clock, Eye, ListFilter, ThumbsUp, XCircle } from 'lucide-react';

interface TabConfig {
  key: StatusFilter;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  color: string;
  activeColor: string;
}

const TABS: TabConfig[] = [
  { key: 'All', label: 'All Applicants', icon: ListFilter, color: 'text-slate-600', activeColor: 'bg-brand-600 text-white shadow-sm' },
  { key: 'Submitted', label: 'Submitted', icon: Clock, color: 'text-amber-600', activeColor: 'bg-amber-600 text-white shadow-sm' },
  { key: 'UnderReview', label: 'Under Review', icon: Eye, color: 'text-blue-600', activeColor: 'bg-blue-600 text-white shadow-sm' },
  { key: 'Shortlisted', label: 'Shortlisted', icon: ThumbsUp, color: 'text-purple-600', activeColor: 'bg-purple-600 text-white shadow-sm' },
  { key: 'Accepted', label: 'Accepted', icon: CheckCircle2, color: 'text-emerald-600', activeColor: 'bg-emerald-600 text-white shadow-sm' },
  { key: 'Rejected', label: 'Rejected', icon: XCircle, color: 'text-rose-600', activeColor: 'bg-rose-600 text-white shadow-sm' },
];

export const StatusFilterTabs: React.FC = () => {
  const { applicants, statusFilter, setStatusFilter } = useApplicationStore();

  const getCount = (status: StatusFilter) => {
    if (status === 'All') return applicants.length;
    return applicants.filter((a) => a.status === status).length;
  };

  return (
    <div className="flex items-center space-x-1 overflow-x-auto pb-2 scrollbar-none">
      <div className="bg-slate-200/70 p-1.5 rounded-xl flex items-center gap-1 border border-slate-200/80">
        {TABS.map((tab) => {
          const isActive = statusFilter === tab.key;
          const count = getCount(tab.key);
          const Icon = tab.icon;

          return (
            <button
              key={tab.key}
              onClick={() => setStatusFilter(tab.key)}
              className={`flex items-center space-x-2 px-3.5 py-2 rounded-lg text-xs md:text-sm font-semibold transition-all duration-200 whitespace-nowrap ${
                isActive
                  ? tab.activeColor
                  : 'text-slate-600 hover:text-slate-900 hover:bg-white/60'
              }`}
            >
              <Icon className="w-4 h-4" />
              <span>{tab.label}</span>
              <span
                className={`ml-1.5 px-2 py-0.5 rounded-full text-xs font-bold transition-colors ${
                  isActive
                    ? 'bg-white/25 text-white'
                    : 'bg-slate-200 text-slate-700'
                }`}
              >
                {count}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
};
