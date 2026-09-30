import React, { useState, useEffect } from 'react';
import { useApplicationStore } from '../../stores/applicationStore';
import type { ApplicationStatus } from '../../types/application';
import {
  Calendar,
  CheckCircle2,
  Clock,
  HeartPulse,
  Mail,
  Phone,
  ShieldCheck,
  Star,
  ThumbsUp,
  User,
  X,
  XCircle,
  FileText,
  AlertCircle,
} from 'lucide-react';

export const ApplicantDetailModal: React.FC = () => {
  const { selectedApplicant, setSelectedApplicant, updateStatus, isLoading } =
    useApplicationStore();

  const [reviewNotes, setReviewNotes] = useState('');
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    if (selectedApplicant) {
      setReviewNotes(selectedApplicant.notes || '');
      setActionError(null);
    }
  }, [selectedApplicant]);

  if (!selectedApplicant) return null;

  const volunteer = selectedApplicant.volunteer;
  const name = volunteer?.fullName || `Volunteer #${selectedApplicant.volunteerId.slice(0, 8)}`;
  const status = selectedApplicant.status;

  const handleStatusChange = async (targetStatus: ApplicationStatus) => {
    setActionError(null);
    // Backend transition guard:
    // Submitted -> UnderReview -> Shortlisted -> Accepted | Rejected
    try {
      if (status === 'Submitted' && targetStatus === 'Shortlisted') {
        const ok1 = await updateStatus(selectedApplicant.id, 'UnderReview', reviewNotes);
        if (ok1) {
          await updateStatus(selectedApplicant.id, 'Shortlisted', reviewNotes);
        }
      } else {
        await updateStatus(selectedApplicant.id, targetStatus, reviewNotes);
      }
    } catch (err: any) {
      setActionError(err.message || 'Transition not permitted by server.');
    }
  };

  const getStatusBadge = (st: ApplicationStatus) => {
    const config = {
      Submitted: 'bg-amber-100 text-amber-800 border-amber-300',
      UnderReview: 'bg-blue-100 text-blue-800 border-blue-300',
      Shortlisted: 'bg-purple-100 text-purple-800 border-purple-300',
      Accepted: 'bg-emerald-100 text-emerald-800 border-emerald-300',
      Rejected: 'bg-rose-100 text-rose-800 border-rose-300',
    }[st];

    return (
      <span className={`px-3 py-1 rounded-full text-xs font-bold border ${config}`}>
        {st}
      </span>
    );
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-200">
      <div
        className="bg-white rounded-3xl shadow-2xl border border-slate-200 w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden animate-in zoom-in-95 duration-200"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="p-6 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
          <div className="flex items-center space-x-3.5">
            <div className="w-12 h-12 rounded-2xl bg-brand-500 text-white flex items-center justify-center font-bold text-lg shadow-sm">
              {name.charAt(0)}
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h3 className="text-xl font-bold text-slate-900">{name}</h3>
                {getStatusBadge(status)}
              </div>
              <p className="text-xs text-slate-500 mt-0.5 font-mono">
                App ID: {selectedApplicant.id}
              </p>
            </div>
          </div>
          <button
            onClick={() => setSelectedApplicant(null)}
            className="p-2 text-slate-400 hover:text-slate-600 rounded-xl hover:bg-slate-100 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Modal Body */}
        <div className="p-6 overflow-y-auto space-y-6 text-sm">
          {actionError && (
            <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-rose-700 text-xs flex items-center space-x-2">
              <AlertCircle className="w-4 h-4 shrink-0 text-rose-500" />
              <span>{actionError}</span>
            </div>
          )}

          {/* Key Metrics Grid */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-100">
              <div className="text-xs text-slate-400 font-medium">Rating Score</div>
              <div className="flex items-center mt-1 text-base font-bold text-amber-600">
                <Star className="w-4 h-4 fill-amber-400 text-amber-500 mr-1" />
                {volunteer?.ratingScore ? volunteer.ratingScore.toFixed(2) : '5.00'}
                <span className="text-xs text-slate-400 font-normal ml-1">/ 5.0</span>
              </div>
            </div>

            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-100">
              <div className="text-xs text-slate-400 font-medium">Experience</div>
              <div className="mt-1 text-base font-bold text-slate-800">
                {volunteer?.experienceYears ?? 3} Years
                <div className="text-[11px] text-slate-400 font-normal">
                  {volunteer?.pastEventsCount ?? 5} Past Events
                </div>
              </div>
            </div>

            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-100">
              <div className="text-xs text-slate-400 font-medium">Max Availability</div>
              <div className="mt-1 text-base font-bold text-slate-800">
                {volunteer?.maxHoursPerWeek ?? 20} hrs
                <div className="text-[11px] text-slate-400 font-normal">per week</div>
              </div>
            </div>

            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-100">
              <div className="text-xs text-slate-400 font-medium">Applied Date</div>
              <div className="mt-1 text-sm font-semibold text-slate-800 flex items-center">
                <Calendar className="w-3.5 h-3.5 text-slate-400 mr-1" />
                {new Date(selectedApplicant.appliedAt).toLocaleDateString()}
              </div>
            </div>
          </div>

          {/* Volunteer Bio */}
          <div>
            <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2">
              Volunteer Bio & Statement
            </h4>
            <div className="p-4 bg-slate-50 rounded-2xl border border-slate-100 text-slate-700 leading-relaxed text-sm">
              {volunteer?.bio || 'No bio provided by volunteer.'}
            </div>
          </div>

          {/* Emergency Contact & Communication */}
          <div>
            <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2">
              Contact & Emergency Information
            </h4>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="flex items-center space-x-3 p-3 bg-slate-50 rounded-xl border border-slate-100">
                <HeartPulse className="w-4 h-4 text-rose-500 shrink-0" />
                <div>
                  <div className="text-xs text-slate-400">Emergency Contact</div>
                  <div className="font-semibold text-slate-800 text-xs">
                    {volunteer?.emergencyContact || 'Not Specified'}
                  </div>
                </div>
              </div>

              <div className="flex items-center space-x-3 p-3 bg-slate-50 rounded-xl border border-slate-100">
                <Mail className="w-4 h-4 text-brand-500 shrink-0" />
                <div>
                  <div className="text-xs text-slate-400">Email Address</div>
                  <div className="font-semibold text-slate-800 text-xs truncate">
                    {volunteer?.email || 'volunteer@eventcrew.com'}
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Skills Breakdown */}
          <div>
            <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2">
              Assessed Skills & Competencies
            </h4>
            <div className="flex flex-wrap gap-2">
              {volunteer?.skills && volunteer.skills.length > 0 ? (
                volunteer.skills.map((skill) => (
                  <div
                    key={skill.id}
                    className="flex items-center space-x-2 px-3 py-1.5 bg-slate-100 rounded-xl border border-slate-200 text-xs font-medium text-slate-700"
                  >
                    <span>{skill.name}</span>
                    <span className="text-[10px] px-1.5 py-0.5 bg-white rounded font-bold text-brand-600 border border-slate-200">
                      {skill.proficiencyLevel}
                    </span>
                  </div>
                ))
              ) : (
                <p className="text-xs text-slate-400">No specific skills listed.</p>
              )}
            </div>
          </div>

          {/* Organizer Review Notes */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
                <FileText className="w-3.5 h-3.5" />
                Organizer Review Notes
              </h4>
              <span className="text-[11px] text-slate-400">Saved on status transition</span>
            </div>
            <textarea
              rows={3}
              value={reviewNotes}
              onChange={(e) => setReviewNotes(e.target.value)}
              placeholder="Internal notes regarding interview, qualifications, or special assignments..."
              className="w-full p-3 bg-slate-50 border border-slate-200 rounded-2xl text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:bg-white transition-all text-slate-800"
            />
          </div>
        </div>

        {/* Action Controls Footer */}
        <div className="p-5 border-t border-slate-100 bg-slate-50/60 flex flex-wrap items-center justify-between gap-3">
          <div className="text-xs text-slate-500">
            Allowed state-machine transitions from <b>{status}</b>
          </div>

          <div className="flex items-center space-x-2">
            {/* Advance to Under Review */}
            {status === 'Submitted' && (
              <button
                onClick={() => handleStatusChange('UnderReview')}
                disabled={isLoading}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-sm transition-all disabled:opacity-50"
              >
                Mark Under Review
              </button>
            )}

            {/* Shortlist */}
            {(status === 'Submitted' || status === 'UnderReview') && (
              <button
                onClick={() => handleStatusChange('Shortlisted')}
                disabled={isLoading}
                className="px-4 py-2 bg-purple-600 hover:bg-purple-700 text-white text-xs font-bold rounded-xl shadow-sm flex items-center space-x-1.5 transition-all disabled:opacity-50"
              >
                <ThumbsUp className="w-3.5 h-3.5" />
                <span>Shortlist</span>
              </button>
            )}

            {/* Accept */}
            {status === 'Shortlisted' && (
              <button
                onClick={() => handleStatusChange('Accepted')}
                disabled={isLoading}
                className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-sm flex items-center space-x-1.5 transition-all disabled:opacity-50"
              >
                <CheckCircle2 className="w-3.5 h-3.5" />
                <span>Accept Volunteer</span>
              </button>
            )}

            {/* Reject */}
            {(status === 'UnderReview' || status === 'Shortlisted') && (
              <button
                onClick={() => handleStatusChange('Rejected')}
                disabled={isLoading}
                className="px-4 py-2 bg-rose-50 hover:bg-rose-100 text-rose-700 border border-rose-200 text-xs font-bold rounded-xl flex items-center space-x-1.5 transition-all disabled:opacity-50"
              >
                <XCircle className="w-3.5 h-3.5 text-rose-600" />
                <span>Reject</span>
              </button>
            )}

            {/* Terminal status display */}
            {(status === 'Accepted' || status === 'Rejected') && (
              <span className="text-xs font-semibold text-slate-500">
                Application is in final state: <b>{status}</b>
              </span>
            )}

            <button
              onClick={() => setSelectedApplicant(null)}
              className="px-4 py-2 bg-white hover:bg-slate-100 border border-slate-200 text-slate-700 text-xs font-semibold rounded-xl transition-all"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
