import * as crypto from 'crypto';
import * as path from 'path';
import type * as vscode from 'vscode';
import { createApprovedDraftMaterializationRequest, materializeDesignStudioRequest } from '../materialization/materializationCoordinator';
import { buildRefinementExperience } from './refinementExperience';
import { loadConceptState } from '../state/conceptStore';
import { loadDesignBriefState } from '../state/designBriefStore';
import { loadDraftState } from '../state/draftStore';
import { loadIterationState } from '../state/iterationStore';
import { loadRefinementState } from '../state/refinementStore';
import type { AlternateReportConcept, DesignArtifactApprovalState, DesignArtifactApprovalKind, DesignBrief } from '../contracts/designStudioModels';
import type {
  DesignStudioAnalyzerHandoffViewModel,
  DesignStudioApprovalCardViewModel,
  DesignStudioConceptComparisonViewModel,
  DesignStudioConceptFlowStepViewModel,
  DesignStudioConceptKpiNodeViewModel,
  DesignStudioConceptNavigationNodeViewModel,
  DesignStudioConceptReviewViewModel,
  DesignStudioDraftNavigationReviewViewModel,
  DesignStudioDraftPageReviewViewModel,
  DesignStudioDraftReviewViewModel,
  DesignStudioMaterializationReadinessViewModel,
  DesignStudioWorkflowStageId,
  DesignStudioWorkflowStageStatus,
  DesignStudioWorkflowStageViewModel,
  DesignStudioWorkspaceViewModel,
} from '../contracts/designStudioShell';

export interface DesignStudioWorkspaceBuildResult {
  threadId: string;
  currentBrief?: DesignBrief;
  workspace: DesignStudioWorkspaceViewModel;
  handoffCandidatesByRequestId: Map<string, NonNullable<ReturnType<typeof materializeDesignStudioRequest> extends infer T ? T extends { ok: true; candidate: infer C } ? C : never : never>>;
}

const DEFAULT_TARGET_SURFACE_TYPE = 'pbirReport';
const DEFAULT_TARGET_ANALYZER = 'pbirDesignReview';
const DEFAULT_TARGET_PROFILE = 'consultant';

function createThreadId(reportPath: string): string {
  return `design-studio:${crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16)}`;
}

function stageLabel(id: DesignStudioWorkflowStageId): string {
  switch (id) {
    case 'brief':
      return 'Design Brief';
    case 'concept':
      return 'Concept Studio';
    case 'draft':
      return 'Draft Studio';
    case 'refinement':
      return 'Refinement Studio';
    case 'materialize':
      return 'Prepare For Review';
    case 'handoff':
      return 'Review Design';
    case 'compare':
      return 'Compare Iterations';
  }
}

function stageSummary(id: DesignStudioWorkflowStageId): { title: string; description: string } {
  switch (id) {
    case 'brief':
      return {
        title: 'Design Brief',
        description: 'Define the audience, objective, story, and navigation expectations for the design thread.',
      };
    case 'concept':
      return {
        title: 'Concept Studio',
        description: 'Review the chapter structure, KPI hierarchy, navigation, and analytical flow before approving the concept baseline.',
      };
    case 'draft':
      return {
        title: 'Draft Studio',
        description: 'Review the designed pages, layouts, navigation, and KPI placement before approving the draft.',
      };
    case 'refinement':
      return {
        title: 'Refinement Studio',
        description: 'Review analyzer-derived design improvements without mutating the report automatically.',
      };
    case 'materialize':
      return {
        title: 'Prepare For Review',
        description: 'Prepare the approved draft for review explicitly without changing the report.',
      };
    case 'handoff':
      return {
        title: 'Review Design',
        description: 'Open Analyzer Workspace explicitly when the prepared review candidate is ready.',
      };
    case 'compare':
      return {
        title: 'Compare Iterations',
        description: 'Compare iterations to see what changed and whether validation improved.',
      };
  }
}

function toReadinessLabel(status: DesignStudioWorkflowStageStatus, stageId: DesignStudioWorkflowStageId): string {
  if (stageId === 'compare' && status === 'approved') {
    return 'Validated';
  }

  switch (status) {
    case 'notStarted':
      return 'Not started';
    case 'inProgress':
      return 'In progress';
    case 'ready':
      return 'Ready';
    case 'approved':
      return 'Approved';
    case 'blocked':
      return 'Blocked';
  }
}

function buildStage(id: DesignStudioWorkflowStageId, status: DesignStudioWorkflowStageStatus): DesignStudioWorkflowStageViewModel {
  const summary = stageSummary(id);
  return {
    id,
    label: stageLabel(id),
    status,
    readinessLabel: toReadinessLabel(status, id),
    title: summary.title,
    description: summary.description,
  };
}

function buildKpiHierarchy(nodes: Array<{ id: string; label: string; level: 'primary' | 'supporting' | 'diagnostic'; childNodeIds: string[] }>): DesignStudioConceptKpiNodeViewModel[] {
  const byId = new Map(nodes.map((node) => [node.id, node]));
  const childIds = new Set(nodes.flatMap((node) => node.childNodeIds));
  const roots = nodes.filter((node) => !childIds.has(node.id));
  const ordered: DesignStudioConceptKpiNodeViewModel[] = [];

  const visit = (nodeId: string, depth: number) => {
    const node = byId.get(nodeId);
    if (!node) {
      return;
    }

    ordered.push({
      label: node.label,
      level: node.level,
      depth,
    });

    for (const childNodeId of node.childNodeIds) {
      visit(childNodeId, depth + 1);
    }
  };

  for (const root of roots) {
    visit(root.id, 0);
  }

  return ordered;
}

function flattenChapterStructure(concept: AlternateReportConcept): string[] {
  return concept.chapterMap.chapters.map((chapter) => chapter.title);
}

function flattenKpiHierarchy(concept: AlternateReportConcept): string[] {
  return buildKpiHierarchy(concept.kpiHierarchy.nodes).map((node) => node.label);
}

function flattenNavigationStructure(concept: AlternateReportConcept): string[] {
  return concept.navigationStructure.sections.map((section) => section.label);
}

function flattenAnalyticalFlow(concept: AlternateReportConcept): string[] {
  return concept.analyticalFlow.steps.map((step) => step.label);
}

function buildConceptComparisons(
  selectedConcept: AlternateReportConcept | undefined,
  alternateConcepts: AlternateReportConcept[],
): DesignStudioConceptComparisonViewModel[] {
  if (!selectedConcept) {
    return [];
  }

  return alternateConcepts
    .filter((concept) => concept.id !== selectedConcept.id)
    .map((concept) => ({
      comparisonConceptLabel: concept.label,
      chapterStructure: {
        baselineItems: flattenChapterStructure(selectedConcept),
        comparisonItems: flattenChapterStructure(concept),
      },
      kpiHierarchy: {
        baselineItems: flattenKpiHierarchy(selectedConcept),
        comparisonItems: flattenKpiHierarchy(concept),
      },
      navigationStructure: {
        baselineItems: flattenNavigationStructure(selectedConcept),
        comparisonItems: flattenNavigationStructure(concept),
      },
      analyticalFlow: {
        baselineItems: flattenAnalyticalFlow(selectedConcept),
        comparisonItems: flattenAnalyticalFlow(concept),
      },
    }));
}

function buildConceptReview(conceptState: NonNullable<Awaited<ReturnType<typeof loadConceptState>>>): DesignStudioConceptReviewViewModel {
  const selectedConcept = conceptState.currentConcept.alternateConcepts.find(
    (concept) => concept.id === conceptState.currentConcept.approvedBaselineConceptId,
  ) ?? conceptState.currentConcept.alternateConcepts.find(
    (concept) => concept.id === conceptState.currentConcept.preferredBaselineConceptId,
  ) ?? conceptState.currentConcept.alternateConcepts[0];

  return {
    title: 'Concept Review Artifacts',
    summary: 'Review the chapter structure, KPI hierarchy, navigation path, and analytical flow before Draft Studio work begins.',
    conceptId: conceptState.currentConcept.id,
    approvalState: conceptState.currentConcept.approvalState,
    alternateConcepts: conceptState.currentConcept.alternateConcepts,
    comparison: conceptState.currentConcept.comparison,
    preferredBaselineConceptId: conceptState.currentConcept.preferredBaselineConceptId,
    approvedBaselineConceptId: conceptState.currentConcept.approvedBaselineConceptId,
    selectedConceptLabel: selectedConcept?.label ?? 'Current concept baseline',
    chapterStructure: selectedConcept?.chapterMap.chapters.map((chapter) => ({
      title: chapter.title,
      objective: chapter.objective,
    })) ?? [],
    kpiHierarchy: buildKpiHierarchy(selectedConcept?.kpiHierarchy.nodes ?? []),
    navigationStructure: selectedConcept?.navigationStructure.sections.map((section, index) => ({
      label: section.label,
      depth: index,
    })) ?? [],
    analyticalFlow: selectedConcept?.analyticalFlow.steps.map<DesignStudioConceptFlowStepViewModel>((step) => ({
      label: step.label,
      objective: step.objective,
    })) ?? [],
    comparisons: buildConceptComparisons(selectedConcept, conceptState.currentConcept.alternateConcepts),
  };
}

function buildDraftReview(
  conceptState: NonNullable<Awaited<ReturnType<typeof loadConceptState>>>,
  draftState: NonNullable<Awaited<ReturnType<typeof loadDraftState>>>,
): DesignStudioDraftReviewViewModel {
  const pageConceptById = new Map(conceptState.currentConcept.pageConcepts.map((pageConcept) => [pageConcept.id, pageConcept]));
  const pageArtifactById = new Map(draftState.pageArtifacts.map((artifact) => [artifact.id, artifact]));

  const draftPages = draftState.pageArtifacts.map<DesignStudioDraftPageReviewViewModel>((artifact) => ({
    title: pageConceptById.get(artifact.pageConceptId ?? '')?.title ?? 'Draft page',
    structureSummary: artifact.structureSummary,
    kpiPlacement: [...artifact.recommendedVisualRoles],
  }));

  const draftLayouts = draftState.layoutArtifacts.map((artifact) => ({
    title: artifact.title,
    layoutType: artifact.layoutType,
    zones: [...artifact.zones],
  }));

  const draftNavigation = draftState.navigationArtifacts.flatMap((artifact) =>
    artifact.sections.map<DesignStudioDraftNavigationReviewViewModel>((section) => ({
      label: section.label,
      pageTitle: pageConceptById.get(pageArtifactById.get(section.pageArtifactId)?.pageConceptId ?? '')?.title ?? section.label,
    })));

  return {
    title: 'Draft Review Artifacts',
    summary: 'Review the designed pages, layouts, navigation, and KPI placement before approval.',
    draftStatusLabel: draftState.currentDraft.approvalState === 'approved' ? 'Approved draft' : 'Draft awaiting approval',
    draftPages,
    draftLayouts,
    draftNavigation,
  };
}

function firstNonApprovedStage(stages: DesignStudioWorkflowStageViewModel[]): DesignStudioWorkflowStageId {
  const inProgress = stages.find((stage) => stage.status === 'inProgress');
  if (inProgress) {
    return inProgress.id;
  }

  const ready = stages.find((stage) => stage.status === 'ready');
  if (ready) {
    return ready.id;
  }

  const notStarted = stages.find((stage) => stage.status === 'notStarted');
  if (notStarted) {
    return notStarted.id;
  }

  const blocked = stages.find((stage) => stage.status === 'blocked');
  if (blocked) {
    return blocked.id;
  }

  return stages.at(-1)?.id ?? 'brief';
}

function buildApprovalCard(
  kind: DesignArtifactApprovalKind,
  approvalState: DesignArtifactApprovalState,
): DesignStudioApprovalCardViewModel {
  switch (kind) {
    case 'designApproval':
      return {
        kind,
        title: 'Design Approval',
        approvalState,
        owner: 'Design Studio',
        unlock: 'Allows the next design stage to proceed from the approved baseline.',
        nonEffects: [
          'Does not materialize the draft.',
          'Does not validate the report.',
        ],
      };
    case 'materializationApproval':
      return {
        kind,
        title: 'Materialization Approval',
        approvalState,
        owner: 'Design Studio',
        unlock: 'Allows candidate preparation for explicit analyzer handoff.',
        nonEffects: [
          'Does not run analyzers automatically.',
          'Does not mutate PBIR assets.',
        ],
      };
    case 'refinementApproval':
      return {
        kind,
        title: 'Refinement Approval',
        approvalState,
        owner: 'Design Studio',
        unlock: 'Accepts advisory design changes into the next iteration path.',
        nonEffects: [
          'Does not validate the refined result.',
        ],
      };
    case 'validationApproval':
      return {
        kind,
        title: 'Validation Approval',
        approvalState,
        owner: 'Analyzer Workspace',
        unlock: 'Records the analyzer-owned validation outcome for this iteration.',
        nonEffects: [
          'Cannot be self-approved by Design Studio.',
        ],
      };
  }
}

export async function buildDesignStudioWorkspace(
  context: vscode.ExtensionContext,
  reportPath: string,
): Promise<DesignStudioWorkspaceBuildResult> {
  const threadId = createThreadId(reportPath);
  const reportLabel = path.basename(reportPath, path.extname(reportPath));
  const [briefState, conceptState, draftState, refinementState, iterationState] = await Promise.all([
    loadDesignBriefState(context, threadId),
    loadConceptState(context, threadId),
    loadDraftState(context, threadId),
    loadRefinementState(context, threadId),
    loadIterationState(context, threadId),
  ]);

  const latestIteration = iterationState?.iterations.at(-1);
  const handoffCandidatesByRequestId = new Map<string, NonNullable<ReturnType<typeof materializeDesignStudioRequest> extends infer T ? T extends { ok: true; candidate: infer C } ? C : never : never>>();

  let materializationReadiness: DesignStudioMaterializationReadinessViewModel | undefined;
  let analyzerHandoff: DesignStudioAnalyzerHandoffViewModel | undefined;
  let materializeStatus: DesignStudioWorkflowStageStatus = 'blocked';
  let handoffStatus: DesignStudioWorkflowStageStatus = 'blocked';

  if (draftState?.currentDraft.approvalState === 'approved') {
    const requestId = `materialization-request:${threadId}`;
    const request = await createApprovedDraftMaterializationRequest(context, {
      threadId,
      requestId,
      targetSurfaceType: DEFAULT_TARGET_SURFACE_TYPE,
      targetAnalyzer: DEFAULT_TARGET_ANALYZER,
      targetAnalyzerProfile: DEFAULT_TARGET_PROFILE,
      handoffContext: {
        repositoryBackedPath: reportPath,
        degradedMappings: [],
        omittedEvidence: [],
      },
    });
    const materialization = materializeDesignStudioRequest(request);

    if (materialization.ok) {
      handoffCandidatesByRequestId.set(requestId, materialization.candidate);
      materializationReadiness = {
        readinessLabel: materialization.candidate.analyzerHandoff.metadata.executableEligibility === 'executable'
          ? 'Ready for analysis'
          : materialization.candidate.analyzerHandoff.metadata.executableEligibility === 'nonExecutablePreview'
            ? 'Preview only'
            : 'Needs attention',
        executableEligibility: materialization.candidate.analyzerHandoff.metadata.executableEligibility,
        targetAnalyzer: request.targetAnalyzer,
        targetAnalyzerProfile: request.targetAnalyzerProfile,
        diagnostics: materialization.diagnostics,
      };
      analyzerHandoff = {
        requestId,
        readinessLabel: materialization.candidate.analyzerHandoff.metadata.executableEligibility === 'executable'
          ? 'Ready to open Analyzer Workspace'
          : 'Analyzer handoff blocked',
        analyzerId: request.targetAnalyzer,
        analyzerProfileId: request.targetAnalyzerProfile,
        canOpen: materialization.candidate.analyzerHandoff.metadata.executableEligibility === 'executable',
        diagnostics: materialization.candidate.analyzerHandoff.metadata.executableEligibility === 'executable'
          ? ['Analysis has not started. Launch is explicit.']
          : materialization.diagnostics,
      };
      materializeStatus = latestIteration?.approvalCheckpoint.materializationApproval.approvalState === 'approved'
        ? 'approved'
        : 'ready';
      handoffStatus = latestIteration?.approvalCheckpoint.validationApproval.approvalState === 'approved'
        ? 'approved'
        : analyzerHandoff.canOpen
          ? 'ready'
          : 'blocked';
    } else {
      materializationReadiness = {
        readinessLabel: 'Needs attention',
        executableEligibility: 'unsupported',
        targetAnalyzer: DEFAULT_TARGET_ANALYZER,
        targetAnalyzerProfile: DEFAULT_TARGET_PROFILE,
        diagnostics: materialization.diagnostics,
      };
      analyzerHandoff = {
        requestId,
        readinessLabel: 'Analyzer handoff blocked',
        analyzerId: DEFAULT_TARGET_ANALYZER,
        analyzerProfileId: DEFAULT_TARGET_PROFILE,
        canOpen: false,
        diagnostics: materialization.diagnostics,
      };
      materializeStatus = 'blocked';
      handoffStatus = 'blocked';
    }
  } else {
    materializationReadiness = {
      readinessLabel: 'Blocked until an approved draft exists',
      executableEligibility: 'unsupported',
      targetAnalyzer: DEFAULT_TARGET_ANALYZER,
      targetAnalyzerProfile: DEFAULT_TARGET_PROFILE,
      diagnostics: ['Draft Studio approval is required before candidate preparation can proceed.'],
    };
    analyzerHandoff = {
      requestId: `materialization-request:${threadId}`,
      readinessLabel: 'Analyzer handoff blocked',
      analyzerId: DEFAULT_TARGET_ANALYZER,
      analyzerProfileId: DEFAULT_TARGET_PROFILE,
      canOpen: false,
      diagnostics: ['No approved draft is available for analyzer handoff.'],
    };
  }

  const refinementStatus: DesignStudioWorkflowStageStatus = refinementState?.proposals.length
    ? refinementState.proposals.every((proposal) => proposal.approvalState === 'approved')
      ? 'approved'
      : 'inProgress'
    : latestIteration?.analyzerResults.length
      ? 'ready'
      : 'blocked';

  const compareStatus: DesignStudioWorkflowStageStatus = (iterationState?.iterations.length ?? 0) >= 2
    ? latestIteration?.approvalCheckpoint.validationApproval.approvalState === 'approved'
      ? 'approved'
      : 'ready'
    : 'notStarted';

  const stages: DesignStudioWorkflowStageViewModel[] = [
    buildStage(
      'brief',
      !briefState
        ? 'notStarted'
        : briefState.current.approvalState === 'approved'
          ? 'approved'
          : briefState.validation.isValid
            ? 'ready'
            : 'inProgress',
    ),
    buildStage(
      'concept',
      !briefState || briefState.current.approvalState !== 'approved'
        ? 'blocked'
        : !conceptState
          ? 'notStarted'
          : conceptState.currentConcept.approvalState === 'approved'
            ? 'approved'
            : conceptState.currentConcept.approvalState === 'pendingApproval'
              ? 'ready'
              : 'inProgress',
    ),
    buildStage(
      'draft',
      !conceptState || conceptState.currentConcept.approvalState !== 'approved'
        ? 'blocked'
        : !draftState
          ? 'ready'
          : draftState.currentDraft.approvalState === 'approved'
            ? 'approved'
            : 'inProgress',
    ),
    buildStage('refinement', refinementStatus),
    buildStage('materialize', materializeStatus),
    buildStage('handoff', handoffStatus),
    buildStage('compare', compareStatus),
  ];

  const designApprovalState: DesignArtifactApprovalState = draftState?.currentDraft.approvalState
    ?? conceptState?.currentConcept.approvalState
    ?? briefState?.current.approvalState
    ?? 'notSubmitted';
  const materializationApprovalState: DesignArtifactApprovalState = latestIteration?.approvalCheckpoint.materializationApproval.approvalState
    ?? (draftState?.currentDraft.approvalState === 'approved' ? 'approved' : 'notSubmitted');
  const refinementApprovalState: DesignArtifactApprovalState = latestIteration?.approvalCheckpoint.refinementApproval.approvalState
    ?? (refinementState?.proposals.length ? 'pendingApproval' : 'notSubmitted');
  const validationApprovalState: DesignArtifactApprovalState = latestIteration?.approvalCheckpoint.validationApproval.approvalState
    ?? 'notSubmitted';

  const currentStage = firstNonApprovedStage(stages);
  const refinementExperience = briefState && conceptState && draftState
    ? buildRefinementExperience({
      brief: briefState.current,
      concept: conceptState.currentConcept,
      draft: draftState.currentDraft,
      pageConcepts: conceptState.currentConcept.pageConcepts,
      pageArtifacts: draftState.pageArtifacts,
      layoutArtifacts: draftState.layoutArtifacts,
      navigationArtifacts: draftState.navigationArtifacts,
      proposals: refinementState?.proposals ?? [],
    })
    : undefined;
  const conceptReview = conceptState ? buildConceptReview(conceptState) : undefined;
  const draftReview = conceptState && draftState ? buildDraftReview(conceptState, draftState) : undefined;

  return {
    threadId,
    currentBrief: briefState?.current,
    handoffCandidatesByRequestId,
    workspace: {
      reportLabel,
      currentStage,
      stages,
      currentStageSummary: stageSummary(currentStage),
      approvalCards: [
        buildApprovalCard('designApproval', designApprovalState),
        buildApprovalCard('materializationApproval', materializationApprovalState),
        buildApprovalCard('refinementApproval', refinementApprovalState),
        buildApprovalCard('validationApproval', validationApprovalState),
      ],
      materializationReadiness,
      analyzerHandoff,
      refinementExperience,
      conceptReview,
      draftReview,
    },
  };
}
