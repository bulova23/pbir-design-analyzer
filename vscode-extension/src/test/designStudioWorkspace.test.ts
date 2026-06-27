import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import { buildDesignStudioWorkspace } from '../design-studio/presentation/designStudioWorkspace';
import {
  createApprovedDraftMaterializationRequest,
  materializeDesignStudioRequest,
} from '../design-studio/materialization/materializationCoordinator';
import {
  approveConceptBaseline,
  generateConceptArtifacts,
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
  collectDraftArtifactVersionIds,
  generateDraftArtifacts,
  submitDraftForApproval,
} from '../design-studio/state/draftStore';
import {
  approveReviewCandidate,
  createReviewCandidate,
  submitReviewCandidateForApproval,
} from '../design-studio/state/prepareForReviewStore';
import {
  markReviewCompleted,
  markAnalyzerResultsAttached,
  recordAnalyzerResultsAvailable,
  recordReviewLaunch,
} from '../design-studio/state/reviewDesignStore';
import {
  attachAnalyzerResultsAtomically,
  attachAvailableAnalyzerResults,
  completeIteration,
  recordIteration,
  reopenIteration,
} from '../design-studio/state/iterationStore';
import type { DesignStudioAnalyzerResultReference } from '../design-studio/contracts/designStudioModels';
import { buildValidationApprovalEvidence } from '../design-studio/contracts/designStudioModels';

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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-design-studio-workspace-test-'));
}

function createThreadId(reportPath: string): string {
  return `design-studio:${crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16)}`;
}

function reviewDesignManifestPath(rootPath: string, threadId: string): string {
  const key = crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
  return path.join(rootPath, 'design-studio', 'threads', key, 'review-design.json');
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

async function createApprovedDraftWorkflow(context: ExtensionContext, threadId: string) {
  await saveDesignBriefDraft(context, threadId, {
    audience: 'Sales leaders',
    businessObjective: 'Reduce missed renewals',
    keyDecisions: ['Which regions need intervention first'],
    primaryKpis: ['Renewal rate', 'Gross margin', 'Forecast accuracy'],
    dimensions: ['Region', 'Segment'],
    intendedStory: 'Lead with risk, then explain the main drivers and actions.',
    successCriteria: ['Leader can pick the next intervention within five minutes'],
    reportType: 'dashboard',
    navigationExpectations: 'Executive Summary first, then Regional Analysis and Store Detail.',
    consumptionContext: 'Weekly renewal review',
    decisionCadence: 'Weekly',
    narrativeRisksOrConstraints: ['Avoid hiding segment outliers'],
    requiredEvidenceDomains: ['Renewal trend', 'pipeline coverage'],
    targetAnalyzableSurfaceFamily: 'pbir',
  });
  await submitDesignBriefForApproval(context, threadId);
  await approveDesignBrief(context, threadId);
  const conceptState = await generateConceptArtifacts(context, threadId);
  await selectConceptBaseline(context, threadId, conceptState.currentConcept.alternateConcepts[0].id);
  await submitConceptBaselineForApproval(context, threadId);
  await approveConceptBaseline(context, threadId);
  await generateDraftArtifacts(context, threadId);
  await submitDraftForApproval(context, threadId);
  return approveDraftArtifacts(context, threadId);
}

describe('designStudioWorkspace', () => {
  it('tracks Design Brief stage transitions and keeps Concept Studio blocked until approval', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Design Brief Workflow.Report.pbir';
    const threadId = createThreadId(reportPath);

    const notStarted = await buildDesignStudioWorkspace(context, reportPath);
    expect(notStarted.currentBrief).toBeUndefined();
    expect(notStarted.workspace.currentStage).toBe('brief');
    expect(notStarted.workspace.stages.find((stage) => stage.id === 'brief')?.status).toBe('notStarted');
    expect(notStarted.workspace.stages.find((stage) => stage.id === 'concept')?.status).toBe('blocked');

    await saveDesignBriefDraft(context, threadId, {
      audience: 'Sales leaders',
      businessObjective: '',
      keyDecisions: [],
      primaryKpis: [],
      dimensions: [],
      intendedStory: '',
      successCriteria: [],
      reportType: 'dashboard',
      navigationExpectations: '',
    });

    const inProgress = await buildDesignStudioWorkspace(context, reportPath);
    expect(inProgress.currentBrief?.approvalState).toBe('notSubmitted');
    expect(inProgress.workspace.currentStage).toBe('brief');
    expect(inProgress.workspace.stages.find((stage) => stage.id === 'brief')?.status).toBe('inProgress');
    expect(inProgress.workspace.stages.find((stage) => stage.id === 'brief')?.readinessLabel).toBe('In progress');
    expect(inProgress.workspace.stages.find((stage) => stage.id === 'concept')?.status).toBe('blocked');

    await saveDesignBriefDraft(context, threadId, {
      audience: 'Sales leaders',
      businessObjective: 'Reduce missed renewals',
      keyDecisions: ['Which regions need intervention first'],
      primaryKpis: ['Renewal rate'],
      dimensions: ['Region'],
      intendedStory: 'Lead with risk, then explain the main drivers and actions.',
      successCriteria: ['Leader can pick the next intervention within five minutes'],
      reportType: 'dashboard',
      navigationExpectations: 'Executive Summary first, then Regional Analysis.',
    });

    const ready = await buildDesignStudioWorkspace(context, reportPath);
    expect(ready.currentBrief?.approvalState).toBe('notSubmitted');
    expect(ready.workspace.currentStage).toBe('brief');
    expect(ready.workspace.stages.find((stage) => stage.id === 'brief')?.status).toBe('ready');
    expect(ready.workspace.stages.find((stage) => stage.id === 'brief')?.readinessLabel).toBe('Ready');
    expect(ready.workspace.stages.find((stage) => stage.id === 'concept')?.status).toBe('blocked');

    await submitDesignBriefForApproval(context, threadId);

    const pendingApproval = await buildDesignStudioWorkspace(context, reportPath);
    expect(pendingApproval.currentBrief?.approvalState).toBe('pendingApproval');
    expect(pendingApproval.workspace.stages.find((stage) => stage.id === 'brief')?.status).toBe('ready');
    expect(pendingApproval.workspace.stages.find((stage) => stage.id === 'concept')?.status).toBe('blocked');

    await approveDesignBrief(context, threadId);

    const approved = await buildDesignStudioWorkspace(context, reportPath);
    expect(approved.currentBrief?.approvalState).toBe('approved');
    expect(approved.workspace.stages.find((stage) => stage.id === 'brief')?.status).toBe('approved');
    expect(approved.workspace.stages.find((stage) => stage.id === 'concept')?.status).toBe('notStarted');
    expect(approved.workspace.currentStage).toBe('concept');
  });

  it('projects explicit Concept Studio workflow transitions and keeps Draft Studio blocked until concept approval', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Concept Workflow.Report.pbir';
    const threadId = createThreadId(reportPath);

    await saveDesignBriefDraft(context, threadId, {
      audience: 'Sales leaders',
      businessObjective: 'Reduce missed renewals',
      keyDecisions: ['Which regions need intervention first'],
      primaryKpis: ['Renewal rate'],
      dimensions: ['Region'],
      intendedStory: 'Lead with risk, then explain the main drivers and actions.',
      successCriteria: ['Leader can pick the next intervention within five minutes'],
      reportType: 'dashboard',
      navigationExpectations: 'Executive Summary first, then Regional Analysis.',
    });
    await submitDesignBriefForApproval(context, threadId);
    await approveDesignBrief(context, threadId);

    const unlocked = await buildDesignStudioWorkspace(context, reportPath);
    expect(unlocked.workspace.stages.find((stage) => stage.id === 'concept')?.status).toBe('notStarted');
    expect(unlocked.workspace.stages.find((stage) => stage.id === 'draft')?.status).toBe('blocked');

    const generated = await generateConceptArtifacts(context, threadId);
    const generatedWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(generated.currentConcept.approvalState).toBe('notSubmitted');
    expect(generatedWorkspace.workspace.stages.find((stage) => stage.id === 'concept')?.status).toBe('inProgress');
    expect(generatedWorkspace.workspace.stages.find((stage) => stage.id === 'draft')?.status).toBe('blocked');

    const selectedConceptId = generated.currentConcept.alternateConcepts[1].id;
    await selectConceptBaseline(context, threadId, selectedConceptId);
    const selectedWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(selectedWorkspace.workspace.stages.find((stage) => stage.id === 'concept')?.status).toBe('inProgress');
    expect(selectedWorkspace.workspace.stages.find((stage) => stage.id === 'draft')?.status).toBe('blocked');

    await submitConceptBaselineForApproval(context, threadId);
    const readyForApprovalWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(readyForApprovalWorkspace.workspace.stages.find((stage) => stage.id === 'concept')?.status).toBe('ready');
    expect(readyForApprovalWorkspace.workspace.stages.find((stage) => stage.id === 'draft')?.status).toBe('blocked');

    await approveConceptBaseline(context, threadId);
    const approvedWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(approvedWorkspace.workspace.stages.find((stage) => stage.id === 'concept')?.status).toBe('approved');
    expect(approvedWorkspace.workspace.stages.find((stage) => stage.id === 'draft')?.status).toBe('notStarted');
    expect(approvedWorkspace.workspace.currentStage).toBe('draft');
  });

  it('projects explicit Draft Studio workflow transitions and keeps Prepare For Review blocked until draft approval', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Draft Workflow.Report.pbir';
    const threadId = createThreadId(reportPath);

    await saveDesignBriefDraft(context, threadId, {
      audience: 'Sales leaders',
      businessObjective: 'Reduce missed renewals',
      keyDecisions: ['Which regions need intervention first'],
      primaryKpis: ['Renewal rate'],
      dimensions: ['Region'],
      intendedStory: 'Lead with risk, then explain the main drivers and actions.',
      successCriteria: ['Leader can pick the next intervention within five minutes'],
      reportType: 'dashboard',
      navigationExpectations: 'Executive Summary first, then Regional Analysis.',
    });
    await submitDesignBriefForApproval(context, threadId);
    await approveDesignBrief(context, threadId);
    const conceptState = await generateConceptArtifacts(context, threadId);
    await selectConceptBaseline(context, threadId, conceptState.currentConcept.alternateConcepts[1].id);
    await submitConceptBaselineForApproval(context, threadId);
    await approveConceptBaseline(context, threadId);

    const unlocked = await buildDesignStudioWorkspace(context, reportPath);
    expect(unlocked.workspace.stages.find((stage) => stage.id === 'draft')?.status).toBe('notStarted');
    expect(unlocked.workspace.stages.find((stage) => stage.id === 'materialize')?.status).toBe('blocked');

    const generated = await generateDraftArtifacts(context, threadId);
    const generatedWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(generated.currentDraft.approvalState).toBe('notSubmitted');
    expect(generatedWorkspace.workspace.stages.find((stage) => stage.id === 'draft')?.status).toBe('ready');
    expect(generatedWorkspace.workspace.stages.find((stage) => stage.id === 'materialize')?.status).toBe('blocked');

    await submitDraftForApproval(context, threadId);
    const readyForApprovalWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(readyForApprovalWorkspace.workspace.stages.find((stage) => stage.id === 'draft')?.status).toBe('ready');
    expect(readyForApprovalWorkspace.workspace.stages.find((stage) => stage.id === 'materialize')?.status).toBe('blocked');

    await approveDraftArtifacts(context, threadId);
    const approvedWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(approvedWorkspace.workspace.stages.find((stage) => stage.id === 'draft')?.status).toBe('approved');
    expect(approvedWorkspace.workspace.stages.find((stage) => stage.id === 'materialize')?.status).toBe('notStarted');
    expect(approvedWorkspace.workspace.stages.find((stage) => stage.id === 'materialize')?.readinessLabel).toBe('Not Started');
    expect(approvedWorkspace.workspace.stages.find((stage) => stage.id === 'handoff')?.status).toBe('blocked');
    expect(approvedWorkspace.workspace.currentStage).toBe('materialize');
  });

  it('executes Prepare For Review through candidate creation, approval, lineage rendering, and Review Design unlock', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Prepare For Review Workflow.Report.pbir';
    const threadId = createThreadId(reportPath);

    const approvedDraft = await createApprovedDraftWorkflow(context, threadId);
    const approvedDraftWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(approvedDraftWorkspace.workspace.materializationReadiness).toEqual(expect.objectContaining({
      nextStepGuidance: 'Create a review candidate from the approved draft.',
      canCreateCandidate: true,
      canSubmitCandidateForApproval: false,
      canApproveCandidate: false,
    }));
    expect(approvedDraftWorkspace.workspace.stages.find((stage) => stage.id === 'materialize')?.status).toBe('notStarted');
    expect(approvedDraftWorkspace.workspace.stages.find((stage) => stage.id === 'handoff')?.status).toBe('blocked');

    const created = await createReviewCandidate(context, { threadId, reportPath });
    expect(created.currentCandidate.approvalState).toBe('notSubmitted');
    expect(created.currentCandidate.sourceLineage).toEqual(expect.arrayContaining([
      expect.objectContaining({
        artifactId: approvedDraft.currentDraft.id,
        artifactVersionId: `${approvedDraft.currentDraft.id}@v${approvedDraft.currentDraft.version}`,
        approvalState: 'approved',
      }),
    ]));
    expect(created.currentCandidate.materializationDiagnostics).toEqual(expect.arrayContaining([
      'No analyzer handoff was executed.',
      'No analyzer workspace was opened.',
      'No report mutation occurred.',
    ]));

    const createdWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(createdWorkspace.workspace.stages.find((stage) => stage.id === 'materialize')?.status).toBe('inProgress');
    expect(createdWorkspace.workspace.stages.find((stage) => stage.id === 'materialize')?.readinessLabel).toBe('Candidate Created');
    expect(createdWorkspace.workspace.stages.find((stage) => stage.id === 'handoff')?.status).toBe('blocked');
    expect(createdWorkspace.workspace.materializationReadiness).toEqual(expect.objectContaining({
      candidateStatusLabel: 'Candidate Created',
      sourceDraftVersionId: `${approvedDraft.currentDraft.id}@v${approvedDraft.currentDraft.version}`,
      sourceConceptVersionId: approvedDraft.currentDraft.sourceConceptVersionId,
      sourceDesignBriefVersionId: approvedDraft.currentDraft.sourceBriefVersionId,
      canCreateCandidate: false,
      canSubmitCandidateForApproval: true,
      canApproveCandidate: false,
      nextStepGuidance: 'Review readiness diagnostics before approval.',
      lineage: expect.arrayContaining([
        expect.objectContaining({
          label: 'Source draft',
          artifactVersionId: `${approvedDraft.currentDraft.id}@v${approvedDraft.currentDraft.version}`,
          approvalState: 'approved',
        }),
      ]),
    }));

    const submitted = await submitReviewCandidateForApproval(context, threadId);
    expect(submitted.currentCandidate.approvalState).toBe('pendingApproval');
    expect(submitted.currentCandidate.version).toBe(created.currentCandidate.version + 1);
    expect(submitted.currentCandidate.sourceLineage).toEqual(created.currentCandidate.sourceLineage);

    const readyWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(readyWorkspace.workspace.stages.find((stage) => stage.id === 'materialize')?.status).toBe('ready');
    expect(readyWorkspace.workspace.stages.find((stage) => stage.id === 'materialize')?.readinessLabel).toBe('Ready For Approval');
    expect(readyWorkspace.workspace.stages.find((stage) => stage.id === 'handoff')?.status).toBe('blocked');
    expect(readyWorkspace.workspace.materializationReadiness).toEqual(expect.objectContaining({
      candidateStatusLabel: 'Ready For Approval',
      canSubmitCandidateForApproval: false,
      canApproveCandidate: true,
      nextStepGuidance: 'Approve the review candidate to unlock Review Design.',
    }));

    const approvedCandidate = await approveReviewCandidate(context, threadId);
    expect(approvedCandidate.currentCandidate.approvalState).toBe('approved');
    expect(approvedCandidate.currentCandidate.version).toBe(submitted.currentCandidate.version + 1);
    expect(approvedCandidate.currentCandidate.sourceLineage).toEqual(submitted.currentCandidate.sourceLineage);

    const approvedCandidateWorkspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(approvedCandidateWorkspace.workspace.stages.find((stage) => stage.id === 'materialize')?.status).toBe('approved');
    expect(approvedCandidateWorkspace.workspace.stages.find((stage) => stage.id === 'handoff')?.status).toBe('ready');
    expect(approvedCandidateWorkspace.workspace.currentStage).toBe('handoff');
    expect(approvedCandidateWorkspace.workspace.materializationReadiness).toEqual(expect.objectContaining({
      candidateStatusLabel: 'Approved',
      nextStepGuidance: 'Review candidate approved. Continue to Review Design.',
    }));
  });

  it('keeps internal workflow ids stable while exposing consultant-facing language and review artifacts', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Sales & Production.Report.pbir';
    const threadId = createThreadId(reportPath);
    const draftState = await createApprovedDraftWorkflow(context, threadId);
    const sourceArtifactVersionIds = collectDraftArtifactVersionIds(draftState);
    const request = await createApprovedDraftMaterializationRequest(context, {
      threadId,
      requestId: `materialization-request:${threadId}`,
      targetSurfaceType: 'pbirReport',
      targetAnalyzer: 'pbirDesignReview',
      targetAnalyzerProfile: 'default',
      handoffContext: {
        repositoryBackedPath: reportPath,
        degradedMappings: [],
        omittedEvidence: [],
      },
    });
    const materialization = materializeDesignStudioRequest(request);
    if (!materialization.ok) {
      throw new Error(materialization.diagnostics.join('\n'));
    }
    const candidate = materialization.candidate;

    await recordIteration(context, {
      threadId,
      sourceArtifactVersionIds,
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: candidate,
      analyzerOutputs: [
        {
          analyzerSource: 'guidedStoryImprovements',
          analyzerRunId: 'run-1',
          resultReference: 'issues:1',
          reportPath,
          scoredAt: '2026-06-14T15:00:00.000Z',
          sourceArtifactVersionIds,
          sourceCandidateId: candidate.id,
          sourceArtifactVersionFingerprint: sourceArtifactVersionIds,
          payload: {
            summary: 'Validation accepted the design changes.',
          },
        },
      ],
      refinementProposals: [],
      validationApproval: {
        approvalState: 'approved',
        provenance: { source: 'analyzerWorkspace' },
        validationLinkage: buildValidationApprovalEvidence({
          analyzerRunId: 'run-1',
          resultReference: 'issues:1',
          sourceCandidateId: candidate.id,
          sourceArtifactVersionFingerprint: sourceArtifactVersionIds,
          validationResultStatus: 'validated',
        }),
      },
    });

    const result = await buildDesignStudioWorkspace(context, reportPath);
    const stageLabels = Object.fromEntries(result.workspace.stages.map((stage) => [stage.id, stage.label]));

    expect(result.threadId).toBe(threadId);
    expect(result.workspace.stages.map((stage) => stage.id)).toEqual([
      'brief',
      'concept',
      'draft',
      'refinement',
      'materialize',
      'previewReview',
      'handoff',
      'compare',
      'completion',
    ]);
    expect(stageLabels.materialize).toBe('Prepare For Review');
    expect(stageLabels.previewReview).toBe('Preview Review');
    expect(stageLabels.handoff).toBe('Review Design');
    expect(result.workspace.approvalCards.find((card) => card.kind === 'validationApproval')).toEqual(
      expect.objectContaining({
        title: 'Validation Approval',
        approvalState: 'approved',
      }),
    );
    expect(result.workspace.conceptReview).toEqual(expect.objectContaining({
      title: 'Concept Review Artifacts',
      selectedConceptLabel: expect.any(String),
      chapterStructure: expect.arrayContaining([
        expect.objectContaining({
          title: expect.any(String),
          objective: expect.any(String),
        }),
      ]),
      kpiHierarchy: expect.arrayContaining([
        expect.objectContaining({
          label: expect.stringMatching(/Renewal rate|Gross margin|Forecast accuracy/i),
        }),
      ]),
      navigationStructure: expect.arrayContaining([
        expect.objectContaining({
          label: expect.stringMatching(/Priorities|Drivers/),
        }),
      ]),
      analyticalFlow: expect.arrayContaining([
        expect.objectContaining({
          label: expect.stringMatching(/Spot the risk|Localize the issue|Explain the cause/),
          objective: expect.any(String),
        }),
      ]),
      comparisons: expect.arrayContaining([
        expect.objectContaining({
          comparisonConceptLabel: expect.any(String),
          chapterStructure: expect.objectContaining({
            baselineItems: expect.any(Array),
            comparisonItems: expect.any(Array),
          }),
          kpiHierarchy: expect.objectContaining({
            baselineItems: expect.any(Array),
            comparisonItems: expect.any(Array),
          }),
          navigationStructure: expect.objectContaining({
            baselineItems: expect.any(Array),
            comparisonItems: expect.any(Array),
          }),
          analyticalFlow: expect.objectContaining({
            baselineItems: expect.any(Array),
            comparisonItems: expect.any(Array),
          }),
        }),
      ]),
    }));
    expect(result.workspace.draftReview).toEqual(expect.objectContaining({
      title: expect.any(String),
      summary: expect.any(String),
      draftPages: expect.arrayContaining([
        expect.objectContaining({
          title: expect.any(String),
          structureSummary: expect.any(String),
          kpiPlacement: expect.any(Array),
        }),
      ]),
      draftLayouts: expect.arrayContaining([
        expect.objectContaining({
          title: expect.any(String),
          layoutType: expect.any(String),
        }),
      ]),
      draftNavigation: expect.arrayContaining([
        expect.objectContaining({
          label: expect.any(String),
        }),
      ]),
    }));
  });

  it('tracks analyzer return-loop states, keeps validation analyzer-owned, and unlocks Refinement Studio only after explicit result attachment', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Review Design Workflow.Report.pbir';
    const threadId = createThreadId(reportPath);

    const draftState = await createApprovedDraftWorkflow(context, threadId);
    await createReviewCandidate(context, { threadId, reportPath });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedCandidate = await approveReviewCandidate(context, threadId);

    const beforeLaunch = await buildDesignStudioWorkspace(context, reportPath);
    expect(beforeLaunch.workspace.stages.find((stage) => stage.id === 'handoff')?.status).toBe('ready');
    expect(beforeLaunch.workspace.stages.find((stage) => stage.id === 'handoff')?.readinessLabel).toBe('Review Not Started');
    expect(beforeLaunch.workspace.stages.find((stage) => stage.id === 'refinement')?.status).toBe('blocked');
    expect(beforeLaunch.workspace.reviewDesign).toEqual(expect.objectContaining({
      reviewReadinessLabel: 'Ready for review',
      reviewStatusLabel: 'Review Not Started',
      completionStatusLabel: 'Review not completed',
      canOpenAnalyzerWorkspace: true,
      canMarkReviewCompleted: false,
      canAttachAnalyzerResults: false,
      approvedReviewCandidateVersionId: `${approvedCandidate.currentCandidate.id}@v${approvedCandidate.currentCandidate.version}`,
      nextStepGuidance: 'Open Analyzer Workspace to review the design.',
      ownershipMessages: expect.arrayContaining([
        'Analyzer Workspace owns validation.',
        'Design Studio does not validate itself.',
        'Review Design launches review only.',
      ]),
    }));
    expect(beforeLaunch.workspace.approvalCards.find((card) => card.kind === 'validationApproval')?.approvalState).toBe('notSubmitted');

    await recordReviewLaunch(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      analyzerId: approvedCandidate.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedCandidate.currentRequest.targetAnalyzerProfile,
    });

    const launched = await buildDesignStudioWorkspace(context, reportPath);
    expect(launched.workspace.stages.find((stage) => stage.id === 'handoff')?.status).toBe('inProgress');
    expect(launched.workspace.stages.find((stage) => stage.id === 'handoff')?.readinessLabel).toBe('Review Launched');
    expect(launched.workspace.stages.find((stage) => stage.id === 'refinement')?.status).toBe('blocked');
    expect(launched.workspace.currentStage).toBe('handoff');
    expect(launched.workspace.reviewDesign).toEqual(expect.objectContaining({
      reviewReadinessLabel: 'Ready for review',
      reviewStatusLabel: 'Review Launched',
      completionStatusLabel: 'Review not completed',
      handoffStatusLabel: 'Analyzer Workspace opened',
      canOpenAnalyzerWorkspace: true,
      canMarkReviewCompleted: true,
      canAttachAnalyzerResults: false,
      nextStepGuidance: 'Complete review in Analyzer Workspace and return here.',
    }));
    expect(launched.workspace.approvalCards.find((card) => card.kind === 'validationApproval')?.approvalState).toBe('notSubmitted');

    await markReviewCompleted(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });

    const completed = await buildDesignStudioWorkspace(context, reportPath);
    expect(completed.workspace.stages.find((stage) => stage.id === 'handoff')?.status).toBe('inProgress');
    expect(completed.workspace.stages.find((stage) => stage.id === 'handoff')?.readinessLabel).toBe('Awaiting Analyzer Results');
    expect(completed.workspace.stages.find((stage) => stage.id === 'refinement')?.status).toBe('blocked');
    expect(completed.workspace.currentStage).toBe('handoff');
    expect(completed.workspace.reviewDesign).toEqual(expect.objectContaining({
      reviewStatusLabel: 'Awaiting Analyzer Results',
      completionStatusLabel: 'Review completed',
      canOpenAnalyzerWorkspace: true,
      canMarkReviewCompleted: false,
      canAttachAnalyzerResults: false,
      nextStepGuidance: 'Review completed, but no analyzer results are attached yet.',
    }));
    expect(completed.workspace.approvalCards.find((card) => card.kind === 'validationApproval')?.approvalState).toBe('notSubmitted');

    await recordAnalyzerResultsAvailable(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: 'issues',
          analyzerRunId: 'run-review-return-1',
          resultReference: 'issues:return-1',
          scoredAt: '2026-06-16T18:00:00.000Z',
          sourceCandidateId: approvedCandidate.currentCandidate.id,
          sourceArtifactVersionFingerprint: collectDraftArtifactVersionIds(draftState),
          validationResultStatus: 'validated',
          validationApprovalState: 'approved',
          findingReferenceIds: ['finding-review-return-1'],
          recommendationReferenceIds: [],
          linkedProposalIds: [],
        }),
      ],
    });

    const available = await buildDesignStudioWorkspace(context, reportPath);
    expect(available.workspace.stages.find((stage) => stage.id === 'handoff')?.status).toBe('ready');
    expect(available.workspace.stages.find((stage) => stage.id === 'handoff')?.readinessLabel).toBe('Analyzer Results Available');
    expect(available.workspace.reviewDesign).toEqual(expect.objectContaining({
      reviewStatusLabel: 'Analyzer Results Available',
      canAttachAnalyzerResults: true,
      nextStepGuidance: 'Attach analyzer results to continue refinement.',
    }));
  });

  it('shows explicit completion readiness, checklist, outstanding items, and reopen guidance without collapsing validation ownership', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Workflow Completion.Report.pbir';
    const threadId = createThreadId(reportPath);

    const draftState = await createApprovedDraftWorkflow(context, threadId);
    await createReviewCandidate(context, { threadId, reportPath });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedCandidate = await approveReviewCandidate(context, threadId);
    await recordReviewLaunch(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      analyzerId: approvedCandidate.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedCandidate.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });
    await recordAnalyzerResultsAvailable(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: 'issues',
          analyzerRunId: 'run-completion-1',
          resultReference: 'issues:completion-1',
          scoredAt: '2026-06-16T18:15:00.000Z',
          sourceCandidateId: approvedCandidate.currentCandidate.id,
          sourceArtifactVersionFingerprint: collectDraftArtifactVersionIds(draftState),
          validationResultStatus: 'needsReview',
          validationApprovalState: 'notSubmitted',
          findingReferenceIds: ['finding-completion-1'],
          recommendationReferenceIds: [],
          linkedProposalIds: [],
        }),
      ],
    });
    await markAnalyzerResultsAttached(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });
    await attachAvailableAnalyzerResults(context, threadId);

    const ready = await buildDesignStudioWorkspace(context, reportPath);
    expect(ready.workspace.stages.map((stage) => stage.id)).toContain('completion');
    expect(ready.workspace.stages.find((stage) => stage.id === 'completion')).toEqual(expect.objectContaining({
      status: 'ready',
      readinessLabel: 'Ready For Completion',
    }));
    expect(ready.workspace.workflowCompletion).toEqual(expect.objectContaining({
      state: 'readyForCompletion',
      checklist: expect.arrayContaining([
        expect.objectContaining({ label: 'Design Brief approved', satisfied: true, required: true }),
        expect.objectContaining({ label: 'Concept approved', satisfied: true, required: true }),
        expect.objectContaining({ label: 'Draft approved', satisfied: true, required: true }),
        expect.objectContaining({ label: 'Review candidate approved', satisfied: true, required: true }),
        expect.objectContaining({ label: 'Review Design completed', satisfied: true, required: true }),
        expect.objectContaining({ label: 'Analyzer results attached', satisfied: true, required: true }),
        expect.objectContaining({ label: 'Refinement reviewed', satisfied: true, required: true }),
        expect.objectContaining({ label: 'Validation approval status recorded', satisfied: false, required: false }),
      ]),
      nextStepGuidance: 'This iteration is ready for completion.',
      canCompleteIteration: true,
      canReopenIteration: false,
    }));
    expect(ready.workspace.approvalCards.find((card) => card.kind === 'validationApproval')?.approvalState).toBe('notSubmitted');

    await completeIteration(context, threadId);

    const completed = await buildDesignStudioWorkspace(context, reportPath);
    expect(completed.workspace.stages.find((stage) => stage.id === 'completion')).toEqual(expect.objectContaining({
      status: 'approved',
      readinessLabel: 'Completed',
    }));
    expect(completed.workspace.workflowCompletion).toEqual(expect.objectContaining({
      state: 'completed',
      canCompleteIteration: false,
      canReopenIteration: true,
      completedBy: 'user',
      completedAt: expect.any(String),
      nextStepGuidance: 'Iteration completed. You may reopen if additional refinement is required.',
    }));

    await reopenIteration(context, threadId);

    const reopened = await buildDesignStudioWorkspace(context, reportPath);
    expect(reopened.workspace.stages.find((stage) => stage.id === 'completion')).toEqual(expect.objectContaining({
      status: 'inProgress',
      readinessLabel: 'Reopened',
    }));
    expect(reopened.workspace.workflowCompletion).toEqual(expect.objectContaining({
      state: 'reopened',
      canCompleteIteration: true,
      canReopenIteration: false,
      reopenedBy: 'user',
      reopenedAt: expect.any(String),
    }));
  });

  it('keeps refinement blocked and preserves review attachment state when atomic attachment fails', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Atomic Failure.Report.pbir';
    const threadId = createThreadId(reportPath);

    const draftState = await createApprovedDraftWorkflow(context, threadId);
    await createReviewCandidate(context, { threadId, reportPath });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedCandidate = await approveReviewCandidate(context, threadId);
    await recordReviewLaunch(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      analyzerId: approvedCandidate.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedCandidate.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });
    await recordAnalyzerResultsAvailable(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: 'issues',
          analyzerRunId: 'run-atomic-failure-1',
          resultReference: 'issues:atomic-failure-1',
          scoredAt: '2026-06-17T10:30:00.000Z',
          sourceCandidateId: approvedCandidate.currentCandidate.id,
          sourceArtifactVersionFingerprint: collectDraftArtifactVersionIds(draftState),
          validationResultStatus: 'needsReview',
          validationApprovalState: 'notSubmitted',
          findingReferenceIds: ['finding-atomic-failure-1'],
          recommendationReferenceIds: [],
          linkedProposalIds: [],
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

    const failed = await attachAnalyzerResultsAtomically(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });

    expect(failed).toEqual(expect.objectContaining({
      ok: false,
      error: expect.stringContaining('source candidate lineage'),
    }));

    const workspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(workspace.workspace.stages.find((stage) => stage.id === 'handoff')).toEqual(expect.objectContaining({
      status: 'ready',
      readinessLabel: 'Analyzer Results Available',
    }));
    expect(workspace.workspace.stages.find((stage) => stage.id === 'refinement')).toEqual(expect.objectContaining({
      status: 'blocked',
      readinessLabel: 'Blocked',
    }));
    expect(workspace.workspace.reviewDesign).toEqual(expect.objectContaining({
      reviewStatusLabel: 'Analyzer Results Available',
      resultStatusLabel: 'Validation approval not attached yet',
      nextStepGuidance: 'Attach analyzer results to continue refinement.',
    }));
  });

  it('keeps attached results distinct from validated state until analyzer-owned validation approval exists', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Validation Pending.Report.pbir';
    const threadId = createThreadId(reportPath);

    const draftState = await createApprovedDraftWorkflow(context, threadId);
    await createReviewCandidate(context, { threadId, reportPath });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedCandidate = await approveReviewCandidate(context, threadId);
    await recordIteration(context, {
      threadId,
      sourceArtifactVersionIds: collectDraftArtifactVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: approvedCandidate.currentCandidate,
      analyzerOutputs: [],
      refinementProposals: [],
    });
    await recordReviewLaunch(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      analyzerId: approvedCandidate.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedCandidate.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });
    await recordAnalyzerResultsAvailable(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: 'issues',
          analyzerRunId: 'run-validation-pending-1',
          resultReference: 'issues:validation-pending-1',
          scoredAt: '2026-06-17T11:00:00.000Z',
          sourceCandidateId: approvedCandidate.currentCandidate.id,
          sourceArtifactVersionFingerprint: collectDraftArtifactVersionIds(draftState),
          validationResultStatus: 'validated',
          validationApprovalState: 'notSubmitted',
          findingReferenceIds: ['finding-validation-pending-1'],
          recommendationReferenceIds: [],
          linkedProposalIds: [],
        }),
      ],
    });

    const attached = await attachAnalyzerResultsAtomically(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });

    expect(attached.ok).toBe(true);

    const workspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(workspace.workspace.reviewDesign).toEqual(expect.objectContaining({
      reviewStatusLabel: 'Results Attached',
      resultStatusLabel: 'Analyzer results are attached to this iteration.',
    }));
    expect(workspace.workspace.approvalCards.find((card) => card.kind === 'validationApproval')).toEqual(expect.objectContaining({
      approvalState: 'notSubmitted',
    }));
    expect(workspace.workspace.stages.find((stage) => stage.id === 'compare')).toEqual(expect.objectContaining({
      status: 'ready',
      readinessLabel: 'Ready',
    }));
    expect(workspace.workspace.workflowCompletion!.checklist).toEqual(expect.arrayContaining([
      expect.objectContaining({ label: 'Review Design completed', satisfied: true }),
      expect.objectContaining({ label: 'Analyzer results attached', satisfied: true }),
      expect.objectContaining({ label: 'Validation approval status recorded', satisfied: false }),
    ]));
  });

  it('shows validated only when analyzer-owned validation approval exists and keeps completion consistent', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Validated State.Report.pbir';
    const threadId = createThreadId(reportPath);

    const draftState = await createApprovedDraftWorkflow(context, threadId);
    await createReviewCandidate(context, { threadId, reportPath });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedCandidate = await approveReviewCandidate(context, threadId);
    await recordIteration(context, {
      threadId,
      sourceArtifactVersionIds: collectDraftArtifactVersionIds(draftState),
      concept: draftState.concept,
      draft: draftState.currentDraft,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      materializedCandidate: approvedCandidate.currentCandidate,
      analyzerOutputs: [],
      refinementProposals: [],
    });
    await recordReviewLaunch(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      analyzerId: approvedCandidate.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedCandidate.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });
    await recordAnalyzerResultsAvailable(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
      results: [
        makeAnalyzerResultReference({
          analyzerSource: 'issues',
          analyzerRunId: 'run-validated-state-1',
          resultReference: 'issues:validated-state-1',
          scoredAt: '2026-06-17T11:15:00.000Z',
          sourceCandidateId: approvedCandidate.currentCandidate.id,
          sourceArtifactVersionFingerprint: collectDraftArtifactVersionIds(draftState),
          validationResultStatus: 'validated',
          validationApprovalState: 'approved',
          findingReferenceIds: ['finding-validated-state-1'],
          recommendationReferenceIds: [],
          linkedProposalIds: [],
        }),
      ],
    });

    const attached = await attachAnalyzerResultsAtomically(context, threadId, {
      requestId: approvedCandidate.currentRequest.id,
      candidate: approvedCandidate.currentCandidate,
    });

    expect(attached.ok).toBe(true);

    const workspace = await buildDesignStudioWorkspace(context, reportPath);
    expect(workspace.workspace.approvalCards.find((card) => card.kind === 'validationApproval')).toEqual(expect.objectContaining({
      approvalState: 'approved',
    }));
    expect(workspace.workspace.stages.find((stage) => stage.id === 'compare')).toEqual(expect.objectContaining({
      status: 'approved',
      readinessLabel: 'Validated',
    }));
    expect(workspace.workspace.workflowCompletion!.checklist).toEqual(expect.arrayContaining([
      expect.objectContaining({ label: 'Validation approval status recorded', satisfied: true }),
    ]));
  });
});
