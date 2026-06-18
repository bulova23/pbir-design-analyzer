import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import type {
  DesignStudioAnalyzerResultReference,
  MaterializedSurfaceCandidate,
  RefinementProposal,
} from '../design-studio/contracts/designStudioModels';
import { buildValidationApprovalEvidence } from '../design-studio/contracts/designStudioModels';
import {
  createApprovedDraftMaterializationRequest,
  materializeDesignStudioRequest,
} from '../design-studio/materialization/materializationCoordinator';
import {
  compareIterations,
  attachAnalyzerResultsAtomically,
  attachAvailableAnalyzerResults,
  completeIteration,
  evaluateIterationCompletion,
  loadIterationState,
  recordIteration,
  reopenIteration,
} from '../design-studio/state/iterationStore';
import {
  approveConceptBaseline,
  generateConceptArtifacts,
  loadConceptState,
  selectConceptBaseline,
  submitConceptBaselineForApproval,
} from '../design-studio/state/conceptStore';
import {
  approveDesignBrief,
  saveDesignBriefDraft,
  submitDesignBriefForApproval,
} from '../design-studio/state/designBriefStore';
import {
  approveDraftArtifacts,
  generateDraftArtifacts,
  submitDraftForApproval,
  type DraftState,
} from '../design-studio/state/draftStore';
import {
  approveReviewCandidate,
  createReviewCandidate,
  submitReviewCandidateForApproval,
} from '../design-studio/state/prepareForReviewStore';
import {
  attachAnalyzerResultLineage,
  approveRefinementProposal,
  deferRefinementProposal,
  ingestIssues,
  loadRefinementState,
  rejectRefinementProposal,
} from '../design-studio/state/refinementStore';
import {
  loadReviewDesignState,
  markAnalyzerResultsAttached,
  markReviewCompleted,
  recordAnalyzerResultsAvailable,
  recordReviewLaunch,
} from '../design-studio/state/reviewDesignStore';

function makeContext(tmpDir: string): ExtensionContext {
  return {
    globalStorageUri: { fsPath: tmpDir },
    secrets: {
      get: jest.fn(),
      store: jest.fn(),
      delete: jest.fn(),
    },
  } as unknown as ExtensionContext;
}

function makeTempDir(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-iteration-store-test-'));
}

function threadStorageDir(rootPath: string, threadId: string): string {
  const key = crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
  return path.join(rootPath, 'design-studio', 'threads', key);
}

function reviewDesignManifestPath(rootPath: string, threadId: string): string {
  return path.join(threadStorageDir(rootPath, threadId), 'review-design.json');
}

function prepareForReviewManifestPath(rootPath: string, threadId: string): string {
  return path.join(threadStorageDir(rootPath, threadId), 'prepare-for-review.json');
}

function makeAnalyzerResultReference(
  overrides: Partial<DesignStudioAnalyzerResultReference> & Pick<DesignStudioAnalyzerResultReference, 'analyzerSource' | 'analyzerRunId' | 'resultReference' | 'scoredAt' | 'sourceCandidateId' | 'sourceArtifactVersionFingerprint' | 'validationResultStatus' | 'validationApprovalState'>,
): DesignStudioAnalyzerResultReference {
  return {
    analyzerResultId: overrides.analyzerResultId ?? `analyzer-result:${overrides.analyzerRunId}`,
    analyzerSource: overrides.analyzerSource,
    analyzerRunId: overrides.analyzerRunId,
    resultReference: overrides.resultReference,
    scoredAt: overrides.scoredAt,
    sourceCandidateId: overrides.sourceCandidateId,
    sourceArtifactVersionFingerprint: [...overrides.sourceArtifactVersionFingerprint],
    analyzerCompletionStatus: 'completed',
    validationResultStatus: overrides.validationResultStatus,
    validationApprovalState: overrides.validationApprovalState,
    findingReferenceIds: [...(overrides.findingReferenceIds ?? [])],
    recommendationReferenceIds: [...(overrides.recommendationReferenceIds ?? [])],
    linkedProposalIds: [...(overrides.linkedProposalIds ?? [])],
    provenance: overrides.provenance ?? {
      source: 'analyzerWorkspace',
      timestamp: overrides.scoredAt,
    },
  };
}

async function createDraftState(context: ExtensionContext, threadId: string): Promise<DraftState> {
  await saveDesignBriefDraft(context, threadId, {
    audience: 'Sales leaders',
    businessObjective: 'Reduce missed renewals',
    keyDecisions: ['Which regions need intervention first'],
    primaryKpis: ['Renewal rate', 'At-risk pipeline'],
    dimensions: ['Region', 'Segment'],
    intendedStory: 'Lead with risk, then explain the main drivers and next steps.',
    successCriteria: ['Leader can decide the next action within five minutes'],
    reportType: 'dashboard',
    navigationExpectations: 'Overview first, detail second.',
    consumptionContext: 'Weekly renewal review',
    decisionCadence: 'Weekly',
    narrativeRisksOrConstraints: ['Avoid hiding segment outliers'],
    requiredEvidenceDomains: ['renewal trend', 'pipeline coverage'],
    targetAnalyzableSurfaceFamily: 'pbir',
  });
  await submitDesignBriefForApproval(context, threadId);
  await approveDesignBrief(context, threadId);
  const conceptState = await generateConceptArtifacts(context, threadId);
  await selectConceptBaseline(context, threadId, conceptState.currentConcept.alternateConcepts[0].id);
  await submitConceptBaselineForApproval(context, threadId);
  await approveConceptBaseline(context, threadId);
  return generateDraftArtifacts(context, threadId);
}

function sourceVersionIds(state: DraftState): string[] {
  return [
    `${state.brief.id}@v${state.brief.version}`,
    `${state.concept.id}@v${state.concept.version}`,
    `${state.concept.navigationStructure.id}@v${state.concept.navigationStructure.version}`,
    `${state.concept.kpiHierarchy.id}@v${state.concept.kpiHierarchy.version}`,
    ...state.concept.pageConcepts.map((pageConcept) => `${pageConcept.id}@v${pageConcept.version}`),
    `${state.currentDraft.id}@v${state.currentDraft.version}`,
    ...state.pageArtifacts.map((artifact) => `${artifact.id}@v${artifact.version}`),
    ...state.layoutArtifacts.map((artifact) => `${artifact.id}@v${artifact.version}`),
    ...state.navigationArtifacts.map((artifact) => `${artifact.id}@v${artifact.version}`),
  ];
}

async function buildCandidate(
  context: ExtensionContext,
  state: DraftState,
  requestId = 'iteration',
): Promise<MaterializedSurfaceCandidate> {
  const request = await createApprovedDraftMaterializationRequest(context, {
    threadId: state.threadId,
    requestId: `materialization-request:${state.threadId}:${requestId}`,
    targetSurfaceType: 'pbirReport',
    targetAnalyzer: 'pbirDesignReview',
    targetAnalyzerProfile: 'default',
  });
  const result = materializeDesignStudioRequest(request);
  if (!result.ok) {
    throw new Error(result.diagnostics.join('\n'));
  }

  return result.candidate;
}

async function createApprovedProposals(
  context: ExtensionContext,
  state: DraftState,
  threadId: string,
  options?: {
    analyzerRunId?: string;
    resultReference?: string;
    issueId?: string;
    issueTitle?: string;
    issueSummary?: string;
    issueRecommendation?: string;
  },
): Promise<RefinementProposal[]> {
  const pageName = state.concept.pageConcepts[0]?.title ?? 'Executive overview';
  const created = await ingestIssues(context, threadId, {
    analyzerRunId: options?.analyzerRunId ?? 'run-issues-1',
    resultReference: options?.resultReference ?? 'issues:1',
    sourceArtifactVersionIds: sourceVersionIds(state),
    reportPath: '/tmp/sales.pbir',
    scoredAt: '2026-06-13T10:01:00.000Z',
    issues: [
      {
        id: options?.issueId ?? 'finding-1',
        title: options?.issueTitle ?? 'Navigation drift',
        summary: options?.issueSummary ?? 'Users do not move cleanly from summary to detail.',
        severity: 'high',
        confidence: 0.91,
        scope: 'crossPage',
        detectionType: 'deterministic',
        affectedPages: [pageName],
        impactArea: 'navigation',
        frameworkImpact: ['narrative'],
        recommendation: options?.issueRecommendation ?? 'Reduce the number of branches and clarify the path.',
        sourceKind: 'issues',
        sourceSection: 'issues',
        evidence: [
          {
            kind: 'storyAssessment',
            label: 'Narrative breakdown',
            pageName,
          },
        ],
      },
    ],
  });

  const proposalId = created.proposals[0]?.id;
  if (!proposalId) {
    throw new Error('Expected refinement proposal.');
  }

  const approved = await approveRefinementProposal(context, threadId, proposalId);
  return approved.proposals;
}

function cloneProposal(proposal: RefinementProposal): RefinementProposal {
  return JSON.parse(JSON.stringify(proposal)) as RefinementProposal;
}

function attachAnalyzerCandidateLineage(
  proposals: RefinementProposal[],
  candidate: MaterializedSurfaceCandidate,
  artifactVersionFingerprint: string[],
): RefinementProposal[] {
  return proposals.map((proposal) => ({
    ...cloneProposal(proposal),
    sourceAnalyzerOutput: {
      ...proposal.sourceAnalyzerOutput,
      sourceCandidateId: candidate.id,
      sourceArtifactVersionFingerprint: [...artifactVersionFingerprint],
    },
  }));
}

describe('iterationStore', () => {
  it('stores approval checkpoints without contradictory top-level approval metadata', async () => {
    const context = makeContext(makeTempDir());
    const pendingDraft = await createDraftState(context, 'thread-closed-loop-lineage');
    await submitDraftForApproval(context, 'thread-closed-loop-lineage');
    const draftState = await approveDraftArtifacts(context, 'thread-closed-loop-lineage');
    const candidate = await buildCandidate(context, draftState);
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, 'thread-closed-loop-lineage'),
      candidate,
      sourceVersionIds(draftState),
    );

    const created = await recordIteration(context, {
      threadId: 'thread-closed-loop-lineage',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs: proposals.map((proposal) => proposal.sourceAnalyzerOutput),
      refinementProposals: proposals,
    });

    expect(created.iterations).toHaveLength(1);
    expect(created.iterations[0]).toEqual(expect.objectContaining({
      previousIterationId: undefined,
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      materializedCandidate: expect.objectContaining({
        candidateId: candidate.id,
      }),
      analyzerResults: [
        expect.objectContaining({
          analyzerRunId: 'run-issues-1',
          resultReference: 'issues:1',
        }),
      ],
      refinementProposals: [
        expect.objectContaining({
          proposalId: proposals[0]?.id,
          approvalState: 'approved',
        }),
      ],
      approvalCheckpoint: expect.objectContaining({
        designApproval: expect.objectContaining({ approvalState: 'approved' }),
        materializationApproval: expect.objectContaining({ approvalState: 'approved' }),
        refinementApproval: expect.objectContaining({ approvalState: 'approved' }),
        validationApproval: expect.objectContaining({ approvalState: 'notSubmitted' }),
      }),
    }));
    expect(created.iterations[0]).not.toHaveProperty('approvalState');
    expect(created.iterations[0]).not.toHaveProperty('approvalKind');
    expect(pendingDraft.history[0]?.draft.approvalState).toBe('notSubmitted');

    const reloaded = await loadIterationState(context, 'thread-closed-loop-lineage');
    expect(reloaded).toEqual(created);
  });

  it('stores canonical recommendation states in iteration history and treats only proposed or deferred recommendations as unresolved', async () => {
    const context = makeContext(makeTempDir());
    const threadId = 'thread-closed-loop-recommendation-state';
    await createDraftState(context, threadId);
    await submitDraftForApproval(context, threadId);
    const draftState = await approveDraftArtifacts(context, threadId);
    const candidate = await buildCandidate(context, draftState, 'recommendation-state');

    const approvedProposals = await createApprovedProposals(context, draftState, threadId);
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';
    const approvedProposal = approvedProposals[0];
    if (!approvedProposal) {
      throw new Error('Expected an approved proposal.');
    }

    const deferredCreated = await ingestIssues(context, threadId, {
      analyzerRunId: 'run-issues-deferred',
      resultReference: 'issues:deferred',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      reportPath: '/tmp/recommendation-state.pbir',
      scoredAt: '2026-06-17T20:40:00.000Z',
      issues: [
        {
          id: 'finding-deferred',
          title: 'Deferred benchmark branch',
          summary: 'The benchmark branch can wait until the next pass.',
          severity: 'medium',
          confidence: 0.8,
          scope: 'crossPage',
          detectionType: 'deterministic',
          affectedPages: [pageName],
          impactArea: 'navigation',
          frameworkImpact: ['narrative'],
          recommendation: 'Defer the benchmark branch.',
          sourceKind: 'issues',
          sourceSection: 'issues',
          evidence: [],
        },
      ],
    });
    const deferredProposalId = deferredCreated.proposals.at(-1)?.id;
    if (!deferredProposalId) {
      throw new Error('Expected deferred proposal.');
    }
    await deferRefinementProposal(context, threadId, deferredProposalId);

    const rejectedCreated = await ingestIssues(context, threadId, {
      analyzerRunId: 'run-issues-rejected',
      resultReference: 'issues:rejected',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      reportPath: '/tmp/recommendation-state.pbir',
      scoredAt: '2026-06-17T20:41:00.000Z',
      issues: [
        {
          id: 'finding-rejected',
          title: 'Rejected benchmark card',
          summary: 'This benchmark card should not be carried forward.',
          severity: 'medium',
          confidence: 0.81,
          scope: 'crossPage',
          detectionType: 'deterministic',
          affectedPages: [pageName],
          impactArea: 'navigation',
          frameworkImpact: ['narrative'],
          recommendation: 'Reject the benchmark card.',
          sourceKind: 'issues',
          sourceSection: 'issues',
          evidence: [],
        },
      ],
    });
    const rejectedProposalId = rejectedCreated.proposals.at(-1)?.id;
    if (!rejectedProposalId) {
      throw new Error('Expected rejected proposal.');
    }
    await rejectRefinementProposal(context, threadId, rejectedProposalId);

    const attachedResults = [
      makeAnalyzerResultReference({
        analyzerSource: 'issues',
        analyzerRunId: approvedProposal.sourceAnalyzerOutput.analyzerRunId,
        resultReference: approvedProposal.sourceAnalyzerOutput.resultReference,
        scoredAt: approvedProposal.sourceAnalyzerOutput.scoredAt,
        sourceCandidateId: candidate.id,
        sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
        validationResultStatus: 'needsReview',
        validationApprovalState: 'notSubmitted',
      }),
      makeAnalyzerResultReference({
        analyzerSource: 'issues',
        analyzerRunId: 'run-issues-deferred',
        resultReference: 'issues:deferred',
        scoredAt: '2026-06-17T20:40:00.000Z',
        sourceCandidateId: candidate.id,
        sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
        validationResultStatus: 'needsReview',
        validationApprovalState: 'notSubmitted',
      }),
      makeAnalyzerResultReference({
        analyzerSource: 'issues',
        analyzerRunId: 'run-issues-rejected',
        resultReference: 'issues:rejected',
        scoredAt: '2026-06-17T20:41:00.000Z',
        sourceCandidateId: candidate.id,
        sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
        validationResultStatus: 'needsReview',
        validationApprovalState: 'notSubmitted',
      }),
    ];
    await attachAnalyzerResultLineage(context, threadId, attachedResults);
    const proposals = (await loadRefinementState(context, threadId))?.proposals ?? [];
    const deferredProposal = proposals.find((proposal) => proposal.id === deferredProposalId);
    const rejectedProposal = proposals.find((proposal) => proposal.id === rejectedProposalId);
    if (!deferredProposal || !rejectedProposal) {
      throw new Error('Expected persisted deferred and rejected proposals.');
    }

    const created = await recordIteration(context, {
      threadId,
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs: proposals.map((proposal) => proposal.sourceAnalyzerOutput),
      refinementProposals: proposals,
    });

    expect(created.iterations[0]?.comparisonSnapshot.recommendations).toEqual(expect.arrayContaining([
      expect.objectContaining({
        proposalId: approvedProposal.id,
        recommendationState: 'approved',
      }),
      expect.objectContaining({
        proposalId: deferredProposal.id,
        recommendationState: 'deferred',
      }),
      expect.objectContaining({
        proposalId: rejectedProposal.id,
        recommendationState: 'rejected',
      }),
    ]));
    expect(created.iterations[0]?.workflowCompletion.deferredRecommendationCount).toBe(1);
    expect(created.iterations[0]?.workflowCompletion.unresolvedRecommendationCount).toBe(1);
  });

  it('compares before and after concept, draft, analyzer, recommendation, and validation changes', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-closed-loop-compare');
    await submitDraftForApproval(context, 'thread-closed-loop-compare');
    let draftState = await approveDraftArtifacts(context, 'thread-closed-loop-compare');
    let candidate = await buildCandidate(context, draftState, 'initial');
    let proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, 'thread-closed-loop-compare'),
      candidate,
      sourceVersionIds(draftState),
    );

    const initial = await recordIteration(context, {
      threadId: 'thread-closed-loop-compare',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs: proposals.map((proposal) => proposal.sourceAnalyzerOutput),
      refinementProposals: proposals,
    });

    const conceptState = await loadConceptState(context, 'thread-closed-loop-compare');
    const secondConceptId = conceptState?.currentConcept.alternateConcepts[1]?.id;
    if (!secondConceptId) {
      throw new Error('Expected a second alternate concept for comparison.');
    }

    await selectConceptBaseline(context, 'thread-closed-loop-compare', secondConceptId);
    await submitConceptBaselineForApproval(context, 'thread-closed-loop-compare');
    await approveConceptBaseline(context, 'thread-closed-loop-compare');
    await generateDraftArtifacts(context, 'thread-closed-loop-compare');
    await submitDraftForApproval(context, 'thread-closed-loop-compare');
    draftState = await approveDraftArtifacts(context, 'thread-closed-loop-compare');
    candidate = await buildCandidate(context, draftState, 'comparison');
    proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, 'thread-closed-loop-compare', {
        analyzerRunId: 'run-issues-2',
        resultReference: 'issues:2',
        issueId: 'finding-2',
        issueTitle: 'Executive path branching',
        issueSummary: 'Executives branch too early and lose the main story thread.',
        issueRecommendation: 'Reduce branching and add a stronger executive entry point.',
      }),
      candidate,
      sourceVersionIds(draftState),
    );
    const comparisonVersions = sourceVersionIds(draftState);

    const compared = await recordIteration(context, {
      threadId: 'thread-closed-loop-compare',
      previousIterationId: initial.iterations[0]!.id,
      sourceArtifactVersionIds: comparisonVersions,
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs: proposals.map((proposal) => proposal.sourceAnalyzerOutput),
      refinementProposals: proposals,
      validationApproval: {
        approvalState: 'approved',
        provenance: { source: 'analyzerWorkspace' },
        validationLinkage: buildValidationApprovalEvidence({
          analyzerRunId: 'run-issues-2',
          resultReference: 'issues:2',
          sourceCandidateId: candidate.id,
          sourceArtifactVersionFingerprint: comparisonVersions,
          validationResultStatus: 'validated',
        }),
      },
    });

    const result = await compareIterations(
      context,
      'thread-closed-loop-compare',
      initial.iterations[0]!.id,
      compared.iterations[1]!.id,
    );

    expect(result.conceptChanges).toEqual(expect.arrayContaining([
      'Changed navigation structure.',
    ]));
    expect(result.draftChanges).toEqual(expect.arrayContaining([
      expect.stringContaining('Added'),
    ]));
    expect(result.analyzerOutputChanges).toEqual([]);
    expect(result.recommendationChanges).toEqual(expect.arrayContaining([
      'Accepted recommendation: Refine the design to address navigation drift.',
    ]));
    expect(result.approvalEvolution).toEqual(expect.arrayContaining([
      'Validation Approval changed from Not submitted to Approved.',
    ]));
    expect(result.validationEvolution).toEqual(expect.arrayContaining([
      'Validation status changed from Not submitted to Validated.',
    ]));
    expect(result.summary).not.toContain('issues:2');
  });

  it('requires analyzer-owned provenance before validation approval can be recorded', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-closed-loop-validation');
    await submitDraftForApproval(context, 'thread-closed-loop-validation');
    const draftState = await approveDraftArtifacts(context, 'thread-closed-loop-validation');
    const candidate = await buildCandidate(context, draftState);

    await expect(recordIteration(context, {
      threadId: 'thread-closed-loop-validation',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs: [],
      refinementProposals: [],
      validationApproval: {
        approvalState: 'approved',
        provenance: { source: 'system' },
        validationLinkage: buildValidationApprovalEvidence({
          analyzerRunId: 'run-invalid',
          resultReference: 'issues:invalid',
          sourceCandidateId: candidate.id,
          sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
          validationResultStatus: 'validated',
        }),
      },
    })).rejects.toThrow('Validation approval requires analyzer-owned provenance.');
  });

  it('never auto-optimizes, auto-executes analyzers, mutates reports, or generates PBIR files', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-closed-loop-guardrails');
    await submitDraftForApproval(context, 'thread-closed-loop-guardrails');
    const draftState = await approveDraftArtifacts(context, 'thread-closed-loop-guardrails');
    const candidate = await buildCandidate(context, draftState);
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, 'thread-closed-loop-guardrails'),
      candidate,
      sourceVersionIds(draftState),
    );

    const created = await recordIteration(context, {
      threadId: 'thread-closed-loop-guardrails',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs: proposals.map((proposal) => proposal.sourceAnalyzerOutput),
      refinementProposals: proposals,
    });

    expect(created.iterations[0]?.guardrails).toEqual({
      autoOptimizationTriggered: false,
      analyzerExecutionTriggered: false,
      reportMutationTriggered: false,
      pbirFilesGenerated: false,
    });
  });

  it('attaches analyzer results explicitly, preserves lineage, and does not auto-grant validation approval', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-closed-loop-attach');
    await submitDraftForApproval(context, 'thread-closed-loop-attach');
    const draftState = await approveDraftArtifacts(context, 'thread-closed-loop-attach');
    await createReviewCandidate(context, {
      threadId: 'thread-closed-loop-attach',
      reportPath: '/tmp/Attach Analyzer Results.Report.pbir',
    });
    await submitReviewCandidateForApproval(context, 'thread-closed-loop-attach');
    const approvedCandidate = await approveReviewCandidate(context, 'thread-closed-loop-attach');
    const proposals = await createApprovedProposals(context, draftState, 'thread-closed-loop-attach', {
      analyzerRunId: 'run-attach-1',
      resultReference: 'issues:attach-1',
    });

    await recordReviewLaunch(context, 'thread-closed-loop-attach', {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      analyzerId: approvedCandidate.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedCandidate.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, 'thread-closed-loop-attach', {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });
    await recordAnalyzerResultsAvailable(context, 'thread-closed-loop-attach', {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: 'issues',
          analyzerRunId: 'run-attach-1',
          resultReference: 'issues:attach-1',
          scoredAt: '2026-06-16T18:20:00.000Z',
          sourceCandidateId: approvedCandidate.currentCandidate.id,
          sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
          validationResultStatus: 'needsReview',
          validationApprovalState: 'notSubmitted',
          findingReferenceIds: proposals.flatMap((proposal) => proposal.linkedFindingIds),
          recommendationReferenceIds: proposals.map((proposal) => proposal.id),
          linkedProposalIds: proposals.map((proposal) => proposal.id),
        }),
      ],
    });
    await attachAnalyzerResultLineage(context, 'thread-closed-loop-attach', [
      makeAnalyzerResultReference({
        analyzerSource: 'issues',
        analyzerRunId: 'run-attach-1',
        resultReference: 'issues:attach-1',
        scoredAt: '2026-06-16T18:20:00.000Z',
        sourceCandidateId: approvedCandidate.currentCandidate.id,
        sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
        validationResultStatus: 'needsReview',
        validationApprovalState: 'notSubmitted',
        findingReferenceIds: proposals.flatMap((proposal) => proposal.linkedFindingIds),
        recommendationReferenceIds: proposals.map((proposal) => proposal.id),
        linkedProposalIds: proposals.map((proposal) => proposal.id),
      }),
    ]);

    await markAnalyzerResultsAttached(context, 'thread-closed-loop-attach', {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });
    const attached = await attachAvailableAnalyzerResults(context, 'thread-closed-loop-attach');

    expect(attached.iterations).toHaveLength(1);
    expect(attached.iterations[0]?.sourceArtifactVersionIds).toEqual(
      [...sourceVersionIds(draftState)].sort((left, right) => left.localeCompare(right)),
    );
    expect(attached.iterations[0]?.materializedCandidate).toEqual(expect.objectContaining({
      candidateId: approvedCandidate.currentCandidate.id,
      sourceLineage: expect.arrayContaining([
        `${draftState.currentDraft.id}@v${draftState.currentDraft.version}`,
      ]),
    }));
    expect(attached.iterations[0]?.analyzerResults).toEqual(expect.arrayContaining([
      expect.objectContaining({
        analyzerRunId: 'run-attach-1',
        resultReference: 'issues:attach-1',
      }),
    ]));
    expect(attached.iterations[0]?.approvalCheckpoint.validationApproval.approvalState).toBe('notSubmitted');
    expect(attached.iterations[0]?.guardrails).toEqual({
      autoOptimizationTriggered: false,
      analyzerExecutionTriggered: false,
      reportMutationTriggered: false,
      pbirFilesGenerated: false,
    });
  });

  it('attaches analyzer results atomically and records validation approval only from analyzer-owned evidence', async () => {
    const context = makeContext(makeTempDir());
    const threadId = 'thread-closed-loop-atomic-attach';

    await createDraftState(context, threadId);
    await submitDraftForApproval(context, threadId);
    const draftState = await approveDraftArtifacts(context, threadId);
    await createReviewCandidate(context, { threadId, reportPath: '/tmp/atomic-attach.Report.pbir' });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedReview = await approveReviewCandidate(context, threadId);
    await recordReviewLaunch(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      analyzerId: approvedReview.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedReview.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, threadId),
      approvedReview.currentCandidate,
      sourceVersionIds(draftState),
    );
    await recordAnalyzerResultsAvailable(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: proposals[0]!.sourceAnalyzerOutput.analyzerSource,
          analyzerRunId: proposals[0]!.sourceAnalyzerOutput.analyzerRunId,
          resultReference: proposals[0]!.sourceAnalyzerOutput.resultReference,
          scoredAt: proposals[0]!.sourceAnalyzerOutput.scoredAt,
          sourceCandidateId: approvedReview.currentCandidate.id,
          sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
          validationResultStatus: 'validated',
          validationApprovalState: 'approved',
          findingReferenceIds: proposals.flatMap((proposal) => proposal.linkedFindingIds),
          recommendationReferenceIds: proposals.map((proposal) => proposal.id),
          linkedProposalIds: proposals.map((proposal) => proposal.id),
        }),
      ],
    });

    const attached = await attachAnalyzerResultsAtomically(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });

    expect(attached.ok).toBe(true);
    const reviewState = await loadReviewDesignState(context, threadId);
    const iterationState = await loadIterationState(context, threadId);
    expect(reviewState?.currentReview?.attachedResults).toHaveLength(1);
    expect(iterationState?.iterations).toHaveLength(1);
    expect(iterationState?.iterations[0]?.approvalCheckpoint.validationApproval).toEqual(expect.objectContaining({
      approvalState: 'approved',
      owner: 'analyzerWorkspace',
      analyzerRunId: proposals[0]!.sourceAnalyzerOutput.analyzerRunId,
      resultReference: proposals[0]!.sourceAnalyzerOutput.resultReference,
      sourceCandidateId: approvedReview.currentCandidate.id,
      validationResultStatus: 'validated',
    }));
    expect(iterationState?.iterations[0]?.workflowCompletion.checklist).toEqual(expect.arrayContaining([
      expect.objectContaining({ id: 'reviewCompleted', satisfied: true }),
      expect.objectContaining({ id: 'analyzerResultsAttached', satisfied: true }),
      expect.objectContaining({ id: 'validationApprovalRecorded', satisfied: true }),
    ]));
  });

  it('rejects atomic attachment when analyzer result lineage is missing and leaves all state unchanged', async () => {
    const context = makeContext(makeTempDir());
    const threadId = 'thread-closed-loop-missing-lineage';

    await createDraftState(context, threadId);
    await submitDraftForApproval(context, threadId);
    const draftState = await approveDraftArtifacts(context, threadId);
    await createReviewCandidate(context, { threadId, reportPath: '/tmp/missing-lineage.Report.pbir' });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedReview = await approveReviewCandidate(context, threadId);
    await recordReviewLaunch(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      analyzerId: approvedReview.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedReview.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, threadId),
      approvedReview.currentCandidate,
      sourceVersionIds(draftState),
    );
    await recordAnalyzerResultsAvailable(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: proposals[0]!.sourceAnalyzerOutput.analyzerSource,
          analyzerRunId: proposals[0]!.sourceAnalyzerOutput.analyzerRunId,
          resultReference: proposals[0]!.sourceAnalyzerOutput.resultReference,
          scoredAt: proposals[0]!.sourceAnalyzerOutput.scoredAt,
          sourceCandidateId: approvedReview.currentCandidate.id,
          sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
          validationResultStatus: 'needsReview',
          validationApprovalState: 'notSubmitted',
          findingReferenceIds: proposals.flatMap((proposal) => proposal.linkedFindingIds),
          recommendationReferenceIds: proposals.map((proposal) => proposal.id),
          linkedProposalIds: proposals.map((proposal) => proposal.id),
        }),
      ],
    });

    const manifestPath = reviewDesignManifestPath(context.globalStorageUri.fsPath, threadId);
    const persisted = JSON.parse(fs.readFileSync(manifestPath, 'utf8')) as {
      currentReview: {
        availableResults: Array<Record<string, unknown>>;
      };
    };
    persisted.currentReview.availableResults[0] = {
      ...persisted.currentReview.availableResults[0],
      sourceCandidateId: '',
    };
    fs.writeFileSync(manifestPath, JSON.stringify(persisted, null, 2), 'utf8');

    const attached = await attachAnalyzerResultsAtomically(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });

    expect(attached).toEqual(expect.objectContaining({
      ok: false,
      error: expect.stringContaining('source candidate lineage'),
    }));
    const reviewState = await loadReviewDesignState(context, threadId);
    const iterationState = await loadIterationState(context, threadId);
    expect(reviewState?.currentReview?.attachedResults).toHaveLength(0);
    expect(iterationState).toBeUndefined();
  });

  it('rejects atomic attachment when analyzer result fingerprint is missing and leaves all state unchanged', async () => {
    const context = makeContext(makeTempDir());
    const threadId = 'thread-closed-loop-missing-fingerprint';

    await createDraftState(context, threadId);
    await submitDraftForApproval(context, threadId);
    const draftState = await approveDraftArtifacts(context, threadId);
    await createReviewCandidate(context, { threadId, reportPath: '/tmp/missing-fingerprint.Report.pbir' });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedReview = await approveReviewCandidate(context, threadId);
    await recordReviewLaunch(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      analyzerId: approvedReview.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedReview.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, threadId),
      approvedReview.currentCandidate,
      sourceVersionIds(draftState),
    );
    await recordAnalyzerResultsAvailable(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: proposals[0]!.sourceAnalyzerOutput.analyzerSource,
          analyzerRunId: proposals[0]!.sourceAnalyzerOutput.analyzerRunId,
          resultReference: proposals[0]!.sourceAnalyzerOutput.resultReference,
          scoredAt: proposals[0]!.sourceAnalyzerOutput.scoredAt,
          sourceCandidateId: approvedReview.currentCandidate.id,
          sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
          validationResultStatus: 'needsReview',
          validationApprovalState: 'notSubmitted',
          findingReferenceIds: proposals.flatMap((proposal) => proposal.linkedFindingIds),
          recommendationReferenceIds: proposals.map((proposal) => proposal.id),
          linkedProposalIds: proposals.map((proposal) => proposal.id),
        }),
      ],
    });

    const manifestPath = reviewDesignManifestPath(context.globalStorageUri.fsPath, threadId);
    const persisted = JSON.parse(fs.readFileSync(manifestPath, 'utf8')) as {
      currentReview: {
        availableResults: Array<Record<string, unknown>>;
      };
    };
    persisted.currentReview.availableResults[0] = {
      ...persisted.currentReview.availableResults[0],
      sourceArtifactVersionFingerprint: [],
    };
    fs.writeFileSync(manifestPath, JSON.stringify(persisted, null, 2), 'utf8');

    const attached = await attachAnalyzerResultsAtomically(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });

    expect(attached).toEqual(expect.objectContaining({
      ok: false,
      error: expect.stringContaining('source artifact/version fingerprint'),
    }));
    const reviewState = await loadReviewDesignState(context, threadId);
    const iterationState = await loadIterationState(context, threadId);
    expect(reviewState?.currentReview?.attachedResults).toHaveLength(0);
    expect(iterationState).toBeUndefined();
  });

  it('rolls back atomic attachment when iteration persistence fails', async () => {
    const context = makeContext(makeTempDir());
    const threadId = 'thread-closed-loop-rollback';

    await createDraftState(context, threadId);
    await submitDraftForApproval(context, threadId);
    const draftState = await approveDraftArtifacts(context, threadId);
    await createReviewCandidate(context, { threadId, reportPath: '/tmp/rollback.Report.pbir' });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedReview = await approveReviewCandidate(context, threadId);
    await recordReviewLaunch(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      analyzerId: approvedReview.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedReview.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, threadId),
      approvedReview.currentCandidate,
      sourceVersionIds(draftState),
    );
    await recordAnalyzerResultsAvailable(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: proposals[0]!.sourceAnalyzerOutput.analyzerSource,
          analyzerRunId: proposals[0]!.sourceAnalyzerOutput.analyzerRunId,
          resultReference: proposals[0]!.sourceAnalyzerOutput.resultReference,
          scoredAt: proposals[0]!.sourceAnalyzerOutput.scoredAt,
          sourceCandidateId: approvedReview.currentCandidate.id,
          sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
          validationResultStatus: 'needsReview',
          validationApprovalState: 'notSubmitted',
          findingReferenceIds: proposals.flatMap((proposal) => proposal.linkedFindingIds),
          recommendationReferenceIds: proposals.map((proposal) => proposal.id),
          linkedProposalIds: proposals.map((proposal) => proposal.id),
        }),
      ],
    });

    const prepareManifest = prepareForReviewManifestPath(context.globalStorageUri.fsPath, threadId);
    const persisted = JSON.parse(fs.readFileSync(prepareManifest, 'utf8')) as {
      currentCandidate: {
        id: string;
      };
    };
    persisted.currentCandidate.id = `${persisted.currentCandidate.id}:corrupted`;
    fs.writeFileSync(prepareManifest, JSON.stringify(persisted, null, 2), 'utf8');

    const attached = await attachAnalyzerResultsAtomically(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });

    expect(attached).toEqual(expect.objectContaining({
      ok: false,
      error: expect.stringContaining('active review candidate lineage'),
    }));
    const reviewState = await loadReviewDesignState(context, threadId);
    const iterationState = await loadIterationState(context, threadId);
    expect(reviewState?.currentReview?.attachedResults).toHaveLength(0);
    expect(iterationState).toBeUndefined();
  });

  it('rejects source artifact version ids that do not exist in persisted draft state', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-closed-loop-source-validation');
    await submitDraftForApproval(context, 'thread-closed-loop-source-validation');
    const draftState = await approveDraftArtifacts(context, 'thread-closed-loop-source-validation');

    await expect(recordIteration(context, {
      threadId: 'thread-closed-loop-source-validation',
      sourceArtifactVersionIds: [`${draftState.currentDraft.id}@v999`],
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      analyzerOutputs: [],
      refinementProposals: [],
    })).rejects.toThrow('Iteration source artifact versions do not match the persisted Draft Studio state.');
  });

  it('rejects materialized candidate lineage that does not match source artifact versions', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-closed-loop-candidate-mismatch');
    await submitDraftForApproval(context, 'thread-closed-loop-candidate-mismatch');
    const draftState = await approveDraftArtifacts(context, 'thread-closed-loop-candidate-mismatch');
    const candidate = await buildCandidate(context, draftState);
    candidate.sourceLineage[0] = {
      ...candidate.sourceLineage[0]!,
      artifactVersionId: `${draftState.currentDraft.id}@v999`,
    };

    await expect(recordIteration(context, {
      threadId: 'thread-closed-loop-candidate-mismatch',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs: [],
      refinementProposals: [],
    })).rejects.toThrow('Materialized candidate lineage must match the iteration source artifact versions.');
  });

  it('rejects analyzer outputs whose source candidate does not match the materialized candidate', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-closed-loop-analyzer-mismatch');
    await submitDraftForApproval(context, 'thread-closed-loop-analyzer-mismatch');
    const draftState = await approveDraftArtifacts(context, 'thread-closed-loop-analyzer-mismatch');
    const candidate = await buildCandidate(context, draftState);
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, 'thread-closed-loop-analyzer-mismatch'),
      candidate,
      sourceVersionIds(draftState),
    );
    proposals[0] = {
      ...cloneProposal(proposals[0]!),
      sourceAnalyzerOutput: {
        ...proposals[0]!.sourceAnalyzerOutput,
        sourceCandidateId: `${candidate.id}:different`,
      },
    };

    await expect(recordIteration(context, {
      threadId: 'thread-closed-loop-analyzer-mismatch',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs: proposals.map((proposal) => proposal.sourceAnalyzerOutput),
      refinementProposals: proposals,
    })).rejects.toThrow('Analyzer outputs must reference the iteration materialized candidate lineage.');
  });

  it('rejects refinement proposals whose analyzer lineage does not match the iteration candidate lineage', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-closed-loop-refinement-mismatch');
    await submitDraftForApproval(context, 'thread-closed-loop-refinement-mismatch');
    const draftState = await approveDraftArtifacts(context, 'thread-closed-loop-refinement-mismatch');
    const candidate = await buildCandidate(context, draftState);
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, 'thread-closed-loop-refinement-mismatch'),
      candidate,
      sourceVersionIds(draftState),
    );
    const analyzerOutputs = proposals.map((proposal) => proposal.sourceAnalyzerOutput);
    proposals[0] = {
      ...cloneProposal(proposals[0]!),
      sourceAnalyzerOutput: {
        ...proposals[0]!.sourceAnalyzerOutput,
        sourceArtifactVersionFingerprint: [`${draftState.currentDraft.id}@v999`],
      },
    };

    await expect(recordIteration(context, {
      threadId: 'thread-closed-loop-refinement-mismatch',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs,
      refinementProposals: proposals,
    })).rejects.toThrow('Refinement proposals must preserve analyzer candidate lineage and source artifact fingerprints.');
  });

  it('evaluates completion readiness from approvals, review completion, and deferred recommendations without implying validation approval', async () => {
    const context = makeContext(makeTempDir());
    const threadId = 'thread-workflow-completion-readiness';

    await createDraftState(context, threadId);
    await submitDraftForApproval(context, threadId);
    const draftState = await approveDraftArtifacts(context, threadId);
    const candidate = await buildCandidate(context, draftState, 'completion-ready');
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, threadId),
      candidate,
      sourceVersionIds(draftState),
    );

    const readiness = await evaluateIterationCompletion(context, threadId);
    expect(readiness.state).toBe('active');
    expect(readiness.isEligible).toBe(false);
    expect(readiness.outstandingItems).toEqual(expect.arrayContaining([
      'Review candidate approval is still required.',
      'Review Design must be completed before the iteration can be closed.',
    ]));

    const recorded = await recordIteration(context, {
      threadId,
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: {
        ...candidate,
        approvalState: 'approved',
      },
      analyzerOutputs: proposals.map((proposal) => proposal.sourceAnalyzerOutput),
      refinementProposals: proposals,
    });

    await expect(completeIteration(context, threadId)).rejects.toThrow('Iteration is not ready for completion.');

    expect(recorded.iterations[0]?.approvalCheckpoint.validationApproval.approvalState).toBe('notSubmitted');
  });

  it('completes and reopens the latest iteration while preserving lineage, approvals, and audit history', async () => {
    const context = makeContext(makeTempDir());
    const threadId = 'thread-workflow-completion-transitions';
    const reportPath = '/tmp/workflow-completion-transitions.Report.pbir';

    await createDraftState(context, threadId);
    await submitDraftForApproval(context, threadId);
    const draftState = await approveDraftArtifacts(context, threadId);
    await createReviewCandidate(context, { threadId, reportPath });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedReview = await approveReviewCandidate(context, threadId);
    await recordReviewLaunch(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      analyzerId: approvedReview.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedReview.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });
    const candidate = approvedReview.currentCandidate;
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, threadId),
      candidate,
      sourceVersionIds(draftState),
    );
    await recordAnalyzerResultsAvailable(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: proposals[0]!.sourceAnalyzerOutput.analyzerSource,
          analyzerRunId: proposals[0]!.sourceAnalyzerOutput.analyzerRunId,
          resultReference: proposals[0]!.sourceAnalyzerOutput.resultReference,
          scoredAt: proposals[0]!.sourceAnalyzerOutput.scoredAt,
          sourceCandidateId: candidate.id,
          sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
          validationResultStatus: 'needsReview',
          validationApprovalState: 'notSubmitted',
          findingReferenceIds: proposals.flatMap((proposal) => proposal.linkedFindingIds),
          recommendationReferenceIds: proposals.map((proposal) => proposal.id),
          linkedProposalIds: proposals.map((proposal) => proposal.id),
        }),
      ],
    });
    await markAnalyzerResultsAttached(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate,
    });

    const created = await recordIteration(context, {
      threadId,
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs: proposals.map((proposal) => proposal.sourceAnalyzerOutput),
      refinementProposals: proposals,
    });

    expect(created.iterations[0]?.workflowCompletion.state).toBe('readyForCompletion');

    const completed = await completeIteration(context, threadId);
    expect(completed.iterations[0]?.workflowCompletion.state).toBe('completed');
    expect(completed.iterations[0]?.workflowCompletion.completedAt).toEqual(expect.any(String));
    expect(completed.iterations[0]?.workflowCompletion.completedBy).toBe('user');
    expect(completed.iterations[0]?.workflowCompletion.history).toEqual(expect.arrayContaining([
      expect.objectContaining({
        action: 'completed',
        actor: 'user',
      }),
    ]));
    expect(completed.iterations[0]?.sourceArtifactVersionIds).toEqual(created.iterations[0]?.sourceArtifactVersionIds);
    expect(completed.iterations[0]?.approvalCheckpoint).toEqual(created.iterations[0]?.approvalCheckpoint);

    const reopened = await reopenIteration(context, threadId);
    expect(reopened.iterations[0]?.workflowCompletion.state).toBe('reopened');
    expect(reopened.iterations[0]?.workflowCompletion.reopenedAt).toEqual(expect.any(String));
    expect(reopened.iterations[0]?.workflowCompletion.reopenedBy).toBe('user');
    expect(reopened.iterations[0]?.workflowCompletion.history).toEqual(expect.arrayContaining([
      expect.objectContaining({
        action: 'completed',
        actor: 'user',
      }),
      expect.objectContaining({
        action: 'reopened',
        actor: 'user',
      }),
    ]));
  });
});
