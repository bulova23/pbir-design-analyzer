import React from 'react';
import { ConceptComparison } from '../components/ConceptComparison';
import type { ConceptStudioAction, ConceptStudioState } from '../state/conceptStudioReducer';

interface ConceptStudioViewProps {
  state: ConceptStudioState;
  dispatch(action: ConceptStudioAction): void;
  onGenerateConcepts(): void;
  onSelectBaseline(conceptId: string): void;
  onApproveBaseline(conceptId: string): void;
}

export function ConceptStudioView({
  state,
  dispatch,
  onGenerateConcepts,
  onSelectBaseline,
  onApproveBaseline,
}: ConceptStudioViewProps) {
  return (
    <section>
      <h1>Concept Studio</h1>
      {!state.canGenerateConcepts ? (
        <p>Concept generation is blocked until the Design Brief is approved.</p>
      ) : (
        <p>Concept Studio produces internal concept artifacts only. No PBIR assets or analyzable surfaces are created here.</p>
      )}

      <button
        type='button'
        disabled={!state.canGenerateConcepts}
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
            preferredBaselineConceptId={state.preferredBaselineConceptId}
            approvedBaselineConceptId={state.approvedBaselineConceptId}
            onSelectBaseline={(conceptId) => {
              dispatch({ type: 'selectBaseline', conceptId });
              onSelectBaseline(conceptId);
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
