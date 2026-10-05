import React, { useState } from 'react';
import { ArrowLeft, ArrowRight, XCircle } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

import { EventWizardStepper } from '../components/events/EventWizardStepper';
import { StepBasicInfo } from '../components/events/StepBasicInfo';
import { StepVenue } from '../components/events/StepVenue';
import { StepRoleRequirements } from '../components/events/StepRoleRequirements';
import { StepReview } from '../components/events/StepReview';
import { useEventWizardStore } from '../stores/eventWizardStore';
import { eventService } from '../services/eventService';
import { DEMO_ORGANIZER_TOKEN } from '../services/api';
import type { CreateEventDto } from '../types/event';

export const EventWizardPage: React.FC = () => {
  const navigate = useNavigate();

  const {
    title,
    description,
    category,
    startDate,
    endDate,
    venueId,
    roleRequirements,
    currentStep,
    nextStep,
    prevStep,
    goToStep,
    isStepValid,
    reset,
  } = useEventWizardStore();

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const canGoNext = isStepValid(currentStep);
  const isLastStep = currentStep === 3;

  // ---------- Submission ----------
  const handleSubmit = async () => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      // Extract organizer ID from the JWT payload (sub claim).
      const organizerId = extractOrganizerIdFromToken();

      const dto: CreateEventDto = {
        organizerId,
        venueId,
        title: title.trim(),
        description: description.trim() || null,
        category: category.trim(),
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
        roleRequirements: roleRequirements.map((r) => ({
          roleName: r.roleName.trim(),
          description: r.description?.trim() || null,
          requiredHeadcount: r.requiredHeadcount,
          minExperienceLevel: r.minExperienceLevel,
        })),
      };

      const created = await eventService.createEvent(dto);
      reset();
      // Navigate to a success page (event list — to be added).
      // For now: navigate back to home (applicant page) with a query flag.
      navigate(`/?created=${created.id}`);
    } catch (e) {
      setSubmitError(e instanceof Error ? e.message : 'Failed to create event.');
    } finally {
      setIsSubmitting(false);
    }
  };

  // ---------- Render ----------
  return (
    <div className="min-h-screen bg-slate-50 py-8">
      <div className="max-w-5xl mx-auto px-4">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900">
              Create New Event
            </h1>
            <p className="text-sm text-slate-500 mt-1">
              Fill in the details step by step to publish your event.
            </p>
          </div>
          <button
            onClick={() => navigate('/')}
            className="flex items-center text-sm text-slate-500 hover:text-slate-800 transition-colors"
          >
            <XCircle className="w-4 h-4 mr-1" />
            Cancel
          </button>
        </div>

        {/* Stepper */}
        <div className="mb-10">
          <EventWizardStepper currentStep={currentStep} />
        </div>

        {/* Active step */}
        <div className="mb-10">
          {currentStep === 0 && <StepBasicInfo />}
          {currentStep === 1 && <StepVenue />}
          {currentStep === 2 && <StepRoleRequirements />}
          {currentStep === 3 && (
            <StepReview
              onSubmit={handleSubmit}
              isSubmitting={isSubmitting}
              submitError={submitError}
            />
          )}
        </div>

        {/* Navigation */}
        <div className="flex items-center justify-between pt-6 border-t border-slate-200">
          <button
            type="button"
            onClick={prevStep}
            disabled={currentStep === 0}
            className="flex items-center px-4 py-2 rounded-xl bg-white border border-slate-200 text-slate-700 text-sm font-semibold hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            <ArrowLeft className="w-4 h-4 mr-1" />
            Previous
          </button>

          {/* Step indicator */}
          <span className="text-xs text-slate-400">
            Step {currentStep + 1} of 4
          </span>

          {!isLastStep && (
            <button
              type="button"
              onClick={nextStep}
              disabled={!canGoNext}
              className="flex items-center px-4 py-2 rounded-xl bg-brand-600 text-white text-sm font-semibold hover:bg-brand-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors shadow-sm shadow-brand-500/20"
            >
              Next
              <ArrowRight className="w-4 h-4 ml-1" />
            </button>
          )}

          {isLastStep && (
            <div className="w-[92px]" /> // placeholder to balance layout
          )}
        </div>

        {/* Jump back to a previous step */}
        {currentStep > 0 && (
          <div className="mt-4 text-center">
            <button
              type="button"
              onClick={() => goToStep(0)}
              className="text-xs text-slate-400 hover:text-brand-600 underline"
            >
              Start over from Step 1
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

// ---------- Helpers ----------

/**
 * Extracts the "sub" (subject / user ID) claim from the demo JWT.
 * This is temporary — once a real login flow exists, the user ID comes
 * from the auth context, not from decoding the token client-side.
 */
function extractOrganizerIdFromToken(): string {
  try {
    const payload = DEMO_ORGANIZER_TOKEN.split('.')[1];
    const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
    return decoded.sub as string;
  } catch {
    // Fallback GUID — matches the seeded organizer from earlier tests.
    return 'ba9315ea-8714-4f39-a11e-00675f796739';
  }
}