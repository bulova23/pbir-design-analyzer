import React from 'react';
import { ConceptComparison } from '../components/ConceptComparison';
import type { ConceptStudioAction, ConceptStudioState } from '../state/conceptStudioReducer';

interface ConceptStudioViewProps {
  state: ConceptStudioState;
  dispatch(action: ConceptStudioAction): void;
  onGenerateConcepts(): void;
  onSelectBaseline(conceptId: string): void;
  onSubmitBaselineForApproval(): void;
  onApproveBaseline(conceptId: string): void;
}

function approvalStatusLabel(approvalState: ConceptStudioState['approvalState']): string {
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

function nextStepGuidance(state: ConceptStudioState): string {
  if (!state.canGenerateConcepts) {
    return 'Approve the Design Brief before generating concepts.';
  }

  if (state.approvalState === 'approved') {
    return 'Concept baseline approved. Continue to Draft Studio.';
  }

  if (state.approvalState === 'pendingApproval') {
    return 'Approve the concept baseline to unlock Draft Studio.';
  }

  if (!state.alternateConcepts.length) {
    return 'Generate concept options from the approved Design Brief.';
  }

  if (!state.preferredBaselineConceptId) {
    return 'Select a preferred baseline before submitting for approval.';
  }

  return 'Submit the selected baseline for approval.';
}

export function ConceptStudioView({
  state,
  dispatch,
  onGenerateConcepts,
  onSelectBaseline,
  onSubmitBaselineForApproval,
  onApproveBaseline,
}: ConceptStudioViewProps) {
  return (
    <section className='detail-card'>
      <h3>Concept Studio execution</h3>
      {!state.canGenerateConcepts ? (
        <p>Concept generation is blocked until the Design Brief is approved.</p>
      ) : (
        <p>Concept Studio produces internal concept artifacts only. No PBIR assets or analyzable surfaces are created here.</p>
      )}

      <section className='detail-card'>
        <h4>Workflow status</h4>
        <p><strong>Concept approval:</strong> {approvalStatusLabel(state.approvalState)}</p>
        <p>{nextStepGuidance(state)}</p>
      </section>

      <button
        type='button'
        disabled={!state.canGenerateConcepts || state.approvalState === 'approved'}
        onClick={() => {
          dispatch({ type: 'generateConcepts' });
          onGenerateConcepts();
        }}
      >
        Generate Concepts
      </button>

      {state.alternateConcepts.length > 0 ? (
        <>
          <h2>Concept alternatives</h2>
          <ConceptComparison
            alternateConcepts={state.alternateConcepts}
            comparison={state.comparison}
            approvalState={state.approvalState}
            preferredBaselineConceptId={state.preferredBaselineConceptId}
            approvedBaselineConceptId={state.approvedBaselineConceptId}
            onSelectBaseline={(conceptId) => {
              dispatch({ type: 'selectBaseline', conceptId });
              onSelectBaseline(conceptId);
            }}
            onSubmitBaselineForApproval={() => {
              dispatch({ type: 'submitBaselineForApproval' });
              onSubmitBaselineForApproval();
            }}
            onApproveBaseline={(conceptId) => {
              dispatch({ type: 'approveBaseline', conceptId });
              onApproveBaseline(conceptId);
            }}
          />
        </>
      ) : null}
    </section>
  );
}
