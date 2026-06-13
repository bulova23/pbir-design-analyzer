import React from 'react';
import type { AlternateConceptComparison, AlternateReportConcept } from '../../../src/design-studio/contracts/designStudioModels';

interface ConceptComparisonProps {
  alternateConcepts: AlternateReportConcept[];
  comparison?: AlternateConceptComparison;
  preferredBaselineConceptId?: string;
  approvedBaselineConceptId?: string;
  onSelectBaseline(conceptId: string): void;
  onApproveBaseline(conceptId: string): void;
}

export function ConceptComparison({
  alternateConcepts,
  comparison,
  preferredBaselineConceptId,
  approvedBaselineConceptId,
  onSelectBaseline,
  onApproveBaseline,
}: ConceptComparisonProps) {
  if (alternateConcepts.length === 0 || !comparison) {
    return null;
  }

  const preferred = alternateConcepts.find((concept) => concept.id === preferredBaselineConceptId);

  return (
    <section>
      <h2>Concept comparison</h2>
      <p>{comparison.summary}</p>
      {preferred ? (
        <p>Preferred baseline: {preferred.label}</p>
      ) : null}
      <p>Draft Studio approval: {approvedBaselineConceptId ? 'Approved' : 'Not approved'}</p>
      <p>Selected baseline stays internal to Concept Studio until a future explicit materialization step.</p>
      <button
        type='button'
        disabled={!preferredBaselineConceptId}
        onClick={() => {
          if (preferredBaselineConceptId) {
            onApproveBaseline(preferredBaselineConceptId);
          }
        }}
      >
        Approve for Draft Studio
      </button>
      <ul>
        {alternateConcepts.map((concept) => (
          <li key={concept.id}>
            <strong>{concept.label}</strong>
            <div>{concept.summary}</div>
            <button type='button' onClick={() => onSelectBaseline(concept.id)}>
              Choose {concept.label}
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
