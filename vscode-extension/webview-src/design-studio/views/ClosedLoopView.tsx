import React, { useMemo, useState } from 'react';
import type { DesignIterationRecord } from '../../../src/design-studio/contracts/designStudioModels';
import { buildIterationComparison, buildIterationTimeline } from '../../../src/design-studio/presentation/iterationExperience';

interface ClosedLoopViewProps {
  iterations: DesignIterationRecord[];
}

function workflowCompletionLabel(value: DesignIterationRecord['workflowCompletion']['state']): string {
  switch (value) {
    case 'readyForCompletion':
      return 'Ready For Completion';
    case 'completed':
      return 'Completed';
    case 'reopened':
      return 'Reopened';
    default:
      return 'Active';
  }
}

export function ClosedLoopView({
  iterations,
}: ClosedLoopViewProps) {
  const timeline = useMemo(() => buildIterationTimeline(iterations), [iterations]);
  const [baseIterationId, setBaseIterationId] = useState(iterations[0]?.id ?? '');
  const [candidateIterationId, setCandidateIterationId] = useState(iterations.at(-1)?.id ?? '');
  const baseIteration = iterations.find((iteration) => iteration.id === baseIterationId) ?? iterations[0];
  const candidateIteration = iterations.find((iteration) => iteration.id === candidateIterationId) ?? iterations.at(-1);
  const comparison = baseIteration && candidateIteration
    ? buildIterationComparison(baseIteration, candidateIteration)
    : undefined;
  const acceptedRecommendations = comparison?.recommendationEvolution.filter((item) => item.startsWith('Accepted recommendation:')) ?? [];
  const changedRecommendations = comparison?.recommendationEvolution.filter((item) => !item.startsWith('Accepted recommendation:')) ?? [];
  const analyzerRunIds = candidateIteration?.analyzerResults.map((result) => result.analyzerRunId) ?? [];
  const attachedResultState = analyzerRunIds.length > 0 ? 'Attached' : 'Not attached';
  const reviewCompletionState = analyzerRunIds.length > 0 ? 'Completed' : 'Not recorded';
  const improvementSignalCount = comparison
    ? comparison.changeSummary.length + comparison.validationEvolution.length
    : 0;

  return (
    <section>
      <h3>Iteration Timeline</h3>
      <p>Review how the design evolved, why decisions changed, and which version produced the current result.</p>

      <section>
        <h4>Timeline</h4>
        <ul>
          {timeline.map((entry) => (
            <li key={entry.iterationId}>
              <strong>{entry.versionLabel}</strong>
              {entry.isCurrentResult ? <span> Current result</span> : null}
              <span> {workflowCompletionLabel(iterations.find((iteration) => iteration.id === entry.iterationId)?.workflowCompletion?.state ?? 'active')}</span>
              <div>{entry.stageLabel}</div>
              <div>{entry.timestampLabel}</div>
              <div>{entry.summary}</div>
              <ul>
                {entry.detailItems.map((item) => (
                  <li key={`${entry.iterationId}:${item}`}>{item}</li>
                ))}
              </ul>
            </li>
          ))}
        </ul>
      </section>

      {iterations.length >= 2 ? (
        <section>
          <h4>Compare Iterations</h4>
          <label>
            Before iteration
            <select value={baseIterationId} onChange={(event) => setBaseIterationId(event.target.value)}>
              {iterations.map((iteration) => (
                <option key={`before:${iteration.id}`} value={iteration.id}>
                  Version {iteration.version}
                </option>
              ))}
            </select>
          </label>
          <label>
            After iteration
            <select value={candidateIterationId} onChange={(event) => setCandidateIterationId(event.target.value)}>
              {iterations.map((iteration) => (
                <option key={`after:${iteration.id}`} value={iteration.id}>
                  Version {iteration.version}
                </option>
              ))}
            </select>
          </label>
        </section>
      ) : null}

      {comparison ? (
        <section>
          <h4>Progress Snapshot</h4>
          <div>
            <p>Improvement signals</p>
            <strong>{improvementSignalCount}</strong>
          </div>
          <div>
            <p>Accepted recommendations</p>
            <strong>{acceptedRecommendations.length}</strong>
          </div>
          <div>
            <p>Change highlights</p>
            <strong>{changedRecommendations.length}</strong>
          </div>

          <h4>What Improved</h4>
          <p>{comparison.summary}</p>
          <ul>
            {comparison.changeSummary.map((item) => (
              <li key={`summary:${item}`}>{item}</li>
            ))}
          </ul>

          <h4>What Was Accepted</h4>
          <ul>
            {acceptedRecommendations.map((item) => (
              <li key={`recommendation:${item}`}>{item}</li>
            ))}
          </ul>

          <h4>What Changed</h4>
          <ul>
            {changedRecommendations.map((item) => (
              <li key={`changed:${item}`}>{item}</li>
            ))}
          </ul>

          <h4>Approval Evolution</h4>
          <ul>
            {comparison.approvalEvolution.map((item) => (
              <li key={`approval:${item}`}>{item}</li>
            ))}
          </ul>

          <h4>Completion Summary</h4>
          <p>Status: {workflowCompletionLabel(candidateIteration?.workflowCompletion?.state ?? 'active')}</p>
          <p>Completed date: {candidateIteration?.workflowCompletion?.completedAt ?? 'Not completed'}</p>
          <p>Completed approvals: {candidateIteration?.workflowCompletion?.approvalsSatisfied.length ?? 0}</p>
          <p>Unresolved recommendations: {candidateIteration?.workflowCompletion?.unresolvedRecommendationCount ?? 0}</p>

          <h4>Analyzer Review</h4>
          <p>Review completion: {reviewCompletionState}</p>
          <p>Attached result state: {attachedResultState}</p>
          <ul>
            {analyzerRunIds.map((runId) => (
              <li key={`run:${runId}`}>Analyzer run: {runId}</li>
            ))}
          </ul>

          <h4>Validation Evolution</h4>
          <ul>
            {comparison.validationEvolution.map((item) => (
              <li key={`validation:${item}`}>{item}</li>
            ))}
          </ul>
        </section>
      ) : null}
    </section>
  );
}
