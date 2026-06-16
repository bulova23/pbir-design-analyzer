import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import type {
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
  loadIterationState,
  recordIteration,
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
  type DraftState,
} from '../design-studio/state/draftStore';
import {
  approveRefinementProposal,
  ingestIssues,
} from '../design-studio/state/refinementStore';

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
    expect(pendingDraft.history[0]?.draft.approvalState).toBe('pendingApproval');

    const reloaded = await loadIterationState(context, 'thread-closed-loop-lineage');
    expect(reloaded).toEqual(created);
  });

  it('compares before and after concept, draft, analyzer, recommendation, and validation changes', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-closed-loop-compare');
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

  it('rejects source artifact version ids that do not exist in persisted draft state', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-closed-loop-source-validation');
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
});
