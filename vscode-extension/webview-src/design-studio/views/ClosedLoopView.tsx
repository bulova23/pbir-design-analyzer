import React from 'react';
import type {
  ClosedLoopIterationComparison,
  DesignIterationRecord,
} from '../../../src/design-studio/contracts/designStudioModels';
import { IterationComparison } from '../components/IterationComparison';

interface ClosedLoopViewProps {
  iterations: DesignIterationRecord[];
  comparison?: ClosedLoopIterationComparison;
  onCompare(baseIterationId: string, candidateIterationId: string): void;
}

export function ClosedLoopView({
  iterations,
  comparison,
  onCompare,
}: ClosedLoopViewProps) {
  const latest = iterations.at(-1);
  const base = iterations[0];

  return (
    <section>
      <h1>Closed Loop</h1>
      <p>Draft {'->'} Assess {'->'} Improve {'->'} Re-Assess {'->'} Compare {'->'} Approve remains explicit and audit-only.</p>

      <section>
        <h2>Lineage</h2>
        <ul>
          {iterations.map((iteration) => (
            <li key={iteration.id}>
              <strong>{iteration.id}</strong>
              <div>Previous iteration: {iteration.previousIterationId ?? 'None'}</div>
              <div>Materialized candidate: {iteration.materializedCandidate?.candidateId ?? 'None'}</div>
              <div>Validation approval: {iteration.approvalCheckpoint.validationApproval.approvalState}</div>
              <div>Analyzer result: {iteration.analyzerResults[0]?.resultReference ?? 'None'}</div>
              <div>Refinement proposal: {iteration.refinementProposals[0]?.proposalId ?? 'None'}</div>
            </li>
          ))}
        </ul>
      </section>

      {base && latest && latest.id !== base.id ? (
        <button
          type='button'
          onClick={() => onCompare(base.id, latest.id)}
        >
          Compare {base.id} to {latest.id}
        </button>
      ) : null}

      {comparison ? <IterationComparison comparison={comparison} /> : null}
    </section>
  );
}
