import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Calendar,
  Users,
  Briefcase,
  MapPin,
  Loader2,
  AlertCircle,
  Sparkles,
  Pencil,
  Trash2,
  CheckCircle2,
  XCircle,
  PlayCircle,
} from 'lucide-react';

import { eventService } from '../services/eventService';
import { agentService } from '../services/agentService';
import { AIPlanPanel } from '../components/events/AIPlanPanel';
import { EditEventModal } from '../components/events/EditEventModal';
import { DEMO_ORGANIZER_TOKEN } from '../services/api';
import type { EventDto, EventStatus } from '../types/event';
import type { WorkflowRunDetailDto } from '../types/agent';

export const EventDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [event, setEvent] = useState<EventDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [isStatusChanging, setIsStatusChanging] = useState(false);
  const [statusError, setStatusError] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const [run, setRun] = useState<WorkflowRunDetailDto | null>(null);
  const [isPlanning, setIsPlanning] = useState(false);
  const [planError, setPlanError] = useState<string | null>(null);

  const [isActioning, setIsActioning] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const reviewerId = extractReviewerId();

  // ------------------------------------------------------------------
  // Load event
  // ------------------------------------------------------------------
  useEffect(() => {
    if (!id) return;
    let cancelled = false;

    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await eventService.getEventById(id);
        if (!cancelled) setEvent(data);
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : 'Failed to load event');
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [id]);

  // ------------------------------------------------------------------
  // Actions
  // ------------------------------------------------------------------
  const handlePlanStaffing = async () => {
    if (!id) return;
    setIsPlanning(true);
    setPlanError(null);

    try {
      const status = await agentService.planStaffing(id);
      const details = await agentService.getRun(status.runId);
      setRun(details);
    } catch (e) {
      setPlanError(e instanceof Error ? e.message : 'Planning failed');
    } finally {
      setIsPlanning(false);
    }
  };

  const handleApprove = async () => {
    if (!run) return;
    setIsActioning(true);
    setActionError(null);
    try {
      const updated = await agentService.approveRun(run.runId, reviewerId);
      setRun(updated);
    } catch (e) {
      setActionError(e instanceof Error ? e.message : 'Approval failed');
    } finally {
      setIsActioning(false);
    }
  };

  const handleReject = async (reason: string) => {
    if (!run) return;
    setIsActioning(true);
    setActionError(null);
    try {
      const updated = await agentService.rejectRun(run.runId, reviewerId, reason);
      setRun(updated);
    } catch (e) {
      setActionError(e instanceof Error ? e.message : 'Rejection failed');
    } finally {
      setIsActioning(false);
    }
  };

  const handleStatusChange = async (newStatus: EventStatus) => {
    if (!event) return;
    setIsStatusChanging(true);
    setStatusError(null);
    try {
      const updated = await eventService.updateEventStatus(event.id, newStatus);
      setEvent(updated);
    } catch (e) {
      setStatusError(e instanceof Error ? e.message : 'Status change failed');
    } finally {
      setIsStatusChanging(false);
    }
  };

  const handleDelete = async () => {
    if (!event) return;
    setIsDeleting(true);
    try {
      await eventService.deleteEvent(event.id);
      navigate('/events');
    } catch (e) {
      setIsDeleting(false);
      setStatusError(e instanceof Error ? e.message : 'Delete failed');
    }
  };

  // ------------------------------------------------------------------
  // Render
  // ------------------------------------------------------------------
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-32 text-slate-500">
        <Loader2 className="w-6 h-6 animate-spin mr-2" />
        Loading event...
      </div>
    );
  }

  if (error || !event) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-12 text-center">
        <AlertCircle className="w-12 h-12 mx-auto text-red-500 mb-3" />
        <h2 className="font-bold text-slate-800 text-lg mb-1">Could not load event</h2>
        <p className="text-sm text-slate-500 mb-6">{error ?? 'Event not found'}</p>
        <button
          onClick={() => navigate('/events')}
          className="px-4 py-2 rounded-xl bg-slate-700 text-white text-sm font-semibold hover:bg-slate-800"
        >
          Back to Events
        </button>
      </div>
    );
  }

  const totalVolunteers = event.roleRequirements.reduce(
    (sum, r) => sum + r.requiredHeadcount,
    0
  );

  const allowedTransitions = getAllowedTransitions(event.status);

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Back */}
      <button
        onClick={() => navigate('/events')}
        className="flex items-center text-sm text-slate-500 hover:text-slate-800 transition-colors mb-6"
      >
        <ArrowLeft className="w-4 h-4 mr-1" />
        Back to Events
      </button>

      {/* Header card */}
      <div className="rounded-2xl border border-slate-200 bg-white p-6 mb-6">
        <div className="flex items-start justify-between mb-4">
          <div className="flex-1">
            <h1 className="text-2xl font-extrabold text-slate-900 mb-2">
              {event.title}
            </h1>
            <EventStatusBadge status={event.status} />
          </div>

          {/* Actions */}
          <div className="flex items-center space-x-2 ml-4">
            <button
              onClick={() => setShowEditModal(true)}
              className="flex items-center px-3 py-1.5 rounded-lg bg-slate-100 text-slate-700 text-xs font-semibold hover:bg-slate-200 transition-colors"
            >
              <Pencil className="w-3.5 h-3.5 mr-1" />
              Edit
            </button>
            <button
              onClick={() => setShowDeleteConfirm(true)}
              className="flex items-center px-3 py-1.5 rounded-lg bg-red-50 text-red-700 text-xs font-semibold hover:bg-red-100 transition-colors"
            >
              <Trash2 className="w-3.5 h-3.5 mr-1" />
              Delete
            </button>
          </div>
        </div>

        {event.description && (
          <p className="text-sm text-slate-600 leading-relaxed mb-5">
            {event.description}
          </p>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-4 border-t border-slate-100">
          <InfoItem
            icon={<Briefcase className="w-4 h-4 text-brand-500" />}
            label="Category"
            value={event.category}
          />
          <InfoItem
            icon={<Calendar className="w-4 h-4 text-brand-500" />}
            label="Schedule"
            value={`${formatDate(event.startDate)} → ${formatDate(event.endDate)}`}
          />
          <InfoItem
            icon={<MapPin className="w-4 h-4 text-brand-500" />}
            label="Venue"
            value={event.venueId ? `${event.venueId.slice(0, 8)}…` : 'Not assigned'}
            mono={!!event.venueId}
          />
        </div>

        {/* Status transitions */}
        {allowedTransitions.length > 0 && (
          <div className="mt-5 pt-4 border-t border-slate-100">
            <div className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-2">
              Status Transitions
            </div>
            <div className="flex flex-wrap gap-2">
              {allowedTransitions.map((s) => (
                <button
                  key={s}
                  onClick={() => handleStatusChange(s)}
                  disabled={isStatusChanging}
                  className="flex items-center px-3 py-1.5 rounded-lg bg-white border border-slate-300 text-slate-700 text-xs font-semibold hover:bg-slate-50 disabled:opacity-50 transition-colors"
                >
                  <PlayCircle className="w-3.5 h-3.5 mr-1" />
                  Move to {s}
                </button>
              ))}
            </div>
            {statusError && (
              <div className="mt-3 rounded-lg bg-red-50 border border-red-200 px-3 py-2 text-xs text-red-700">
                {statusError}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Roles card */}
      <div className="rounded-2xl border border-slate-200 bg-white p-6 mb-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="font-bold text-slate-800">Role Requirements</h2>
          <span className="text-xs text-slate-500">
            {event.roleRequirements.length} role{event.roleRequirements.length !== 1 ? 's' : ''} •{' '}
            {totalVolunteers} volunteers
          </span>
        </div>

        {event.roleRequirements.length === 0 ? (
          <p className="text-sm text-slate-400 italic">No roles defined yet.</p>
        ) : (
          <div className="space-y-2">
            {event.roleRequirements.map((role) => (
              <div
                key={role.id}
                className="flex items-start justify-between rounded-xl bg-slate-50 px-4 py-3"
              >
                <div>
                  <div className="font-semibold text-slate-800 text-sm">
                    {role.roleName}
                  </div>
                  {role.description && (
                    <div className="text-xs text-slate-500 mt-0.5">{role.description}</div>
                  )}
                </div>
                <div className="flex items-center text-xs text-slate-600 ml-3 flex-shrink-0">
                  <Users className="w-3 h-3 mr-1" />
                  {role.requiredHeadcount} • {role.minExperienceLevel}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* AI Section */}
      <div className="mb-6">
        {!run && !isPlanning && (
          <div className="rounded-2xl border-2 border-dashed border-brand-200 bg-brand-50/40 p-8 text-center">
            <Sparkles className="w-10 h-10 mx-auto text-brand-500 mb-3" />
            <h3 className="font-bold text-slate-800 mb-1">
              Generate an AI staffing plan
            </h3>
            <p className="text-sm text-slate-500 mb-5">
              The multi-agent workflow will analyze the event and recommend roles,
              candidates, and shifts.
            </p>
            <button
              onClick={handlePlanStaffing}
              className="inline-flex items-center px-5 py-2.5 rounded-xl bg-brand-600 text-white text-sm font-semibold hover:bg-brand-700 transition-colors shadow-sm shadow-brand-500/20"
            >
              <Sparkles className="w-4 h-4 mr-2" />
              Plan Staffing with AI
            </button>
          </div>
        )}

        {isPlanning && (
          <div className="rounded-2xl border border-brand-200 bg-white p-8 text-center">
            <Loader2 className="w-8 h-8 mx-auto text-brand-600 animate-spin mb-3" />
            <div className="font-semibold text-slate-800 mb-1">
              Running the multi-agent workflow...
            </div>
            <div className="text-xs text-slate-500">
              Planning → Matching → Scheduling → Validation
            </div>
          </div>
        )}

        {planError && (
          <div className="rounded-xl bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700 mb-4 flex items-start">
            <AlertCircle className="w-4 h-4 mr-2 mt-0.5 flex-shrink-0" />
            <span>{planError}</span>
          </div>
        )}

        {run && (
          <AIPlanPanel
            run={run}
            reviewerId={reviewerId}
            onApprove={handleApprove}
            onReject={handleReject}
            isActioning={isActioning}
            actionError={actionError}
          />
        )}
      </div>

      {/* Edit Modal */}
      {showEditModal && (
        <EditEventModal
          event={event}
          onClose={() => setShowEditModal(false)}
          onSaved={(updated) => {
            setEvent(updated);
            setShowEditModal(false);
          }}
        />
      )}

      {/* Delete Confirm Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6">
            <div className="flex items-center mb-4">
              <XCircle className="w-6 h-6 text-red-500 mr-3" />
              <h3 className="font-bold text-slate-900 text-lg">Delete this event?</h3>
            </div>
            <p className="text-sm text-slate-600 mb-6">
              This will permanently delete <strong>{event.title}</strong> and all
              its role requirements. This cannot be undone.
            </p>
            <div className="flex items-center justify-end space-x-2">
              <button
                onClick={() => setShowDeleteConfirm(false)}
                disabled={isDeleting}
                className="px-4 py-2 rounded-xl text-slate-700 text-sm font-semibold hover:bg-slate-100 disabled:opacity-50 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleDelete}
                disabled={isDeleting}
                className="flex items-center px-4 py-2 rounded-xl bg-red-600 text-white text-sm font-semibold hover:bg-red-700 disabled:opacity-50 transition-colors shadow-sm shadow-red-500/20"
              >
                {isDeleting ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-1.5 animate-spin" />
                    Deleting...
                  </>
                ) : (
                  <>
                    <CheckCircle2 className="w-4 h-4 mr-1.5" />
                    Yes, delete
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

// ============================================================
// Small components + helpers
// ============================================================

const InfoItem: React.FC<{
  icon: React.ReactNode;
  label: string;
  value: string;
  mono?: boolean;
}> = ({ icon, label, value, mono }) => (
  <div>
    <div className="flex items-center text-xs font-bold text-slate-500 uppercase tracking-wide mb-1">
      {icon}
      <span className="ml-1.5">{label}</span>
    </div>
    <div className={`text-sm text-slate-700 ${mono ? 'font-mono' : ''}`}>{value}</div>
  </div>
);

const EventStatusBadge: React.FC<{ status: EventStatus }> = ({ status }) => {
  const style = (() => {
    switch (status) {
      case 'Draft':
        return 'bg-slate-100 text-slate-600 border-slate-300';
      case 'Published':
        return 'bg-blue-100 text-blue-700 border-blue-300';
      case 'StaffingInProgress':
        return 'bg-amber-100 text-amber-700 border-amber-300';
      case 'FullyStaffed':
        return 'bg-emerald-100 text-emerald-700 border-emerald-300';
      case 'Completed':
        return 'bg-teal-100 text-teal-700 border-teal-300';
      case 'Cancelled':
        return 'bg-red-100 text-red-700 border-red-300';
      default:
        return 'bg-slate-100 text-slate-600 border-slate-300';
    }
  })();

  return (
    <span className={`inline-block px-3 py-1 rounded-lg text-xs font-bold border ${style}`}>
      {status}
    </span>
  );
};

function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

/**
 * Mirrors the backend's AllowedTransitions dictionary.
 * Kept in sync with EventService.cs — must match exactly.
 */
function getAllowedTransitions(current: EventStatus): EventStatus[] {
  switch (current) {
    case 'Draft':
      return ['Published', 'Cancelled'];
    case 'Published':
      return ['StaffingInProgress', 'Cancelled'];
    case 'StaffingInProgress':
      return ['FullyStaffed', 'Published', 'Cancelled'];
    case 'FullyStaffed':
      return ['Completed', 'StaffingInProgress', 'Cancelled'];
    case 'Completed':
    case 'Cancelled':
      return [];
    default:
      return [];
  }
}

function extractReviewerId(): string {
  try {
    const payload = DEMO_ORGANIZER_TOKEN.split('.')[1];
    const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
    return decoded.sub as string;
  } catch {
    return 'a0000000-0000-0000-0000-000000000001';
  }
}