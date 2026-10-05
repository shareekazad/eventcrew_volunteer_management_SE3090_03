import React, { useEffect, useState } from 'react';
import { MapPin, Users, Building2, Loader2, Plus, X, AlertCircle } from 'lucide-react';
import { useEventWizardStore } from '../../stores/eventWizardStore';
import { eventService, type VenueDto, type CreateVenueDto } from '../../services/eventService';

export const StepVenue: React.FC = () => {
  const { venueId, setVenue } = useEventWizardStore();

  const [venues, setVenues] = useState<VenueDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showCreateForm, setShowCreateForm] = useState(false);

  const loadVenues = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await eventService.getAllVenues();
      setVenues(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load venues');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadVenues();
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
          {/* Venue dropdown + Create button */}
          <div>
            <div className="flex items-end justify-between mb-2">
              <label className="flex items-center text-sm font-semibold text-slate-700">
                <Building2 className="w-4 h-4 mr-2 text-brand-600" />
                Select Venue
              </label>
              <button
                type="button"
                onClick={() => setShowCreateForm(true)}
                className="flex items-center text-xs text-brand-600 hover:text-brand-700 font-semibold"
              >
                <Plus className="w-3 h-3 mr-1" />
                Create new venue
              </button>
            </div>
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

          {venues.length === 0 && !showCreateForm && (
            <div className="rounded-xl bg-slate-50 border border-slate-200 px-4 py-3 text-sm text-slate-600">
              No venues available yet. Click <strong>Create new venue</strong> above or
              proceed without one.
            </div>
          )}
        </>
      )}

      {/* Inline Create Venue Form */}
      {showCreateForm && (
        <CreateVenueForm
          onCancel={() => setShowCreateForm(false)}
          onCreated={(newVenue) => {
            setVenues((prev) => [...prev, newVenue]);
            setVenue(newVenue.id);
            setShowCreateForm(false);
          }}
        />
      )}
    </div>
  );
};

// ============================================================================
// Inline Create Venue Form
// ============================================================================
const CreateVenueForm: React.FC<{
  onCancel: () => void;
  onCreated: (venue: VenueDto) => void;
}> = ({ onCancel, onCreated }) => {
  const [name, setName] = useState('');
  const [address, setAddress] = useState('');
  const [city, setCity] = useState('');
  const [capacity, setCapacity] = useState(100);
  const [latitude, setLatitude] = useState<string>('');
  const [longitude, setLongitude] = useState<string>('');

  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async () => {
    setError(null);

    if (!name.trim()) {
      setError('Venue name is required.');
      return;
    }
    if (!address.trim()) {
      setError('Address is required.');
      return;
    }
    if (!city.trim()) {
      setError('City is required.');
      return;
    }
    if (capacity < 1) {
      setError('Capacity must be greater than 0.');
      return;
    }

    const lat = latitude.trim() ? parseFloat(latitude) : null;
    const lng = longitude.trim() ? parseFloat(longitude) : null;

    if (lat !== null && (isNaN(lat) || lat < -90 || lat > 90)) {
      setError('Latitude must be between -90 and 90.');
      return;
    }
    if (lng !== null && (isNaN(lng) || lng < -180 || lng > 180)) {
      setError('Longitude must be between -180 and 180.');
      return;
    }

    setIsSaving(true);
    try {
      const dto: CreateVenueDto = {
        name: name.trim(),
        address: address.trim(),
        city: city.trim(),
        latitude: lat,
        longitude: lng,
        capacity,
      };
      const created = await eventService.createVenue(dto);
      onCreated(created);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to create venue.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="rounded-2xl border-2 border-brand-200 bg-brand-50/40 p-5 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center">
          <Plus className="w-4 h-4 text-brand-600 mr-2" />
          <span className="font-bold text-slate-800 text-sm">New Venue</span>
        </div>
        <button
          onClick={onCancel}
          className="text-slate-400 hover:text-slate-700 p-1"
          disabled={isSaving}
        >
          <X className="w-4 h-4" />
        </button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div className="sm:col-span-2">
          <label className="block text-xs font-semibold text-slate-600 mb-1">
            Name <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            maxLength={150}
            placeholder="e.g. BMICH"
            className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm bg-white"
          />
        </div>

        <div className="sm:col-span-2">
          <label className="block text-xs font-semibold text-slate-600 mb-1">
            Address <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            placeholder="Street address"
            className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm bg-white"
          />
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-600 mb-1">
            City <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={city}
            onChange={(e) => setCity(e.target.value)}
            maxLength={100}
            placeholder="e.g. Colombo"
            className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm bg-white"
          />
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-600 mb-1">
            Capacity <span className="text-red-500">*</span>
          </label>
          <input
            type="number"
            min={1}
            value={capacity}
            onChange={(e) => setCapacity(parseInt(e.target.value) || 0)}
            className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm bg-white"
          />
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-600 mb-1">
            Latitude (optional)
          </label>
          <input
            type="text"
            value={latitude}
            onChange={(e) => setLatitude(e.target.value)}
            placeholder="e.g. 6.9012"
            className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm bg-white"
          />
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-600 mb-1">
            Longitude (optional)
          </label>
          <input
            type="text"
            value={longitude}
            onChange={(e) => setLongitude(e.target.value)}
            placeholder="e.g. 79.8626"
            className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm bg-white"
          />
        </div>
      </div>

      {error && (
        <div className="rounded-lg bg-red-50 border border-red-200 px-3 py-2 text-xs text-red-700 flex items-start">
          <AlertCircle className="w-3.5 h-3.5 mr-1.5 mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <div className="flex items-center justify-end space-x-2">
        <button
          type="button"
          onClick={onCancel}
          disabled={isSaving}
          className="px-3 py-1.5 rounded-lg text-slate-600 text-sm hover:bg-white disabled:opacity-50 transition-colors"
        >
          Cancel
        </button>
        <button
          type="button"
          onClick={handleSubmit}
          disabled={isSaving}
          className="flex items-center px-4 py-2 rounded-lg bg-brand-600 text-white text-sm font-semibold hover:bg-brand-700 disabled:opacity-50 transition-colors"
        >
          {isSaving ? (
            <>
              <Loader2 className="w-3.5 h-3.5 mr-1.5 animate-spin" />
              Creating...
            </>
          ) : (
            'Create Venue'
          )}
        </button>
      </div>
    </div>
  );
};