import React from 'react';
import { Plus, Trash2, Users, Briefcase } from 'lucide-react';
import { useEventWizardStore } from '../../stores/eventWizardStore';
import type { ExperienceLevel } from '../../types/event';

const EXPERIENCE_LEVELS: ExperienceLevel[] = [
  'Beginner',
  'Intermediate',
  'Advanced',
  'Expert',
];

export const StepRoleRequirements: React.FC = () => {
  const { roleRequirements, addRole, updateRole, removeRole } =
    useEventWizardStore();

  return (
    <div className="max-w-3xl mx-auto space-y-4">
      <div className="flex items-start justify-between">
        <div>
          <h3 className="text-lg font-bold text-slate-800">Role Requirements</h3>
          <p className="text-sm text-slate-500">
            Define the volunteer roles you need for this event.
          </p>
        </div>
        <button
          type="button"
          onClick={addRole}
          className="flex items-center px-4 py-2 rounded-xl bg-brand-600 text-white text-sm font-semibold hover:bg-brand-700 transition-colors shadow-sm"
        >
          <Plus className="w-4 h-4 mr-1" />
          Add Role
        </button>
      </div>

      {roleRequirements.length === 0 && (
        <div className="rounded-2xl border-2 border-dashed border-slate-200 bg-white py-12 text-center">
          <Briefcase className="w-10 h-10 mx-auto text-slate-300 mb-3" />
          <p className="text-sm text-slate-500 mb-4">
            No roles added yet. You can add roles now or after publishing.
          </p>
          <button
            type="button"
            onClick={addRole}
            className="text-brand-600 hover:text-brand-700 text-sm font-semibold"
          >
            + Add your first role
          </button>
        </div>
      )}

      {roleRequirements.map((role, index) => (
        <div
          key={index}
          className="rounded-2xl border border-slate-200 bg-white p-5 space-y-4"
        >
          {/* Header: index + remove */}
          <div className="flex items-center justify-between">
            <span className="inline-flex items-center px-3 py-1 rounded-lg bg-brand-50 text-brand-700 text-xs font-bold">
              Role #{index + 1}
            </span>
            <button
              type="button"
              onClick={() => removeRole(index)}
              className="text-slate-400 hover:text-red-600 transition-colors p-1"
              title="Remove this role"
            >
              <Trash2 className="w-4 h-4" />
            </button>
          </div>

          {/* Row 1: Role name + Headcount */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="sm:col-span-2">
              <label className="block text-xs font-semibold text-slate-600 mb-1">
                Role Name <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={role.roleName}
                onChange={(e) => updateRole(index, { roleName: e.target.value })}
                placeholder="e.g. Usher, Registration Desk, Photographer"
                maxLength={100}
                className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-600 mb-1">
                <Users className="w-3 h-3 inline mr-1" />
                Volunteers <span className="text-red-500">*</span>
              </label>
              <input
                type="number"
                min={1}
                value={role.requiredHeadcount}
                onChange={(e) =>
                  updateRole(index, {
                    requiredHeadcount: parseInt(e.target.value) || 1,
                  })
                }
                className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm"
              />
            </div>
          </div>

          {/* Row 2: Experience level */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">
              Minimum Experience Level
            </label>
            <select
              value={role.minExperienceLevel}
              onChange={(e) =>
                updateRole(index, {
                  minExperienceLevel: e.target.value as ExperienceLevel,
                })
              }
              className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm bg-white"
            >
              {EXPERIENCE_LEVELS.map((level) => (
                <option key={level} value={level}>
                  {level}
                </option>
              ))}
            </select>
          </div>

          {/* Row 3: Description */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">
              Description (optional)
            </label>
            <textarea
              value={role.description ?? ''}
              onChange={(e) =>
                updateRole(index, { description: e.target.value || null })
              }
              placeholder="Briefly describe what this volunteer will do"
              maxLength={500}
              rows={2}
              className="w-full px-3 py-2 rounded-lg border border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 outline-none text-sm resize-none"
            />
          </div>
        </div>
      ))}

      {roleRequirements.length > 0 && (
        <p className="text-xs text-slate-400 text-center">
          {roleRequirements.length} role{roleRequirements.length !== 1 ? 's' : ''} •{' '}
          {roleRequirements.reduce((sum, r) => sum + (r.requiredHeadcount || 0), 0)}{' '}
          total volunteers needed
        </p>
      )}
    </div>
  );
};