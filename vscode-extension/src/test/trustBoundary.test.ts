import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import type {
  DesignArtifactValidationLinkage,
  MaterializationRequest,
  MaterializedSurfaceCandidate,
  RefinementProposal,
} from '../design-studio/contracts/designStudioModels';
import { buildValidationApprovalEvidence } from '../design-studio/contracts/designStudioModels';
import { AnalyzerHandoffService } from '../design-studio/materialization/analyzerHandoffService';
import {
  createApprovedDraftMaterializationRequest,
  materializeDesignStudioRequest,
} from '../design-studio/materialization/materializationCoordinator';
import { DesignProviderRegistry, createDesignProviderCapability } from '../design-studio/providers/designProviderRegistry';
import {
  approveConceptBaseline,
  generateConceptArtifacts,
} from '../design-studio/state/conceptStore';
import {
  approveDesignBrief,
  saveDesignBriefDraft,
} from '../design-studio/state/designBriefStore';
import {
  approveDraftArtifacts,
  generateDraftArtifacts,
  type DraftState,
} from '../design-studio/state/draftStore';
import {
  recordIteration,
} from '../design-studio/state/iterationStore';
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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-trust-boundary-test-'));
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
  await approveDesignBrief(context, threadId);
  await generateConceptArtifacts(context, threadId);
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
  requestId = 'trust-boundary',
  overrides?: Partial<MaterializationRequest>,
): Promise<MaterializedSurfaceCandidate> {
  const request = {
    ...(await createApprovedDraftMaterializationRequest(context, {
      threadId: state.threadId,
      requestId: `materialization-request:${state.threadId}:${requestId}`,
      targetSurfaceType: 'pbirReport',
      targetAnalyzer: 'pbirDesignReview',
      targetAnalyzerProfile: 'default',
      handoffContext: overrides?.handoffContext,
    })),
    ...overrides,
  };
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
  resultReference = 'issues:trust-boundary',
): Promise<RefinementProposal[]> {
  const pageName = state.concept.pageConcepts[0]?.title ?? 'Executive overview';
  const created = await ingestIssues(context, threadId, {
    analyzerRunId: 'run-trust-boundary-1',
    resultReference,
    sourceArtifactVersionIds: sourceVersionIds(state),
    reportPath: '/tmp/sales.pbir',
    scoredAt: '2026-06-13T10:01:00.000Z',
    issues: [
      {
        id: 'finding-trust-boundary-1',
        title: 'Navigation drift',
        summary: 'Users do not move cleanly from summary to detail.',
        severity: 'high',
        confidence: 0.91,
        scope: 'crossPage',
        detectionType: 'deterministic',
        affectedPages: [pageName],
        impactArea: 'navigation',
        frameworkImpact: ['narrative'],
        recommendation: 'Reduce the number of branches and clarify the path.',
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

function attachAnalyzerCandidateLineage(
  proposals: RefinementProposal[],
  candidate: MaterializedSurfaceCandidate,
  artifactVersionFingerprint: string[],
): RefinementProposal[] {
  return proposals.map((proposal) => ({
    ...JSON.parse(JSON.stringify(proposal)) as RefinementProposal,
    sourceAnalyzerOutput: {
      ...proposal.sourceAnalyzerOutput,
      sourceCandidateId: candidate.id,
      sourceArtifactVersionFingerprint: [...artifactVersionFingerprint],
    },
  }));
}

describe('designStudio trust boundary guardrails', () => {
  it('keeps workflow stages gated and analyzer-owned', async () => {
    const context = makeContext(makeTempDir());

    await expect(generateConceptArtifacts(context, 'thread-workflow-guardrails')).rejects.toThrow(
      'Concept generation requires an approved Design Brief.',
    );

    await saveDesignBriefDraft(context, 'thread-workflow-guardrails', {
      audience: 'Sales leaders',
      businessObjective: 'Reduce missed renewals',
      keyDecisions: ['Which regions need intervention first'],
      primaryKpis: ['Renewal rate'],
      dimensions: ['Region'],
      intendedStory: 'Lead with risk, then explain the main drivers and next steps.',
      successCriteria: ['Leader can decide the next action within five minutes'],
      reportType: 'dashboard',
      navigationExpectations: 'Overview first, detail second.',
    });
    await approveDesignBrief(context, 'thread-workflow-guardrails');
    await generateConceptArtifacts(context, 'thread-workflow-guardrails');

    await expect(generateDraftArtifacts(context, 'thread-workflow-guardrails')).rejects.toThrow(
      'Draft generation requires an approved Concept baseline.',
    );

    await approveConceptBaseline(context, 'thread-workflow-guardrails');
    const pendingDraft = await generateDraftArtifacts(context, 'thread-workflow-guardrails');

    await expect(createApprovedDraftMaterializationRequest(context, {
      threadId: 'thread-workflow-guardrails',
      requestId: 'materialization-request:thread-workflow-guardrails:pending',
      targetSurfaceType: 'pbirReport',
      targetAnalyzer: 'pbirDesignReview',
      targetAnalyzerProfile: 'default',
    })).rejects.toThrow('Draft-to-surface materialization requires an approved draft version.');

    const openAnalyzerWorkspace = jest.fn<Promise<void>, [unknown]>().mockResolvedValue(undefined);
    const handoffService = new AnalyzerHandoffService({ openAnalyzerWorkspace });
    const approvedDraft = await approveDraftArtifacts(context, 'thread-workflow-guardrails');
    const previewCandidate = await buildCandidate(context, approvedDraft);
    const previewResult = await handoffService.handoffCandidate(previewCandidate);

    expect(previewResult.ok).toBe(false);
    expect(previewResult.eligibility).toBe('nonExecutablePreview');
    expect(openAnalyzerWorkspace).not.toHaveBeenCalled();

    await expect(recordIteration(context, {
      threadId: 'thread-workflow-guardrails',
      sourceArtifactVersionIds: sourceVersionIds(approvedDraft),
      analyzerOutputs: [],
      refinementProposals: [],
      validationApproval: {
        approvalState: 'approved',
        provenance: { source: 'analyzerWorkspace' },
      },
    })).rejects.toThrow('Validation approval requires analyzer-owned provenance.');
  });

  it('keeps approval stages separate and requires analyzer-owned validation evidence', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-approval-guardrails');
    const draftState = await approveDraftArtifacts(context, 'thread-approval-guardrails');
    const candidate = await buildCandidate(context, draftState, 'approval');
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, 'thread-approval-guardrails'),
      candidate,
      sourceVersionIds(draftState),
    );

    const invalidValidation = buildValidationApprovalEvidence({
      analyzerRunId: 'run-trust-boundary-1',
      resultReference: 'issues:trust-boundary',
      sourceCandidateId: candidate.id,
      sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
      validationResultStatus: 'validated',
    });

    await expect(recordIteration(context, {
      threadId: 'thread-approval-guardrails',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
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
        provenance: { source: 'system' },
        validationLinkage: invalidValidation,
      },
    })).rejects.toThrow('Validation approval requires analyzer-owned provenance.');
  });

  it('keeps providers optional, non-authoritative, and unable to create analyzable surfaces directly', async () => {
    const registry = new DesignProviderRegistry();

    expect(registry.listProviders()).toEqual([]);
    expect(registry.listCapabilities()).toEqual([]);

    const capability = createDesignProviderCapability({
      providerId: 'provider.mock',
      providerDisplayName: 'Mock provider',
      capabilityId: 'draft-layouts',
      capabilityKind: 'generationAssistance',
      supportedArtifactKinds: ['draftLayoutArtifact'],
      supportedSurfaceFamilies: ['pbir'],
      requiresExternalService: true,
      supportsOfflineOperation: false,
      trustPosture: 'advisoryOnly',
      provenanceRequirements: 'required',
      failureBehavior: 'degradeGracefully',
    });

    expect(capability.workflowConstraints.requiresApproval).toBe(true);
    expect(capability.workflowConstraints.requiresValidation).toBe(true);
    expect(capability.workflowConstraints.allowsAnalyzableSurfaceCreation).toBe(false);
    expect(capability.workflowConstraints.allowsMaterialization).toBe(false);
    expect(capability.workflowConstraints.allowsReportMutation).toBe(false);
    expect(capability.workflowConstraints.allowsPbirAssetGeneration).toBe(false);
  });

  it('keeps materialization explicit, candidate-only, diagnostic, and non-mutating', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-materialization-guardrails');
    const draftState = await approveDraftArtifacts(context, 'thread-materialization-guardrails');
    const candidate = await buildCandidate(context, draftState, 'materialization');

    expect(candidate.kind).toBe('materializedSurfaceCandidate');
    expect(candidate.derivedSurface.sourceLocation).toContain('design-studio://materialization/');
    expect(candidate.materializationDiagnostics).toEqual(expect.arrayContaining([
      'Draft-to-surface candidate materialization produced candidate metadata only.',
      'No PBIR files were created.',
      'No analyzer handoff was executed.',
      'No analyzer workspace was opened.',
      'No report mutation occurred.',
    ]));

    const materialization = materializeDesignStudioRequest({
      ...(await createApprovedDraftMaterializationRequest(context, {
        threadId: 'thread-materialization-guardrails',
        requestId: 'materialization-request:thread-materialization-guardrails:no-diagnostics',
        targetSurfaceType: 'pbirReport',
        targetAnalyzer: 'pbirDesignReview',
        targetAnalyzerProfile: 'default',
      })),
    });

    expect(materialization.sideEffects).toEqual({
      analyzerHandoffExecuted: false,
      analyzerWorkspaceOpened: false,
      pbirFilesCreated: false,
      reportMutationOccurred: false,
      deliveryTriggered: false,
      providerExecutionTriggered: false,
    });
  });

  it('keeps closed-loop iterations explicit and non-automating', async () => {
    const context = makeContext(makeTempDir());
    await createDraftState(context, 'thread-regression-guardrails');
    const draftState = await approveDraftArtifacts(context, 'thread-regression-guardrails');
    const candidate = await buildCandidate(context, draftState, 'closed-loop');
    const proposals = attachAnalyzerCandidateLineage(
      await createApprovedProposals(context, draftState, 'thread-regression-guardrails'),
      candidate,
      sourceVersionIds(draftState),
    );
    const validationApproval: DesignArtifactValidationLinkage = buildValidationApprovalEvidence({
      analyzerRunId: 'run-trust-boundary-1',
      resultReference: 'issues:trust-boundary',
      sourceCandidateId: candidate.id,
      sourceArtifactVersionFingerprint: sourceVersionIds(draftState),
      validationResultStatus: 'validated',
    });

    const state = await recordIteration(context, {
      threadId: 'thread-regression-guardrails',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
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
        validationLinkage: validationApproval,
      },
    });

    expect(state.iterations[0]?.approvalCheckpoint.designApproval.approvalState).toBe('approved');
    expect(state.iterations[0]?.approvalCheckpoint.materializationApproval.approvalState).toBe('approved');
    expect(state.iterations[0]?.approvalCheckpoint.refinementApproval.approvalState).toBe('approved');
    expect(state.iterations[0]?.approvalCheckpoint.validationApproval.owner).toBe('analyzerWorkspace');
    expect(state.iterations[0]?.guardrails).toEqual({
      autoOptimizationTriggered: false,
      analyzerExecutionTriggered: false,
      reportMutationTriggered: false,
      pbirFilesGenerated: false,
    });
  });
});
