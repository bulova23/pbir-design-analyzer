import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import type { DesignIterationRecord } from '../../../src/design-studio/contracts/designStudioModels';
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
        analyzerSource: version === 1 ? 'storyAssessment' : 'guidedStoryImprovements',
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
        pageTitles: version === 1 ? ['Executive overview'] : ['Executive overview', 'Benchmark detail'],
        navigationPattern: version === 1 ? 'guidedFlow' : 'hubAndSpoke',
      },
      draft: {
        summary: version === 1 ? 'Original draft summary' : 'Refined draft summary',
        pageStructureSummaries: version === 1
          ? ['Executive overview scaffold']
          : ['Executive overview scaffold', 'Benchmark comparison page'],
        layoutTitles: version === 1 ? ['KPI grid'] : ['KPI grid', 'Benchmark comparison'],
        navigationFrameworks: version === 1 ? ['guidedFlow'] : ['hubAndSpoke'],
      },
      analyzerOutputs: [
        {
          resultReference: `issues:${version}`,
          analyzerRunId: `run-${version}`,
          analyzerSource: version === 1 ? 'storyAssessment' : 'guidedStoryImprovements',
          validationResultStatus: version === 1 ? 'needsReview' : 'validated',
        },
      ],
      recommendations: [
        {
          proposalId: `proposal:${version}`,
          suggestedDesignChange: version === 1
            ? 'Clarify the executive question.'
            : 'Improve report flow with a clearer executive entry point.',
          expectedImpact: 'Improve report flow.',
          approvalState: 'approved',
        },
        {
          proposalId: `proposal:${version}:rejected`,
          suggestedDesignChange: 'Add benchmark recommendation.',
          expectedImpact: 'Strengthen comparison context.',
          approvalState: version === 1 ? 'pendingApproval' : 'rejected',
        },
      ],
      validationStatus: version === 1 ? 'needsReview' : 'validated',
    },
    guardrails: {
      autoOptimizationTriggered: false,
      analyzerExecutionTriggered: false,
      reportMutationTriggered: false,
      pbirFilesGenerated: false,
    },
    workflowCompletion: {
      state: version === 1 ? 'active' : 'completed',
      isEligible: version === 2,
      checklist: [
        { id: 'briefApproved', label: 'Design Brief approved', satisfied: true, required: true },
        { id: 'conceptApproved', label: 'Concept approved', satisfied: true, required: true },
        { id: 'draftApproved', label: 'Draft approved', satisfied: true, required: true },
        { id: 'candidateApproved', label: 'Review candidate approved', satisfied: true, required: true },
        { id: 'reviewCompleted', label: 'Review completed', satisfied: version === 2, required: true },
      ],
      outstandingItems: version === 1 ? ['Review Design must be completed before the iteration can be closed.'] : [],
      approvalsSatisfied: ['designApproval', 'materializationApproval'],
      deferredRecommendationCount: 1,
      unresolvedRecommendationCount: version === 1 ? 2 : 1,
      nextStepGuidance: version === 2
        ? 'Iteration completed. You may reopen if additional refinement is required.'
        : 'Complete required workflow stages before closing this iteration.',
      completedAt: version === 2 ? '2026-06-13T16:00:00.000Z' : undefined,
      completedBy: version === 2 ? 'user' : undefined,
      history: version === 2
        ? [{ action: 'completed', actor: 'user', timestamp: '2026-06-13T16:00:00.000Z' }]
        : [],
    },
    ...overrides,
  };
}

describe('ClosedLoopView', () => {
  it('renders an iteration timeline and leads with what improved, what changed, and what was accepted', () => {
    const iterations = [makeIteration('iteration:1', 1), makeIteration('iteration:2', 2)];

    render(
      <ClosedLoopView iterations={iterations} />,
    );

    expect(screen.getByRole('heading', { name: 'Iteration Timeline' })).toBeInTheDocument();
    expect(screen.getAllByText('Version 1').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Version 2').length).toBeGreaterThan(0);
    expect(screen.getByText('Current result')).toBeInTheDocument();
    expect(screen.getByText('Completed')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Progress Snapshot' })).toBeInTheDocument();
    expect(screen.getByText('Improvement signals')).toBeInTheDocument();
    expect(screen.getByText('Accepted recommendations')).toBeInTheDocument();
    expect(screen.getByText('Change highlights')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'What Improved' })).toBeInTheDocument();
    expect(screen.getByText('Changed navigation structure.')).toBeInTheDocument();
    expect(screen.getByText('Added benchmark comparison page.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'What Was Accepted' })).toBeInTheDocument();
    expect(screen.getByText('Accepted recommendation: Improve report flow with a clearer executive entry point.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'What Changed' })).toBeInTheDocument();
    expect(screen.getByText('Rejected recommendation: Add benchmark recommendation.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Approval Evolution' })).toBeInTheDocument();
    expect(screen.getByText('Validation Approval changed from Not submitted to Approved.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Validation Evolution' })).toBeInTheDocument();
    expect(screen.getAllByText('Validation status changed from Needs review to Validated.').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Completion Summary' })).toBeInTheDocument();
    expect(screen.getByText(/Completed approvals:/)).toBeInTheDocument();
    expect(screen.getByText(/Unresolved recommendations:/)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Analyzer Review' })).toBeInTheDocument();
    expect(screen.getByText('Review completion: Completed')).toBeInTheDocument();
    expect(screen.getByText('Attached result state: Attached')).toBeInTheDocument();
    expect(screen.getAllByText('Analyzer run: run-2').length).toBeGreaterThan(0);
    expect(screen.getByText('Workflow Completion changed from Active to Completed.')).toBeInTheDocument();
  });

  it('lets the user compare a selected before and after iteration without mutating anything', () => {
    const iterations = [makeIteration('iteration:1', 1), makeIteration('iteration:2', 2)];

    render(
      <ClosedLoopView iterations={iterations} />,
    );

    fireEvent.change(screen.getByLabelText('Before iteration'), { target: { value: 'iteration:1' } });
    fireEvent.change(screen.getByLabelText('After iteration'), { target: { value: 'iteration:2' } });

    expect(screen.getByText('This iteration improved the design and validation story.')).toBeInTheDocument();
    expect(screen.getAllByText('Guided Story Improvements review replaced Story Assessment.').length).toBeGreaterThan(0);
    expect(screen.queryByText('issues:2')).not.toBeInTheDocument();
  });
});
