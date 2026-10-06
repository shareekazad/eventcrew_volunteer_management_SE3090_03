import React from 'react';
import { useApplicationStore } from '../../stores/applicationStore';
import type { Application, ApplicationStatus } from '../../types/application';
import {
  Calendar,
  CheckCircle2,
  ChevronRight,
  Eye,
  Star,
  ThumbsUp,
  User,
  XCircle,
} from 'lucide-react';

interface ApplicantTableProps {
  applicants: Application[];
}

export const ApplicantTable: React.FC<ApplicantTableProps> = ({ applicants }) => {
  const { setSelectedApplicant, updateStatus, isLoading } = useApplicationStore();

  const getStatusBadge = (status: ApplicationStatus) => {
    switch (status) {
      case 'Submitted':
        return (
          <span className="inline-flex items-center text-xs font-medium text-slate-700">
            <span className="w-2 h-2 rounded-full bg-amber-500 mr-2 shrink-0"></span>
            Pending Organizer
          </span>
        );
      case 'UnderReview':
        return (
          <span className="inline-flex items-center text-xs font-medium text-slate-700">
            <span className="w-2 h-2 rounded-full bg-blue-500 mr-2 shrink-0"></span>
            Under Review
          </span>
        );
      case 'Shortlisted':
        return (
          <span className="inline-flex items-center text-xs font-medium text-slate-700">
            <span className="w-2 h-2 rounded-full bg-purple-500 mr-2 shrink-0"></span>
            Shortlisted
          </span>
        );
      case 'Accepted':
        return (
          <span className="inline-flex items-center text-xs font-medium text-slate-700">
            <span className="w-2 h-2 rounded-full bg-emerald-500 mr-2 shrink-0"></span>
            Approved
          </span>
        );
      case 'Rejected':
        return (
          <span className="inline-flex items-center text-xs font-medium text-slate-700">
            <span className="w-2 h-2 rounded-full bg-rose-500 mr-2 shrink-0"></span>
            Rejected
          </span>
        );
      default:
        return null;
    }
  };

  const getSkillCategoryClass = (category?: string) => {
    switch (category?.toLowerCase()) {
      case 'medical':
        return 'bg-red-50 text-red-700 border-red-200';
      case 'technical':
        return 'bg-cyan-50 text-cyan-700 border-cyan-200';
      case 'operations':
      case 'logistics':
        return 'bg-amber-50 text-amber-700 border-amber-200';
      case 'hospitality':
        return 'bg-pink-50 text-pink-700 border-pink-200';
      case 'administration':
        return 'bg-indigo-50 text-indigo-700 border-indigo-200';
      default:
        return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  };

  const handleQuickAction = async (
    e: React.MouseEvent,
    applicant: Application,
    targetStatus: ApplicationStatus
  ) => {
    e.stopPropagation(); // prevent modal opening
    // Backend transition guard:
    // Submitted -> UnderReview -> Shortlisted -> Accepted | Rejected
    // If Submitted and user clicks Shortlist, transition through UnderReview first or directly if supported
    if (applicant.status === 'Submitted' && targetStatus === 'Shortlisted') {
      const ok = await updateStatus(applicant.id, 'UnderReview', 'Advancing to review stage');
      if (ok) {
        await updateStatus(applicant.id, 'Shortlisted', 'Candidate meets criteria');
      }
    } else {
      await updateStatus(applicant.id, targetStatus);
    }
  };

  const formatDate = (isoString: string) => {
    try {
      const d = new Date(isoString);
      return d.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
      });
    } catch {
      return isoString;
    }
  };

  return (
    <div className="overflow-x-auto -mx-6 mt-4 border-t border-slate-100">
      <table className="w-full text-left border-collapse text-xs">
        <thead>
          <tr className="border-b border-slate-100 text-slate-400 font-medium">
            <th className="py-3 px-6">Volunteer</th>
            <th className="py-3 px-4">Applied Date</th>
            <th className="py-3 px-4">Skills &amp; Rating</th>
            <th className="py-3 px-4">Status</th>
            <th className="py-3 px-6 text-right">Actions</th>
          </tr>
        </thead>
          <tbody className="divide-y divide-slate-100 text-sm">
            {applicants.map((app) => {
              const volunteer = app.volunteer;
              const name = volunteer?.fullName || `Volunteer #${app.volunteerId.slice(0, 8)}`;
              const email = volunteer?.email;
              const rating = volunteer?.ratingScore || 5.0;

              return (
                <tr
                  key={app.id}
                  onClick={() => setSelectedApplicant(app)}
                  className="hover:bg-slate-50/80 transition-colors cursor-pointer group"
                >
                  {/* Volunteer Name & Avatar */}
                  <td className="py-4 px-5">
                    <div className="flex items-center space-x-3">
                      <div className="w-10 h-10 rounded-full bg-brand-50 border border-brand-200 flex items-center justify-center text-brand-700 font-bold text-sm shrink-0">
                        {name.charAt(0)}
                      </div>
                      <div>
                        <div className="font-semibold text-slate-900 group-hover:text-brand-600 transition-colors flex items-center gap-1.5">
                          {name}
                          <ChevronRight className="w-3.5 h-3.5 opacity-0 group-hover:opacity-100 transition-opacity text-brand-600" />
                        </div>
                        {email && <div className="text-xs text-slate-400">{email}</div>}
                      </div>
                    </div>
                  </td>

                  {/* Applied Date */}
                  <td className="py-4 px-4 whitespace-nowrap text-slate-600">
                    <div className="flex items-center text-xs text-slate-500">
                      <Calendar className="w-3.5 h-3.5 mr-1.5 text-slate-400" />
                      {formatDate(app.appliedAt)}
                    </div>
                  </td>

                  {/* Skills tags & Rating */}
                  <td className="py-4 px-4">
                    <div className="flex flex-col gap-1.5">
                      <div className="flex items-center gap-1 text-xs font-semibold text-amber-700">
                        <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-500" />
                        <span>{rating.toFixed(1)}</span>
                        <span className="text-slate-400 font-normal">
                          ({volunteer?.pastEventsCount ?? 0} events)
                        </span>
                      </div>
                      <div className="flex flex-wrap gap-1 max-w-xs">
                        {volunteer?.skills && volunteer.skills.length > 0 ? (
                          volunteer.skills.slice(0, 3).map((sk) => (
                            <span
                              key={sk.id}
                              className={`text-[11px] px-2 py-0.5 rounded-md border font-medium ${getSkillCategoryClass(
                                sk.category
                              )}`}
                            >
                              {sk.name}
                            </span>
                          ))
                        ) : (
                          <span className="text-xs text-slate-400">General Crew</span>
                        )}
                        {volunteer?.skills && volunteer.skills.length > 3 && (
                          <span className="text-[10px] text-slate-400 self-center">
                            +{volunteer.skills.length - 3} more
                          </span>
                        )}
                      </div>
                    </div>
                  </td>

                  {/* Status Badge */}
                  <td className="py-4 px-4 whitespace-nowrap">
                    {getStatusBadge(app.status)}
                  </td>

                  {/* Action Buttons: Shortlist, Accept, Reject */}
                  <td className="py-4 px-5 text-right whitespace-nowrap">
                    <div className="flex items-center justify-end space-x-1.5">
                      {/* Under Review Button (if Submitted) */}
                      {app.status === 'Submitted' && (
                        <button
                          onClick={(e) => handleQuickAction(e, app, 'UnderReview')}
                          disabled={isLoading}
                          className="px-2.5 py-1.5 text-xs font-semibold text-blue-700 bg-blue-50 hover:bg-blue-100 rounded-lg border border-blue-200 transition-colors"
                          title="Move to Under Review"
                        >
                          Review
                        </button>
                      )}

                      {/* Shortlist Button (if Submitted or UnderReview) */}
                      {(app.status === 'Submitted' || app.status === 'UnderReview') && (
                        <button
                          onClick={(e) => handleQuickAction(e, app, 'Shortlisted')}
                          disabled={isLoading}
                          className="px-2.5 py-1.5 text-xs font-semibold text-purple-700 bg-purple-50 hover:bg-purple-100 rounded-lg border border-purple-200 transition-colors flex items-center space-x-1"
                          title="Shortlist applicant"
                        >
                          <ThumbsUp className="w-3 h-3" />
                          <span>Shortlist</span>
                        </button>
                      )}

                      {/* Accept Button (if Shortlisted) */}
                      {app.status === 'Shortlisted' && (
                        <button
                          onClick={(e) => handleQuickAction(e, app, 'Accepted')}
                          disabled={isLoading}
                          className="px-2.5 py-1.5 text-xs font-semibold text-emerald-700 bg-emerald-50 hover:bg-emerald-100 rounded-lg border border-emerald-200 transition-colors flex items-center space-x-1"
                          title="Accept applicant"
                        >
                          <CheckCircle2 className="w-3 h-3" />
                          <span>Accept</span>
                        </button>
                      )}

                      {/* Reject Button (if UnderReview or Shortlisted) */}
                      {(app.status === 'UnderReview' || app.status === 'Shortlisted') && (
                        <button
                          onClick={(e) => handleQuickAction(e, app, 'Rejected')}
                          disabled={isLoading}
                          className="px-2.5 py-1.5 text-xs font-semibold text-rose-700 bg-rose-50 hover:bg-rose-100 rounded-lg border border-rose-200 transition-colors flex items-center space-x-1"
                          title="Reject applicant"
                        >
                          <XCircle className="w-3 h-3" />
                          <span>Reject</span>
                        </button>
                      )}

                      {/* Terminal state indicators */}
                      {(app.status === 'Accepted' || app.status === 'Rejected') && (
                        <span className="text-xs text-slate-400 italic">No further actions</span>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    );
};
