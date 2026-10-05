import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Calendar,
  Users,
  MapPin,
  Briefcase,
} from 'lucide-react';
import type { EventDto, EventStatus } from '../../types/event';

interface Props {
  event: EventDto;
}

export const EventListCard: React.FC<Props> = ({ event }) => {
  const navigate = useNavigate();
  const statusStyle = statusBadgeStyle(event.status);
  const totalVolunteers = event.roleRequirements.reduce(
    (sum, r) => sum + r.requiredHeadcount,
    0
  );

  return (
    <div
      onClick={() => navigate(`/events/${event.id}`)}
      className="rounded-2xl border border-slate-200 bg-white p-5 hover:border-brand-300 hover:shadow-md transition-all cursor-pointer"
    >
      {/* Header: title + status */}
      <div className="flex items-start justify-between mb-3">
        <h3 className="font-bold text-slate-800 text-base leading-tight pr-3 flex-1">
          {event.title}
        </h3>
        <span
          className={`px-2.5 py-1 rounded-lg text-[11px] font-bold border ${statusStyle}`}
        >
          {event.status}
        </span>
      </div>

      {/* Category */}
      <div className="flex items-center text-sm text-slate-500 mb-2">
        <Briefcase className="w-3.5 h-3.5 mr-1.5 text-brand-500" />
        {event.category}
      </div>

      {/* Date */}
      <div className="flex items-center text-sm text-slate-500 mb-2">
        <Calendar className="w-3.5 h-3.5 mr-1.5 text-brand-500" />
        {formatDate(event.startDate)}
      </div>

      {/* Venue */}
      <div className="flex items-center text-sm text-slate-500 mb-3">
        <MapPin className="w-3.5 h-3.5 mr-1.5 text-brand-500" />
        {event.venueId ? (
          <span className="font-mono text-[11px] text-slate-400">
            {event.venueId.slice(0, 8)}…
          </span>
        ) : (
          <span className="italic text-slate-400">No venue assigned</span>
        )}
      </div>

      {/* Description preview */}
      {event.description && (
        <p className="text-xs text-slate-500 line-clamp-2 mb-3">
          {event.description}
        </p>
      )}

      {/* Footer: roles summary */}
      <div className="flex items-center justify-between pt-3 border-t border-slate-100">
        <div className="flex items-center text-xs text-slate-500">
          <Users className="w-3.5 h-3.5 mr-1 text-brand-500" />
          {event.roleRequirements.length} role
          {event.roleRequirements.length !== 1 ? 's' : ''} • {totalVolunteers}{' '}
          volunteers
        </div>
      </div>
    </div>
  );
};

// ---- Helpers ----

function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

function statusBadgeStyle(status: EventStatus): string {
  switch (status) {
    case 'Draft':
      return 'bg-slate-100 text-slate-600 border-slate-200';
    case 'Published':
      return 'bg-blue-50 text-blue-700 border-blue-200';
    case 'StaffingInProgress':
      return 'bg-amber-50 text-amber-700 border-amber-200';
    case 'FullyStaffed':
      return 'bg-emerald-50 text-emerald-700 border-emerald-200';
    case 'Completed':
      return 'bg-teal-50 text-teal-700 border-teal-200';
    case 'Cancelled':
      return 'bg-red-50 text-red-700 border-red-200';
    default:
      return 'bg-slate-100 text-slate-600 border-slate-200';
  }
}