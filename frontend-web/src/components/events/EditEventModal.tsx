import React, { useEffect, useState } from 'react';
import { X, Loader2, AlertCircle } from 'lucide-react';
import { eventService, type VenueDto, type UpdateEventDto } from '../../services/eventService';
import type { EventDto } from '../../types/event';

interface Props {
  event: EventDto;
  onClose: () => void;
  onSaved: (updated: EventDto) => void;
}

/**
 * Modal for editing a top-level event.
 * Only edits the fields the backend's UpdateEventDto accepts:
 *   venueId, title, description, category, startDate, endDate.
 * Status changes are handled separately via the status transition endpoint.
 */
export const EditEventModal: React.FC<Props> = ({ event, onClose, onSaved }) => {
  const [title, setTitle] = useState(event.title);
  const [description, setDescription] = useState(event.description ?? '');
  const [category, setCategory] = useState(event.category);
  const [startDate, setStartDate] = useState(toDatetimeLocal(event.startDate));
  const [endDate, setEndDate] = useState(toDatetimeLocal(event.endDate));
  const [venueId, setVenueId] = useState<string | null>(event.venueId);

  const [venues, setVenues] = useState<VenueDto[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load venues for the dropdown
  useEffect(() => {
    let cancelled = false;
    eventService
      .getAllVenues()
      .then((data) => {
        if (!cancelled) setVenues(data);
      })
      .catch(() => {
        // Silent — venue is optional
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const handleSave = async () => {
    setError(null);

    // Client-side validation (mirrors backend rules)
    if (title.trim().length === 0) {
      setError('Title is required.');
      return;
    }
    if (category.trim().length === 0) {
      setError('Category is required.');
      return;
    }
    if (!startDate || !endDate) {
      setError('Start and end dates are required.');
      return;
    }
    if (new Date(startDate) >= new Date(endDate)) {
      setError('End date must be after start date.');
      return;
    }

    setIsSaving(true);
    try {
      const dto: UpdateEventDto = {
        venueId,
        title: title.trim(),
        description: description.trim() || null,
        category: category.trim(),
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
      };

      const updated = await eventService.updateEvent(event.id, dto);
      onSaved(updated);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save event.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-200 sticky top-0 bg-white z-10">
          <div>
            <h2 className="text-lg font-extrabold text-slate-900">Edit Event</h2>
            <p className="text-xs text-slate-500 mt-0.5">
              Update event details. Status changes use separate actions.
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-slate-800 transition-colors p-1"
            disabled={isSaving}
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 space-y-4">
          {/* Title */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">
              Title <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              maxLength={200}
              className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm"
            />
          </div>

          {/* Category */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">
              Category <span className="text-red-500">*</span>
            </label>
            <select
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm bg-white"
            >
              <option value="">Select a category...</option>
              {['Conference', 'Workshop', 'Meetup', 'Concert', 'Sports', 'Community', 'Charity', 'Other'].map(
                (c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                )
              )}
            </select>
          </div>

          {/* Description */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">
              Description
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={2000}
              rows={4}
              className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm resize-none"
            />
          </div>

          {/* Dates */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-slate-600 mb-1">
                Start <span className="text-red-500">*</span>
              </label>
              <input
                type="datetime-local"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-600 mb-1">
                End <span className="text-red-500">*</span>
              </label>
              <input
                type="datetime-local"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm"
              />
            </div>
          </div>

          {/* Venue */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">Venue</label>
            <select
              value={venueId ?? ''}
              onChange={(e) => setVenueId(e.target.value || null)}
              className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm bg-white"
            >
              <option value="">No venue</option>
              {venues.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name} — {v.city} (capacity {v.capacity})
                </option>
              ))}
            </select>
          </div>

          {/* Error */}
          {error && (
            <div className="rounded-lg bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700 flex items-start">
              <AlertCircle className="w-4 h-4 mr-2 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end space-x-2 px-6 py-4 border-t border-slate-200 bg-slate-50 sticky bottom-0">
          <button
            onClick={onClose}
            disabled={isSaving}
            className="px-4 py-2 rounded-xl text-slate-700 text-sm font-semibold hover:bg-white disabled:opacity-50 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleSave}
            disabled={isSaving}
            className="flex items-center px-4 py-2 rounded-xl bg-brand-600 text-white text-sm font-semibold hover:bg-brand-700 disabled:opacity-50 transition-colors shadow-sm shadow-brand-500/20"
          >
            {isSaving ? (
              <>
                <Loader2 className="w-4 h-4 mr-1.5 animate-spin" />
                Saving...
              </>
            ) : (
              'Save Changes'
            )}
          </button>
        </div>
      </div>
    </div>
  );
};

// ============================================================================
// Helpers
// ============================================================================

/**
 * Converts an ISO 8601 string into the format expected by
 * <input type="datetime-local" />: "YYYY-MM-DDTHH:MM".
 */
function toDatetimeLocal(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(
    d.getHours()
  )}:${pad(d.getMinutes())}`;
}