import React, { useEffect, useState } from 'react';
import { MapPin, Users, Building2, Loader2 } from 'lucide-react';
import { useEventWizardStore } from '../../stores/eventWizardStore';
import { eventService, type VenueDto } from '../../services/eventService';

export const StepVenue: React.FC = () => {
  const { venueId, setVenue } = useEventWizardStore();

  const [venues, setVenues] = useState<VenueDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await eventService.getAllVenues();
        if (!cancelled) setVenues(data);
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : 'Failed to load venues');
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    load();
    return () => {
      cancelled = true;
    };
  }, []);

  const selected = venues.find((v) => v.id === venueId) ?? null;

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <div className="rounded-xl bg-blue-50 border border-blue-200 px-4 py-3 text-sm text-blue-800">
        <strong>Optional:</strong> You can publish the event without a venue and add it later.
      </div>

      {isLoading && (
        <div className="flex items-center justify-center py-12 text-slate-500">
          <Loader2 className="w-5 h-5 animate-spin mr-2" />
          Loading venues...
        </div>
      )}

      {error && (
        <div className="rounded-xl bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {!isLoading && !error && (
        <>
          {/* Venue dropdown */}
          <div>
            <label className="flex items-center text-sm font-semibold text-slate-700 mb-2">
              <Building2 className="w-4 h-4 mr-2 text-brand-600" />
              Select Venue
            </label>
            <select
              value={venueId ?? ''}
              onChange={(e) => setVenue(e.target.value || null)}
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none transition-all bg-white"
            >
              <option value="">No venue (add later)</option>
              {venues.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name} — {v.city} (capacity {v.capacity})
                </option>
              ))}
            </select>
          </div>

          {/* Selected venue details */}
          {selected && (
            <div className="rounded-2xl border border-slate-200 bg-white p-5 space-y-3">
              <div className="flex items-start">
                <MapPin className="w-5 h-5 text-brand-600 mr-3 mt-0.5 flex-shrink-0" />
                <div>
                  <div className="font-bold text-slate-800">{selected.name}</div>
                  <div className="text-sm text-slate-500">
                    {selected.address}, {selected.city}
                  </div>
                </div>
              </div>

              <div className="flex items-center text-sm text-slate-600 pt-2 border-t border-slate-100">
                <Users className="w-4 h-4 text-brand-600 mr-2" />
                <span>
                  Capacity: <strong>{selected.capacity}</strong> attendees
                </span>
              </div>
            </div>
          )}

          {venues.length === 0 && (
            <div className="rounded-xl bg-slate-50 border border-slate-200 px-4 py-3 text-sm text-slate-600">
              No venues available. You can still proceed without a venue.
            </div>
          )}
        </>
      )}
    </div>
  );
};