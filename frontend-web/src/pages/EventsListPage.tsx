import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, RefreshCw, CalendarX, Loader2, AlertCircle } from 'lucide-react';
import { EventListCard } from '../components/events/EventListCard';
import { eventService } from '../services/eventService';
import type { EventDto } from '../types/event';

export const EventsListPage: React.FC = () => {
  const navigate = useNavigate();

  const [events, setEvents] = useState<EventDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadEvents = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await eventService.getAllEvents();
      setEvents(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load events');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadEvents();
  }, []);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Page header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-extrabold text-slate-900">Events</h1>
          <p className="text-sm text-slate-500 mt-1">
            Manage your events and staffing plans.
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={loadEvents}
            disabled={isLoading}
            className="flex items-center px-3 py-2 rounded-xl bg-white border border-slate-200 text-slate-700 text-sm font-medium hover:bg-slate-50 disabled:opacity-50 transition-colors"
          >
            <RefreshCw
              className={`w-4 h-4 mr-1.5 ${isLoading ? 'animate-spin' : ''}`}
            />
            Refresh
          </button>
          <button
            onClick={() => navigate('/events/new')}
            className="flex items-center px-4 py-2 rounded-xl bg-brand-600 text-white text-sm font-semibold hover:bg-brand-700 transition-colors shadow-sm shadow-brand-500/20"
          >
            <Plus className="w-4 h-4 mr-1.5" />
            Create Event
          </button>
        </div>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20 text-slate-500">
          <Loader2 className="w-6 h-6 animate-spin mr-2" />
          Loading events...
        </div>
      )}

      {/* Error */}
      {!isLoading && error && (
        <div className="rounded-2xl border border-red-200 bg-red-50 p-6 text-center">
          <AlertCircle className="w-10 h-10 mx-auto text-red-500 mb-3" />
          <h3 className="font-bold text-red-800 mb-1">Could not load events</h3>
          <p className="text-sm text-red-600 mb-4">{error}</p>
          <button
            onClick={loadEvents}
            className="px-4 py-2 rounded-xl bg-red-600 text-white text-sm font-semibold hover:bg-red-700 transition-colors"
          >
            Retry
          </button>
        </div>
      )}

      {/* Empty state */}
      {!isLoading && !error && events.length === 0 && (
        <div className="rounded-2xl border-2 border-dashed border-slate-200 bg-white py-20 text-center">
          <CalendarX className="w-12 h-12 mx-auto text-slate-300 mb-4" />
          <h3 className="font-bold text-slate-700 mb-1">No events yet</h3>
          <p className="text-sm text-slate-500 mb-6">
            Create your first event to get started.
          </p>
          <button
            onClick={() => navigate('/events/new')}
            className="inline-flex items-center px-5 py-2.5 rounded-xl bg-brand-600 text-white text-sm font-semibold hover:bg-brand-700 transition-colors shadow-sm shadow-brand-500/20"
          >
            <Plus className="w-4 h-4 mr-1.5" />
            Create your first event
          </button>
        </div>
      )}

      {/* Events grid */}
      {!isLoading && !error && events.length > 0 && (
        <>
          <div className="mb-4 text-xs text-slate-400">
            {events.length} event{events.length !== 1 ? 's' : ''} found
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {events.map((event) => (
              <EventListCard
                key={event.id}
                event={event}
                onView={(e) => {
                  // Navigate to detail — for now, alert with ID (detail page comes later)
                  // TODO: replace with a proper detail route once built
                  alert(`Event: ${e.title}\nStatus: ${e.status}\nRoles: ${e.roleRequirements.length}`);
                }}
              />
            ))}
          </div>
        </>
      )}
    </div>
  );
};