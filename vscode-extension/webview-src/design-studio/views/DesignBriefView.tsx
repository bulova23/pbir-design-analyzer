import React, { useMemo, useState } from 'react';
import type { DesignBriefEditorAction, DesignBriefEditorState } from '../state/designBriefReducer';

interface DesignBriefViewProps {
  state: DesignBriefEditorState;
  dispatch(action: DesignBriefEditorAction): void;
  onSave(): void;
  onSubmitForApproval(): void;
  onApprove(): void;
}

function approvalStatusLabel(approvalState: DesignBriefEditorState['approvalState']): string {
  switch (approvalState) {
    case 'approved':
      return 'Approved';
    case 'pendingApproval':
      return 'Pending approval';
    case 'rejected':
      return 'Rejected';
    default:
      return 'Not submitted';
  }
}

function nextStepGuidance(state: DesignBriefEditorState): string {
  if (state.approvalState === 'approved') {
    return 'Design Brief approved. Continue to Concept Studio.';
  }

  if (state.approvalState === 'pendingApproval') {
    return 'Submitted for approval. Approve the brief to continue.';
  }

  if (state.isValid) {
    return 'Ready for approval.';
  }

  return 'Complete required fields to continue.';
}

function validationStatusLabel(state: DesignBriefEditorState): string {
  if (state.approvalState === 'approved') {
    return 'Approved';
  }

  if (state.approvalState === 'pendingApproval') {
    return 'Pending approval';
  }

  return state.isValid ? 'Ready for approval' : 'Missing required fields';
}

function Field(props: {
  label: string;
  value: string;
  onChange(value: string): void;
  error?: string;
  multiline?: boolean;
  disabled?: boolean;
}) {
  const id = props.label.toLowerCase().replace(/\s+/g, '-');
  const errorId = `${id}-error`;

  return (
    <label htmlFor={id} style={{ display: 'grid', gap: 4 }}>
      <span>{props.label}</span>
      {props.multiline ? (
        <textarea
          id={id}
          value={props.value}
          onChange={(event) => props.onChange(event.target.value)}
          rows={3}
          disabled={props.disabled}
          aria-invalid={props.error ? 'true' : 'false'}
          aria-describedby={props.error ? errorId : undefined}
        />
      ) : (
        <input
          id={id}
          value={props.value}
          onChange={(event) => props.onChange(event.target.value)}
          disabled={props.disabled}
          aria-invalid={props.error ? 'true' : 'false'}
          aria-describedby={props.error ? errorId : undefined}
        />
      )}
      {props.error ? <span id={errorId}>{props.error}</span> : null}
    </label>
  );
}

export function DesignBriefView({
  state,
  dispatch,
  onSave,
  onSubmitForApproval,
  onApprove,
}: DesignBriefViewProps) {
  const [showAdvanced, setShowAdvanced] = useState(false);
  const isApproved = state.approvalState === 'approved';
  const fieldErrors = useMemo(() => {
    const map = new Map<string, string>();
    for (const error of state.validationErrors) {
      if (error.field === 'approvalState') {
        continue;
      }

      map.set(error.field, error.message);
    }

    return map;
  }, [state.validationErrors]);
  const requiredFeedback = state.validationErrors
    .filter((error) => error.field !== 'approvalState')
    .map((error) => error.message);

  const setField = (
    field: keyof Omit<DesignBriefEditorState, 'approvalState' | 'validationErrors' | 'validationMessages' | 'isValid' | 'canGenerateConcepts'>,
    value: string,
  ) => dispatch({ type: 'setField', field, value });

  return (
    <section className='detail-card'>
      <h3>Design Brief authoring</h3>
      <p>Capture the design intent baseline here, then save, validate, submit, and approve it explicitly before Concept Studio continues.</p>

      <section className='detail-card'>
        <h4>Workflow status</h4>
        <p><strong>Approval status:</strong> {approvalStatusLabel(state.approvalState)}</p>
        <p><strong>Validation status:</strong> {validationStatusLabel(state)}</p>
        <p>{nextStepGuidance(state)}</p>
      </section>

      <div style={{ display: 'grid', gap: 12 }}>
        <section>
          <h4>Start with the essentials</h4>
          <p>Capture the audience, business objective, story, dimensions, and navigation path first. Add deeper workflow context only when it improves the brief.</p>
          <div style={{ display: 'grid', gap: 12 }}>
            <Field label='Audience' value={state.audience} onChange={(value) => setField('audience', value)} error={fieldErrors.get('audience')} disabled={isApproved} />
            <Field label='Business Objective' value={state.businessObjective} onChange={(value) => setField('businessObjective', value)} error={fieldErrors.get('businessObjective')} disabled={isApproved} />
            <Field label='Key Decisions' value={state.keyDecisions} onChange={(value) => setField('keyDecisions', value)} error={fieldErrors.get('keyDecisions')} multiline disabled={isApproved} />
            <Field label='Primary KPIs' value={state.primaryKpis} onChange={(value) => setField('primaryKpis', value)} error={fieldErrors.get('primaryKpis')} multiline disabled={isApproved} />
            <Field label='Dimensions' value={state.dimensions} onChange={(value) => setField('dimensions', value)} error={fieldErrors.get('dimensions')} multiline disabled={isApproved} />
            <Field label='Intended Story' value={state.intendedStory} onChange={(value) => setField('intendedStory', value)} error={fieldErrors.get('intendedStory')} multiline disabled={isApproved} />
            <Field label='Success Criteria' value={state.successCriteria} onChange={(value) => setField('successCriteria', value)} error={fieldErrors.get('successCriteria')} multiline disabled={isApproved} />

            <label htmlFor='report-type' style={{ display: 'grid', gap: 4 }}>
              <span>Report Type</span>
              <select
                id='report-type'
                value={state.reportType}
                onChange={(event) => setField('reportType', event.target.value)}
                disabled={isApproved}
              >
                <option value='dashboard'>Dashboard</option>
                <option value='scorecard'>Scorecard</option>
                <option value='narrativeBriefing'>Narrative briefing</option>
                <option value='operationalMonitoring'>Operational monitoring</option>
              </select>
            </label>

            <Field
              label='Navigation Expectations'
              value={state.navigationExpectations}
              onChange={(value) => setField('navigationExpectations', value)}
              error={fieldErrors.get('navigationExpectations')}
              multiline
              disabled={isApproved}
            />
          </div>
        </section>

        <div>
          <button type='button' onClick={() => setShowAdvanced((value) => !value)}>
            {showAdvanced ? 'Hide advanced brief details' : 'Show advanced brief details'}
          </button>
        </div>

        {showAdvanced ? (
          <section>
            <h4>Advanced design context</h4>
            <p>Use advanced details for evidence, cadence, and surface constraints when the consultant needs more precision.</p>
            <div style={{ display: 'grid', gap: 12 }}>
              <Field label='Consumption Context' value={state.consumptionContext} onChange={(value) => setField('consumptionContext', value)} disabled={isApproved} />
              <Field label='Decision Cadence' value={state.decisionCadence} onChange={(value) => setField('decisionCadence', value)} disabled={isApproved} />
              <Field label='Narrative Risks Or Constraints' value={state.narrativeRisksOrConstraints} onChange={(value) => setField('narrativeRisksOrConstraints', value)} multiline disabled={isApproved} />
              <Field label='Required Evidence Domains' value={state.requiredEvidenceDomains} onChange={(value) => setField('requiredEvidenceDomains', value)} multiline disabled={isApproved} />
              <Field label='Target Analyzable Surface Family' value={state.targetAnalyzableSurfaceFamily} onChange={(value) => setField('targetAnalyzableSurfaceFamily', value)} disabled={isApproved} />
            </div>
          </section>
        ) : null}
      </div>

      {requiredFeedback.length > 0 ? (
        <section className='detail-card'>
          <h4>Validation feedback</h4>
          <ul>
            {requiredFeedback.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        </section>
      ) : null}

      <div className='workflow-actions'>
        <button
          type='button'
          disabled={isApproved}
          onClick={() => {
            dispatch({ type: 'validate' });
            onSave();
          }}
        >
          Save Draft
        </button>
        <button
          type='button'
          disabled={!state.isValid || state.approvalState !== 'notSubmitted'}
          onClick={() => {
            dispatch({ type: 'markSubmitted' });
            onSubmitForApproval();
          }}
        >
          Submit For Approval
        </button>
        <button
          type='button'
          disabled={!state.isValid || state.approvalState !== 'pendingApproval'}
          onClick={() => {
            dispatch({ type: 'markApproved' });
            onApprove();
          }}
        >
          Approve Brief
        </button>
      </div>
    </section>
  );
}
