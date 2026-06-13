import React from 'react';
import type { DesignBriefEditorAction, DesignBriefEditorState } from '../state/designBriefReducer';

interface DesignBriefViewProps {
  state: DesignBriefEditorState;
  dispatch(action: DesignBriefEditorAction): void;
  onSave(): void;
  onApprove(): void;
  onGenerateConcepts(): void;
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

function Field(props: {
  label: string;
  value: string;
  onChange(value: string): void;
  multiline?: boolean;
}) {
  const id = props.label.toLowerCase().replace(/\s+/g, '-');

  return (
    <label htmlFor={id} style={{ display: 'grid', gap: 4 }}>
      <span>{props.label}</span>
      {props.multiline ? (
        <textarea
          id={id}
          value={props.value}
          onChange={(event) => props.onChange(event.target.value)}
          rows={3}
        />
      ) : (
        <input
          id={id}
          value={props.value}
          onChange={(event) => props.onChange(event.target.value)}
        />
      )}
    </label>
  );
}

export function DesignBriefView({
  state,
  dispatch,
  onSave,
  onApprove,
  onGenerateConcepts,
}: DesignBriefViewProps) {
  return (
    <section>
      <h1>Design Brief</h1>
      <p>Approval status: {approvalStatusLabel(state.approvalState)}</p>

      <div style={{ display: 'grid', gap: 12 }}>
        <Field label='Audience' value={state.audience} onChange={(value) => dispatch({ type: 'setField', field: 'audience', value })} />
        <Field label='Business Objective' value={state.businessObjective} onChange={(value) => dispatch({ type: 'setField', field: 'businessObjective', value })} />
        <Field label='Key Decisions' value={state.keyDecisions} onChange={(value) => dispatch({ type: 'setField', field: 'keyDecisions', value })} multiline />
        <Field label='Primary KPIs' value={state.primaryKpis} onChange={(value) => dispatch({ type: 'setField', field: 'primaryKpis', value })} multiline />
        <Field label='Dimensions' value={state.dimensions} onChange={(value) => dispatch({ type: 'setField', field: 'dimensions', value })} multiline />
        <Field label='Intended Story' value={state.intendedStory} onChange={(value) => dispatch({ type: 'setField', field: 'intendedStory', value })} multiline />
        <Field label='Success Criteria' value={state.successCriteria} onChange={(value) => dispatch({ type: 'setField', field: 'successCriteria', value })} multiline />

        <label htmlFor='report-type' style={{ display: 'grid', gap: 4 }}>
          <span>Report Type</span>
          <select
            id='report-type'
            value={state.reportType}
            onChange={(event) => dispatch({ type: 'setField', field: 'reportType', value: event.target.value })}
          >
            <option value='dashboard'>Dashboard</option>
            <option value='scorecard'>Scorecard</option>
            <option value='narrativeBriefing'>Narrative briefing</option>
            <option value='operationalMonitoring'>Operational monitoring</option>
          </select>
        </label>

        <Field label='Navigation Expectations' value={state.navigationExpectations} onChange={(value) => dispatch({ type: 'setField', field: 'navigationExpectations', value })} multiline />
        <Field label='Consumption Context' value={state.consumptionContext} onChange={(value) => dispatch({ type: 'setField', field: 'consumptionContext', value })} />
        <Field label='Decision Cadence' value={state.decisionCadence} onChange={(value) => dispatch({ type: 'setField', field: 'decisionCadence', value })} />
        <Field label='Narrative Risks Or Constraints' value={state.narrativeRisksOrConstraints} onChange={(value) => dispatch({ type: 'setField', field: 'narrativeRisksOrConstraints', value })} multiline />
        <Field label='Required Evidence Domains' value={state.requiredEvidenceDomains} onChange={(value) => dispatch({ type: 'setField', field: 'requiredEvidenceDomains', value })} multiline />
        <Field label='Target Analyzable Surface Family' value={state.targetAnalyzableSurfaceFamily} onChange={(value) => dispatch({ type: 'setField', field: 'targetAnalyzableSurfaceFamily', value })} />
      </div>

      {state.validationMessages.length > 0 ? (
        <ul>
          {state.validationMessages.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      ) : null}

      <div style={{ display: 'flex', gap: 8 }}>
        <button
          type='button'
          onClick={() => {
            dispatch({ type: 'validate' });
            onSave();
          }}
        >
          Save Brief
        </button>
        <button
          type='button'
          onClick={() => {
            dispatch({ type: 'markApprovalRequested' });
            onApprove();
          }}
        >
          Request Approval
        </button>
        <button
          type='button'
          disabled={!state.canGenerateConcepts}
          onClick={onGenerateConcepts}
        >
          Generate Concepts
        </button>
      </div>
    </section>
  );
}
