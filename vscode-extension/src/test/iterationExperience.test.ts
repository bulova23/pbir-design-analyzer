import type {
  ClosedLoopIterationComparison,
  DesignIterationRecord,
} from '../design-studio/contracts/designStudioModels';
import {
  buildIterationComparison,
  buildIterationTimeline,
} from '../design-studio/presentation/iterationExperience';

function makeIteration(id: string, version: number, overrides: Partial<DesignIterationRecord> = {}): DesignIterationRecord {
  return {
    id,
    threadId: 'thread-iteration-experience',
    kind: 'designIterationRecord',
    version,
    lifecycleState: 'reviewed',
    createdAt: version === 1 ? '2026-06-13T12:00:00.000Z' : '2026-06-13T15:30:00.000Z',
    updatedAt: version === 1 ? '2026-06-13T12:00:00.000Z' : '2026-06-13T15:30:00.000Z',
    authorSource: 'system',
    provenance: {
      source: 'system',
      notes: ['Closed loop records remain audit-only.'],
    },
    previousIterationId: version === 1 ? undefined : 'iteration:1',
    sourceArtifactVersionIds: [`draft-report:thread-iteration-experience@v${version}`],
    materializedCandidate: {
      candidateId: `candidate:${version}`,
      sourceLineage: [`draft-report:thread-iteration-experience@v${version}`],
      targetSurfaceType: 'pbirReport',
      analyzerHandoffReference: 'syntheticPreview',
      materializationMode: 'draftToSurfaceCandidate',
    },
    analyzerResults: [
      {
        analyzerSource: version === 1 ? 'storyAssessment' : 'guidedStoryImprovements',
        analyzerRunId: `run-${version}`,
        resultReference: `issues:${version}`,
        scoredAt: version === 1 ? '2026-06-13T12:00:00.000Z' : '2026-06-13T15:30:00.000Z',
        validationResultStatus: version === 1 ? 'needsReview' : 'validated',
      },
    ],
    refinementProposals: [
      {
        proposalId: `proposal:${version}:accepted`,
        approvalState: 'approved',
        suggestedDesignChange: version === 1
          ? 'Clarify the executive question.'
          : 'Improve report flow with a clearer executive entry point.',
        expectedImpact: 'Improve report flow.',
        linkedFindingIds: ['finding-1'],
      },
      {
        proposalId: `proposal:${version}:rejected`,
        approvalState: version === 1 ? 'pendingApproval' : 'rejected',
        suggestedDesignChange: 'Add benchmark recommendation.',
        expectedImpact: 'Strengthen comparison context.',
        linkedFindingIds: ['finding-2'],
      },
      {
        proposalId: `proposal:${version}:deferred`,
        approvalState: version === 1 ? 'pendingApproval' : 'pendingApproval',
        suggestedDesignChange: 'Add KPI hierarchy.',
        expectedImpact: 'Make drill paths easier to follow.',
        linkedFindingIds: ['finding-3'],
      },
    ],
    approvalCheckpoint: {
      designApproval: { approvalKind: 'designApproval', approvalState: 'approved' },
      materializationApproval: { approvalKind: 'materializationApproval', approvalState: 'approved' },
      refinementApproval: { approvalKind: 'refinementApproval', approvalState: version === 1 ? 'pendingApproval' : 'rejected' },
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
          proposalId: `proposal:${version}:accepted`,
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
        {
          proposalId: `proposal:${version}:deferred`,
          suggestedDesignChange: 'Add KPI hierarchy.',
          expectedImpact: 'Make drill paths easier to follow.',
          approvalState: 'pendingApproval',
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
    comparisonSummary: version === 1
      ? 'Started with an executive overview draft.'
      : 'Improved report flow and added benchmark context.',
    ...overrides,
  };
}

describe('iterationExperience', () => {
  it('builds a readable timeline that explains how the current result was produced', () => {
    const entries = buildIterationTimeline([
      makeIteration('iteration:1', 1),
      makeIteration('iteration:2', 2),
    ]);

    expect(entries).toEqual([
      expect.objectContaining({
        iterationId: 'iteration:1',
        versionLabel: 'Version 1',
        stageLabel: 'Refinement review',
        summary: 'Started with an executive overview draft.',
      }),
      expect.objectContaining({
        iterationId: 'iteration:2',
        versionLabel: 'Version 2',
        stageLabel: 'Validation checkpoint',
        summary: 'Improved report flow and added benchmark context.',
        isCurrentResult: true,
      }),
    ]);
    expect(entries[1]?.detailItems).toEqual(expect.arrayContaining([
      'Concept ready',
      'Draft ready',
      'Materialized candidate prepared',
      'Analyzer review recorded',
      'Approval checkpoint recorded',
    ]));
  });

  it('builds human-readable change, recommendation, approval, and validation evolution without leaking raw IDs', () => {
    const comparison = buildIterationComparison(
      makeIteration('iteration:1', 1),
      makeIteration('iteration:2', 2),
    );

    expect(comparison).toEqual(expect.objectContaining<Partial<ClosedLoopIterationComparison>>({
      baseIterationId: 'iteration:1',
      candidateIterationId: 'iteration:2',
    }));
    expect(comparison.summary).toContain('improved');
    expect(comparison.changeSummary).toEqual(expect.arrayContaining([
      'Changed navigation structure.',
      'Added benchmark comparison page.',
      'Added benchmark comparison.',
    ]));
    expect(comparison.recommendationEvolution).toEqual(expect.arrayContaining([
      'Accepted recommendation: Improve report flow with a clearer executive entry point.',
      'Rejected recommendation: Add benchmark recommendation.',
      'Deferred recommendation: Add KPI hierarchy.',
    ]));
    expect(comparison.approvalEvolution).toEqual(expect.arrayContaining([
      'Refinement Approval changed from Pending approval to Rejected.',
      'Validation Approval changed from Not submitted to Approved.',
    ]));
    expect(comparison.validationEvolution).toEqual(expect.arrayContaining([
      'Validation status changed from Needs review to Validated.',
      'Guided Story Improvements review replaced Story Assessment.',
    ]));
    expect(JSON.stringify(comparison)).not.toContain('issues:2');
    expect(JSON.stringify(comparison)).not.toContain('run-2');
  });
});
