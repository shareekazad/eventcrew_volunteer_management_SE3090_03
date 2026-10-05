import React from 'react';
import {
  FileText,
  Tag,
  Calendar,
  MapPin,
  Users,
  Briefcase,
  AlertCircle,
} from 'lucide-react';
import { useEventWizardStore } from '../../stores/eventWizardStore';

interface Props {
  onSubmit: () => void;
  isSubmitting: boolean;
  submitError: string | null;
}

export const StepReview: React.FC<Props> = ({
  onSubmit,
  isSubmitting,
  submitError,
}) => {
  const { title, description, category, startDate, endDate, venueId, roleRequirements } =
    useEventWizardStore();

  const formatDate = (iso: string) => {
    if (!iso) return '—';
    const d = new Date(iso);
    return d.toLocaleString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const totalVolunteers = roleRequirements.reduce(
    (sum, r) => sum + (r.requiredHeadcount || 0),
    0
  );

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <div>
        <h3 className="text-lg font-bold text-slate-800">Review Your Event</h3>
        <p className="text-sm text-slate-500">
          Please check everything below before submitting.
        </p>
      </div>

      {/* Title */}
      <div className="rounded-2xl border border-slate-200 bg-white p-5">
        <div className="flex items-start">
          <FileText className="w-5 h-5 text-brand-600 mr-3 mt-0.5 flex-shrink-0" />
          <div>
            <div className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">
              Title
            </div>
            <div className="font-semibold text-slate-800">
              {title || <span className="text-red-500">Missing</span>}
            </div>
          </div>
        </div>
      </div>

      {/* Category + Dates */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="rounded-2xl border border-slate-200 bg-white p-5">
          <Tag className="w-5 h-5 text-brand-600 mb-2" />
          <div className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">
            Category
          </div>
          <div className="font-semibold text-slate-800">
            {category || <span className="text-red-500">Missing</span>}
          </div>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-5">
          <Calendar className="w-5 h-5 text-brand-600 mb-2" />
          <div className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">
            Schedule
          </div>
          <div className="text-sm text-slate-700 space-y-0.5">
            <div>
              <span className="text-slate-500">From:</span> {formatDate(startDate)}
            </div>
            <div>
              <span className="text-slate-500">To:</span> {formatDate(endDate)}
            </div>
          </div>
        </div>
      </div>

      {/* Description */}
      {description && (
        <div className="rounded-2xl border border-slate-200 bg-white p-5">
          <div className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-2">
            Description
          </div>
          <p className="text-sm text-slate-700 whitespace-pre-wrap">{description}</p>
        </div>
      )}

      {/* Venue */}
      <div className="rounded-2xl border border-slate-200 bg-white p-5">
        <div className="flex items-start">
          <MapPin className="w-5 h-5 text-brand-600 mr-3 mt-0.5 flex-shrink-0" />
          <div>
            <div className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">
              Venue
            </div>
            <div className="text-sm text-slate-700">
              {venueId ? (
                <span className="font-mono text-xs">{venueId}</span>
              ) : (
                <span className="text-slate-400 italic">No venue assigned</span>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Roles */}
      <div className="rounded-2xl border border-slate-200 bg-white p-5">
        <div className="flex items-center mb-3">
          <Briefcase className="w-5 h-5 text-brand-600 mr-2" />
          <div className="font-semibold text-slate-800">Role Requirements</div>
          <span className="ml-auto text-xs text-slate-500">
            {roleRequirements.length} role{roleRequirements.length !== 1 ? 's' : ''} •{' '}
            {totalVolunteers} volunteers
          </span>
        </div>

        {roleRequirements.length === 0 ? (
          <p className="text-sm text-slate-400 italic">
            No roles added. You can add them after publishing.
          </p>
        ) : (
          <div className="space-y-2">
            {roleRequirements.map((role, index) => (
              <div
                key={index}
                className="flex items-start justify-between rounded-xl bg-slate-50 px-4 py-2.5"
              >
                <div>
                  <div className="font-semibold text-slate-800 text-sm">
                    {role.roleName || (
                      <span className="text-red-500">Unnamed role</span>
                    )}
                  </div>
                  {role.description && (
                    <div className="text-xs text-slate-500 mt-0.5">
                      {role.description}
                    </div>
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

      {/* Submit error */}
      {submitError && (
        <div className="rounded-xl bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700 flex items-start">
          <AlertCircle className="w-4 h-4 mr-2 mt-0.5 flex-shrink-0" />
          <span>{submitError}</span>
        </div>
      )}

      {/* Submit button */}
      <div className="flex justify-end pt-2">
        <button
          type="button"
          onClick={onSubmit}
          disabled={isSubmitting}
          className="px-6 py-3 rounded-xl bg-brand-600 text-white font-semibold hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shadow-md shadow-brand-500/20"
        >
          {isSubmitting ? 'Creating Event...' : 'Create Event'}
        </button>
      </div>
    </div>
  );
};