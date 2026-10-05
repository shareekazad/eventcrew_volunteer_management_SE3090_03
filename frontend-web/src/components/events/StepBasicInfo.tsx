import React from 'react';
import { FileText, Calendar, Tag } from 'lucide-react';
import { useEventWizardStore } from '../../stores/eventWizardStore';

export const StepBasicInfo: React.FC = () => {
  const {
    title,
    description,
    category,
    startDate,
    endDate,
    setBasicInfo,
  } = useEventWizardStore();

  const handleChange = (field: string, value: string) => {
    setBasicInfo({
      title: field === 'title' ? value : title,
      description: field === 'description' ? value : description,
      category: field === 'category' ? value : category,
      startDate: field === 'startDate' ? value : startDate,
      endDate: field === 'endDate' ? value : endDate,
    });
  };

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      {/* Title */}
      <div>
        <label className="flex items-center text-sm font-semibold text-slate-700 mb-2">
          <FileText className="w-4 h-4 mr-2 text-brand-600" />
          Event Title <span className="text-red-500 ml-1">*</span>
        </label>
        <input
          type="text"
          value={title}
          onChange={(e) => handleChange('title', e.target.value)}
          placeholder="e.g. Colombo Tech Meetup 2026"
          maxLength={200}
          className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none transition-all"
        />
        <p className="text-xs text-slate-400 mt-1">
          {title.length}/200 characters
        </p>
      </div>

      {/* Category */}
      <div>
        <label className="flex items-center text-sm font-semibold text-slate-700 mb-2">
          <Tag className="w-4 h-4 mr-2 text-brand-600" />
          Category <span className="text-red-500 ml-1">*</span>
        </label>
        <select
          value={category}
          onChange={(e) => handleChange('category', e.target.value)}
          className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none transition-all bg-white"
        >
          <option value="">Select a category...</option>
          <option value="Conference">Conference</option>
          <option value="Workshop">Workshop</option>
          <option value="Meetup">Meetup</option>
          <option value="Concert">Concert</option>
          <option value="Sports">Sports</option>
          <option value="Community">Community</option>
          <option value="Charity">Charity</option>
          <option value="Other">Other</option>
        </select>
      </div>

      {/* Description */}
      <div>
        <label className="flex items-center text-sm font-semibold text-slate-700 mb-2">
          <FileText className="w-4 h-4 mr-2 text-brand-600" />
          Description
        </label>
        <textarea
          value={description}
          onChange={(e) => handleChange('description', e.target.value)}
          placeholder="Describe what the event is about, who should attend, and what volunteers will do."
          maxLength={2000}
          rows={4}
          className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none transition-all resize-none"
        />
        <p className="text-xs text-slate-400 mt-1">
          {description.length}/2000 characters
        </p>
      </div>

      {/* Dates - two columns */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label className="flex items-center text-sm font-semibold text-slate-700 mb-2">
            <Calendar className="w-4 h-4 mr-2 text-brand-600" />
            Start Date & Time <span className="text-red-500 ml-1">*</span>
          </label>
          <input
            type="datetime-local"
            value={startDate}
            onChange={(e) => handleChange('startDate', e.target.value)}
            className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none transition-all"
          />
        </div>
        <div>
          <label className="flex items-center text-sm font-semibold text-slate-700 mb-2">
            <Calendar className="w-4 h-4 mr-2 text-brand-600" />
            End Date & Time <span className="text-red-500 ml-1">*</span>
          </label>
          <input
            type="datetime-local"
            value={endDate}
            onChange={(e) => handleChange('endDate', e.target.value)}
            className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none transition-all"
          />
        </div>
      </div>

      {/* Date validation message */}
      {startDate && endDate && new Date(startDate) >= new Date(endDate) && (
        <div className="rounded-xl bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
          End date must be after start date.
        </div>
      )}
    </div>
  );
};