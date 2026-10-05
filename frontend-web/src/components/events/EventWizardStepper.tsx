import React from 'react';
import { Check } from 'lucide-react';

interface Step {
  number: number;
  label: string;
}

const STEPS: Step[] = [
  { number: 1, label: 'Basic Info' },
  { number: 2, label: 'Venue' },
  { number: 3, label: 'Roles' },
  { number: 4, label: 'Review' },
];

interface Props {
  currentStep: number; // 0-indexed
}

export const EventWizardStepper: React.FC<Props> = ({ currentStep }) => {
  return (
    <div className="flex items-center justify-between w-full max-w-3xl mx-auto px-4">
      {STEPS.map((step, index) => {
        const isCompleted = index < currentStep;
        const isActive = index === currentStep;
        const isPending = index > currentStep;

        return (
          <React.Fragment key={step.number}>
            {/* Circle + label */}
            <div className="flex flex-col items-center relative z-10">
              <div
                className={[
                  'w-10 h-10 rounded-full flex items-center justify-center font-bold text-sm transition-all duration-200',
                  isCompleted &&
                    'bg-emerald-500 text-white shadow-md shadow-emerald-500/30',
                  isActive &&
                    'bg-brand-600 text-white ring-4 ring-brand-500/20 shadow-md shadow-brand-500/30',
                  isPending && 'bg-slate-200 text-slate-500',
                ]
                  .filter(Boolean)
                  .join(' ')}
              >
                {isCompleted ? <Check className="w-5 h-5" /> : step.number}
              </div>
              <span
                className={[
                  'mt-2 text-xs font-semibold whitespace-nowrap',
                  isActive && 'text-brand-700',
                  isCompleted && 'text-emerald-600',
                  isPending && 'text-slate-400',
                ]
                  .filter(Boolean)
                  .join(' ')}
              >
                {step.label}
              </span>
            </div>

            {/* Connector line */}
            {index < STEPS.length - 1 && (
              <div className="flex-1 h-0.5 mx-2 -mt-6 bg-slate-200 relative">
                <div
                  className={[
                    'absolute inset-y-0 left-0 bg-emerald-500 transition-all duration-300',
                    isCompleted ? 'w-full' : 'w-0',
                  ].join(' ')}
                />
              </div>
            )}
          </React.Fragment>
        );
      })}
    </div>
  );
};