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
} from '../design-studio/state/draftStore';
import { recordIteration } from '../design-studio/state/iterationStore';
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
    expect(approvedWorkspace.workspace.stages.find((stage) => stage.id === 'draft')?.status).toBe('ready');
    expect(approvedWorkspace.workspace.currentStage).toBe('draft');
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
      'handoff',
      'compare',
    ]);
    expect(stageLabels.materialize).toBe('Prepare For Review');
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
});
