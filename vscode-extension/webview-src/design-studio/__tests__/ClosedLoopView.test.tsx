import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import type {
  ClosedLoopIterationComparison,
  DesignIterationRecord,
} from '../../../src/design-studio/contracts/designStudioModels';
import { ClosedLoopView } from '../views/ClosedLoopView';

function makeIteration(id: string, version: number, overrides: Partial<DesignIterationRecord> = {}): DesignIterationRecord {
  return {
    id,
    threadId: 'thread-closed-loop-view',
    kind: 'designIterationRecord',
    version,
    lifecycleState: 'reviewed',
    createdAt: '2026-06-13T12:00:00.000Z',
    updatedAt: '2026-06-13T12:00:00.000Z',
    authorSource: 'system',
    provenance: {
      source: 'system',
      notes: ['Closed loop records remain audit-only.'],
    },
    sourceArtifactVersionIds: [`draft-report:thread-closed-loop-view@v${version}`],
    comparisonSummary: `Iteration ${version} summary`,
    previousIterationId: version === 1 ? undefined : 'iteration:1',
    materializedCandidate: {
      candidateId: `candidate:${version}`,
      sourceLineage: [`draft-report:thread-closed-loop-view@v${version}`],
      targetSurfaceType: 'pbirReport',
      analyzerHandoffReference: 'syntheticPreview',
      materializationMode: 'draftToSurfaceCandidate',
    },
    analyzerResults: [
      {
        analyzerSource: 'issues',
        analyzerRunId: `run-${version}`,
        resultReference: `issues:${version}`,
        scoredAt: '2026-06-13T12:00:00.000Z',
        validationResultStatus: version === 1 ? 'needsReview' : 'validated',
      },
    ],
    refinementProposals: [
      {
        proposalId: `proposal:${version}`,
        approvalState: 'approved',
        suggestedDesignChange: version === 1
          ? 'Clarify the executive question.'
          : 'Clarify the executive question and simplify branching.',
        expectedImpact: 'Reduce cognitive load.',
        linkedFindingIds: ['finding-1'],
      },
    ],
    approvalCheckpoint: {
      designApproval: { approvalKind: 'designApproval', approvalState: 'approved' },
      materializationApproval: { approvalKind: 'materializationApproval', approvalState: 'approved' },
      refinementApproval: { approvalKind: 'refinementApproval', approvalState: 'approved' },
      validationApproval: {
        approvalKind: 'validationApproval',
        approvalState: version === 1 ? 'notSubmitted' : 'approved',
        validationResultStatus: version === 1 ? 'needsReview' : 'validated',
        owner: version === 1 ? undefined : 'analyzerWorkspace',
        analyzerRunId: version === 1 ? undefined : `run-${version}`,
        resultReference: version === 1 ? undefined : `issues:${version}`,
      },
    },
    comparisonSnapshot: {
      concept: {
        summary: version === 1 ? 'Original concept summary' : 'Refined concept summary',
        pageTitles: ['Executive overview'],
        navigationPattern: 'guidedFlow',
      },
      draft: {
        summary: version === 1 ? 'Original draft summary' : 'Refined draft summary',
        pageStructureSummaries: ['Executive overview scaffold'],
        layoutTitles: ['KPI grid'],
        navigationFrameworks: ['guidedFlow'],
      },
      analyzerOutputs: [
        {
          resultReference: `issues:${version}`,
          analyzerRunId: `run-${version}`,
          analyzerSource: 'issues',
          validationResultStatus: version === 1 ? 'needsReview' : 'validated',
        },
      ],
      recommendations: [
        {
          proposalId: `proposal:${version}`,
          suggestedDesignChange: version === 1
            ? 'Clarify the executive question.'
            : 'Clarify the executive question and simplify branching.',
          expectedImpact: 'Reduce cognitive load.',
        },
      ],
      validationStatus: version === 1 ? 'notSubmitted' : 'validated',
    },
    guardrails: {
      autoOptimizationTriggered: false,
      analyzerExecutionTriggered: false,
      reportMutationTriggered: false,
      pbirFilesGenerated: false,
    },
    ...overrides,
  };
}

describe('ClosedLoopView', () => {
  it('shows lineage, approval checkpoints, analyzer linkage, and refinement linkage', () => {
    const onCompare = jest.fn();
    const iterations = [makeIteration('iteration:1', 1), makeIteration('iteration:2', 2)];
    const comparison: ClosedLoopIterationComparison = {
      baseIterationId: 'iteration:1',
      candidateIterationId: 'iteration:2',
      summary: 'Iteration 2 improves the first draft without auto-approval.',
      conceptChanges: ['Concept summary changed from Original concept summary to Refined concept summary.'],
      draftChanges: ['Draft summary changed from Original draft summary to Refined draft summary.'],
      analyzerOutputChanges: ['Analyzer output changed from issues:1 to issues:2.'],
      recommendationChanges: ['Recommendation changed to simplify branching.'],
      validationStatusChanges: ['Validation status changed from notSubmitted to validated.'],
    };

    render(
      <ClosedLoopView
        iterations={iterations}
        comparison={comparison}
        onCompare={onCompare}
      />,
    );

    expect(screen.getByText('Closed Loop')).toBeInTheDocument();
    expect(screen.getByText('Lineage')).toBeInTheDocument();
    expect(screen.getByText('iteration:1')).toBeInTheDocument();
    expect(screen.getByText('iteration:2')).toBeInTheDocument();
    expect(screen.getByText('Materialized candidate: candidate:2')).toBeInTheDocument();
    expect(screen.getByText('Validation approval: approved')).toBeInTheDocument();
    expect(screen.getByText('Analyzer result: issues:2')).toBeInTheDocument();
    expect(screen.getByText('Refinement proposal: proposal:2')).toBeInTheDocument();
    expect(screen.getByText('Iteration 2 improves the first draft without auto-approval.')).toBeInTheDocument();
  });

  it('renders before and after comparison details and requests an explicit compare action', () => {
    const onCompare = jest.fn();
    const iterations = [makeIteration('iteration:1', 1), makeIteration('iteration:2', 2)];
    const comparison: ClosedLoopIterationComparison = {
      baseIterationId: 'iteration:1',
      candidateIterationId: 'iteration:2',
      summary: 'Iteration 2 improves the first draft without auto-approval.',
      conceptChanges: ['Concept summary changed from Original concept summary to Refined concept summary.'],
      draftChanges: ['Draft summary changed from Original draft summary to Refined draft summary.'],
      analyzerOutputChanges: ['Analyzer output changed from issues:1 to issues:2.'],
      recommendationChanges: ['Recommendation changed to simplify branching.'],
      validationStatusChanges: ['Validation status changed from notSubmitted to validated.'],
    };

    render(
      <ClosedLoopView
        iterations={iterations}
        comparison={comparison}
        onCompare={onCompare}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Compare iteration:1 to iteration:2' }));

    expect(onCompare).toHaveBeenCalledWith('iteration:1', 'iteration:2');
    expect(screen.getByText('Concept summary changed from Original concept summary to Refined concept summary.')).toBeInTheDocument();
    expect(screen.getByText('Draft summary changed from Original draft summary to Refined draft summary.')).toBeInTheDocument();
    expect(screen.getByText('Analyzer output changed from issues:1 to issues:2.')).toBeInTheDocument();
    expect(screen.getByText('Recommendation changed to simplify branching.')).toBeInTheDocument();
    expect(screen.getByText('Validation status changed from notSubmitted to validated.')).toBeInTheDocument();
  });
});
