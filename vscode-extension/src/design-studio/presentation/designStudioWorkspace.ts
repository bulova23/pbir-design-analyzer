import * as crypto from 'crypto';
import * as path from 'path';
import type * as vscode from 'vscode';
import { createApprovedDraftMaterializationRequest, materializeDesignStudioRequest } from '../materialization/materializationCoordinator';
import { buildRefinementExperience } from './refinementExperience';
import { loadConceptState } from '../state/conceptStore';
import { loadDesignBriefState } from '../state/designBriefStore';
import { loadDraftState } from '../state/draftStore';
import { evaluateIterationCompletion, loadIterationState } from '../state/iterationStore';
import { loadPrepareForReviewState } from '../state/prepareForReviewStore';
import { loadDesignStudioPreviewReviewState, type DesignStudioPreviewReviewRecord } from '../state/previewReviewStore';
import { buildDesignStudioExecutionReadinessDashboard } from '../state/executionReadinessStore';
import { loadRefinementState } from '../state/refinementStore';
import { loadReviewDesignState } from '../state/reviewDesignStore';
import type { AlternateReportConcept, DesignArtifactApprovalState, DesignArtifactApprovalKind, DesignBrief } from '../contracts/designStudioModels';
import type {
  DesignStudioAnalyzerResultSummaryViewModel,
  DesignStudioAnalyzerHandoffViewModel,
  DesignStudioApprovalCardViewModel,
  DesignStudioConceptComparisonViewModel,
  DesignStudioConceptFlowStepViewModel,
  DesignStudioConceptKpiNodeViewModel,
  DesignStudioConceptReviewViewModel,
  DesignStudioDraftNavigationReviewViewModel,
  DesignStudioDraftPageReviewViewModel,
  DesignStudioDraftReviewViewModel,
  DesignStudioPreviewReviewViewModel,
  DesignStudioExecutionReadinessViewModel,
  DesignStudioWorkflowCompletionViewModel,
  DesignStudioMaterializationReadinessViewModel,
  DesignStudioReviewDesignViewModel,
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
const DEFAULT_TARGET_PROFILE = 'default';

function validationStatusLabel(value: string): string {
  switch (value) {
    case 'validated':
      return 'Validated';
    case 'rejected':
      return 'Rejected';
    case 'needsReview':
      return 'Needs review';
    case 'approved':
      return 'Approved';
    case 'pendingApproval':
      return 'Pending approval';
    default:
      return 'Not submitted';
  }
}

function validationResultSummaryLabel(input: {
  validationResultStatus: string;
  validationApprovalState: string;
}): string {
  if (input.validationResultStatus === 'validated' && input.validationApprovalState !== 'approved') {
    return 'Validation pending';
  }

  return validationStatusLabel(input.validationResultStatus);
}

function analyzerSourceLabel(value: string): string {
  switch (value) {
    case 'storyAssessment':
      return 'Story Assessment';
    case 'guidedStoryImprovements':
      return 'Guided Story Improvements';
    case 'crossPageNarrative':
      return 'Cross-Page Narrative';
    case 'fixPlan':
      return 'Fix Plan';
    default:
      return 'Issues';
  }
}

function approvalStateLabel(value: DesignArtifactApprovalState): string {
  switch (value) {
    case 'approved':
      return 'Approved';
    case 'pendingApproval':
      return 'Ready For Approval';
    case 'rejected':
      return 'Rejected';
    default:
      return 'Candidate Created';
  }
}

function eligibilityLabel(value: NonNullable<DesignStudioMaterializationReadinessViewModel['executableEligibility']>): string {
  switch (value) {
    case 'executable':
      return 'Executable';
    case 'nonExecutablePreview':
      return 'Preview only';
    default:
      return 'Unsupported';
  }
}

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
    case 'previewReview':
      return 'Preview Review';
    case 'handoff':
      return 'Review Design';
    case 'compare':
      return 'Compare Iterations';
    case 'completion':
      return 'Workflow Completion';
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
    case 'previewReview':
      return {
        title: 'Preview Review',
        description: 'Inspect the PBIR preview package and review handoff metadata without running validation or mutating the report.',
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
    case 'completion':
      return {
        title: 'Workflow Completion',
        description: 'Close the iteration explicitly without changing approval ownership, validation ownership, or report deployment state.',
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

function buildStage(
  id: DesignStudioWorkflowStageId,
  status: DesignStudioWorkflowStageStatus,
  readinessLabelOverride?: string,
): DesignStudioWorkflowStageViewModel {
  const summary = stageSummary(id);
  return {
    id,
    label: stageLabel(id),
    status,
    readinessLabel: readinessLabelOverride ?? toReadinessLabel(status, id),
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
    draftId: draftState.currentDraft.id,
    approvalState: draftState.currentDraft.approvalState,
    draftStatusLabel: draftState.currentDraft.approvalState === 'approved'
      ? 'Approved draft'
      : draftState.currentDraft.approvalState === 'pendingApproval'
        ? 'Ready for approval'
        : 'Draft generated',
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
        title: 'Review Candidate Approval',
        approvalState,
        owner: 'Design Studio',
        unlock: 'Allows Review Design to continue from the approved review candidate.',
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

function previewReviewLabel(action: string): string {
  switch (action) {
    case 'markedReviewed':
      return 'Preview Reviewed';
    case 'revisionRequested':
      return 'Revision Requested';
    case 'deferred':
      return 'Review Deferred';
    case 'analyzerCandidateMetadataPrepared':
      return 'Analyzer Metadata Prepared';
    default:
      return 'Pending Review';
  }
}

function findPreviewReference(
  record: DesignStudioPreviewReviewRecord,
  artifactType: string,
): string | undefined {
  return record.previewPackage.fileInventory.find((file) => file.artifactType === artifactType)?.reference;
}

function buildPreviewReviewViewModel(record: DesignStudioPreviewReviewRecord): DesignStudioPreviewReviewViewModel {
  return {
    previewReviewId: record.previewReviewId,
    schemaVersion: record.schemaVersion,
    previewPackageId: record.previewPackage.packageId,
    previewPackageSchemaVersion: record.previewPackage.schemaVersion,
    previewPackageHash: record.previewPackage.packageHash,
    generatedUtc: record.previewPackage.generatedUtc,
    reviewHandoffId: record.reviewHandoff.handoffId,
    reviewHandoffSchemaVersion: record.reviewHandoff.schemaVersion,
    reviewReadiness: record.reviewHandoff.reviewReadiness,
    readinessState: record.readinessState,
    reviewerAction: record.reviewerAction,
    reviewerNotes: record.reviewerNotes,
    reviewTimestamp: record.reviewTimestamp,
    requiredReviewerAction: record.reviewHandoff.requiredReviewerAction,
    summary: {
      fileCount: record.previewPackage.summary.fileCount,
      warningCount: record.previewPackage.summary.warningCount,
      rejectedArtifactCount: record.previewPackage.summary.rejectedArtifactCount,
      hashCount: record.previewPackage.hashInventory.length,
    },
    references: {
      previewMarkdown: findPreviewReference(record, 'previewMarkdown'),
      previewJson: findPreviewReference(record, 'previewJson'),
      canonicalIr: findPreviewReference(record, 'canonicalIrJson'),
      previewManifest: findPreviewReference(record, 'previewManifestJson'),
      diagnostics: findPreviewReference(record, 'diagnosticsMarkdown'),
      reviewHandoff: record.reviewHandoff.handoffId,
    },
    fileInventory: record.previewPackage.fileInventory.map((file) => ({ ...file })),
    hashInventory: record.previewPackage.hashInventory.map((entry) => ({ ...entry })),
    lineage: {
      ...record.previewPackage.lineage,
      immutableLineage: [...record.previewPackage.lineage.immutableLineage],
    },
    rollbackMetadata: { ...record.previewPackage.rollbackMetadata },
    analyzerBoundary: { ...record.reviewHandoff.analyzerWorkspaceBoundary },
    reviewOnlyBoundary: { ...record.reviewOnlyBoundary },
    warnings: [...record.warnings, ...record.previewPackage.warnings, ...record.reviewHandoff.warnings],
    rejectedArtifacts: [...record.previewPackage.rejectedArtifacts],
    canMarkReviewed: record.reviewerAction !== 'markedReviewed',
    canRequestRevision: record.reviewerAction !== 'revisionRequested',
    canDeferReview: record.reviewerAction !== 'deferred',
    canPrepareAnalyzerCandidateMetadata: record.reviewerAction !== 'analyzerCandidateMetadataPrepared',
  };
}

export async function buildDesignStudioWorkspace(
  context: vscode.ExtensionContext,
  reportPath: string,
): Promise<DesignStudioWorkspaceBuildResult> {
  const threadId = createThreadId(reportPath);
  const reportLabel = path.basename(reportPath, path.extname(reportPath));
  const [briefState, conceptState, draftState, prepareForReviewState, previewReviewState, refinementState, reviewDesignState, iterationState, workflowCompletion] = await Promise.all([
    loadDesignBriefState(context, threadId),
    loadConceptState(context, threadId),
    loadDraftState(context, threadId),
    loadPrepareForReviewState(context, threadId),
    loadDesignStudioPreviewReviewState(context, threadId),
    loadRefinementState(context, threadId),
    loadReviewDesignState(context, threadId),
    loadIterationState(context, threadId),
    evaluateIterationCompletion(context, threadId),
  ]);

  const latestIteration = iterationState?.iterations.at(-1);
  const handoffCandidatesByRequestId = new Map<string, NonNullable<ReturnType<typeof materializeDesignStudioRequest> extends infer T ? T extends { ok: true; candidate: infer C } ? C : never : never>>();

  let materializationReadiness: DesignStudioMaterializationReadinessViewModel | undefined;
  let analyzerHandoff: DesignStudioAnalyzerHandoffViewModel | undefined;
  let reviewDesign: DesignStudioReviewDesignViewModel | undefined;
  let previewReview: DesignStudioPreviewReviewViewModel | undefined;
  let executionReadiness: DesignStudioExecutionReadinessViewModel | undefined;
  let materializeStatus: DesignStudioWorkflowStageStatus = 'blocked';
  let previewReviewStatus: DesignStudioWorkflowStageStatus = 'blocked';
  let handoffStatus: DesignStudioWorkflowStageStatus = 'blocked';

  if (previewReviewState?.currentReview) {
    previewReview = buildPreviewReviewViewModel(previewReviewState.currentReview);
    executionReadiness = buildDesignStudioExecutionReadinessDashboard(previewReviewState.currentReview);
    previewReviewStatus = previewReviewState.currentReview.reviewerAction === 'markedReviewed'
      || previewReviewState.currentReview.reviewerAction === 'analyzerCandidateMetadataPrepared'
      ? 'approved'
      : previewReviewState.currentReview.reviewerAction === 'pending'
        ? 'ready'
        : 'inProgress';
  }

  if (draftState?.currentDraft.approvalState === 'approved') {
    const request = await createApprovedDraftMaterializationRequest(context, {
      threadId,
      requestId: prepareForReviewState?.currentRequest.id ?? `materialization-request:${threadId}`,
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
      const candidate = prepareForReviewState?.currentCandidate ?? materialization.candidate;
      const candidateSummary = {
        sourceDraftVersionId: draftState.currentDraft ? `${draftState.currentDraft.id}@v${draftState.currentDraft.version}` : undefined,
        sourceConceptVersionId: draftState.currentDraft.sourceConceptVersionId,
        sourceDesignBriefVersionId: draftState.currentDraft.sourceBriefVersionId,
      };
      const approvalsUsed = [
        `Draft approval: ${draftState.currentDraft.approvalState === 'approved' ? 'Approved' : 'Not approved'}`,
        `Concept approval: ${conceptState?.currentConcept.approvalState === 'approved' ? 'Approved' : 'Not approved'}`,
        `Design Brief approval: ${briefState?.current.approvalState === 'approved' ? 'Approved' : 'Not approved'}`,
      ];

      if (candidate.approvalState === 'approved' && candidate.analyzerHandoff.metadata.executableEligibility === 'executable') {
        handoffCandidatesByRequestId.set(request.id, candidate);
      }

      materializationReadiness = {
        readinessLabel: !prepareForReviewState
          ? 'Ready to create a review candidate'
          : candidate.analyzerHandoff.metadata.executableEligibility === 'executable'
            ? 'Ready for consultant review'
            : candidate.analyzerHandoff.metadata.executableEligibility === 'nonExecutablePreview'
              ? 'Preview only'
              : 'Needs attention',
        executableEligibility: candidate.analyzerHandoff.metadata.executableEligibility,
        targetAnalyzer: request.targetAnalyzer,
        targetAnalyzerProfile: request.targetAnalyzerProfile,
        diagnostics: prepareForReviewState ? candidate.materializationDiagnostics : ['Create a review candidate from the approved draft.'],
        candidateId: prepareForReviewState?.currentCandidate.id,
        requestId: prepareForReviewState?.currentRequest.id,
        candidateStatusLabel: prepareForReviewState
          ? approvalStateLabel(candidate.approvalState)
          : 'Not Started',
        materializationStatus: prepareForReviewState
          ? eligibilityLabel(candidate.analyzerHandoff.metadata.executableEligibility)
          : 'Not started',
        nextStepGuidance: !prepareForReviewState
          ? 'Create a review candidate from the approved draft.'
          : candidate.approvalState === 'notSubmitted'
            ? 'Review readiness diagnostics before approval.'
            : candidate.approvalState === 'pendingApproval'
              ? 'Approve the review candidate to unlock Review Design.'
              : 'Review candidate approved. Continue to Review Design.',
        canCreateCandidate: !prepareForReviewState,
        canSubmitCandidateForApproval: prepareForReviewState?.currentCandidate.approvalState === 'notSubmitted',
        canApproveCandidate: prepareForReviewState?.currentCandidate.approvalState === 'pendingApproval',
        sourceDraftVersionId: candidateSummary.sourceDraftVersionId,
        sourceConceptVersionId: candidateSummary.sourceConceptVersionId,
        sourceDesignBriefVersionId: candidateSummary.sourceDesignBriefVersionId,
        lineage: prepareForReviewState
          ? [
            {
              label: 'Source draft',
              artifactVersionId: candidateSummary.sourceDraftVersionId ?? 'Unavailable',
              approvalState: draftState.currentDraft.approvalState,
            },
            ...(candidateSummary.sourceConceptVersionId
              ? [{
                label: 'Source concept',
                artifactVersionId: candidateSummary.sourceConceptVersionId,
                approvalState: conceptState?.currentConcept.approvalState ?? 'notSubmitted',
              }]
              : []),
            ...(candidateSummary.sourceDesignBriefVersionId
              ? [{
                label: 'Source design brief',
                artifactVersionId: candidateSummary.sourceDesignBriefVersionId,
                approvalState: briefState?.current.approvalState ?? 'notSubmitted',
              }]
              : []),
          ]
          : undefined,
        approvalsUsed,
      };
      analyzerHandoff = {
        requestId: request.id,
        readinessLabel: candidate.approvalState === 'approved'
          && candidate.analyzerHandoff.metadata.executableEligibility === 'executable'
          ? 'Ready to open Analyzer Workspace'
          : 'Analyzer handoff blocked',
        analyzerId: request.targetAnalyzer,
        analyzerProfileId: request.targetAnalyzerProfile,
        canOpen: candidate.approvalState === 'approved'
          && candidate.analyzerHandoff.metadata.executableEligibility === 'executable',
        diagnostics: candidate.approvalState === 'approved'
          && candidate.analyzerHandoff.metadata.executableEligibility === 'executable'
          ? ['Analysis has not started. Launch is explicit.']
          : candidate.approvalState !== 'approved'
            ? ['Approve the review candidate before opening Review Design.']
            : candidate.materializationDiagnostics,
      };
      reviewDesign = {
        requestId: request.id,
        candidateId: candidate.id,
        sourceDraftVersionId: candidateSummary.sourceDraftVersionId,
        sourceConceptVersionId: candidateSummary.sourceConceptVersionId,
        sourceDesignBriefVersionId: candidateSummary.sourceDesignBriefVersionId,
        approvedReviewCandidateVersionId: prepareForReviewState?.currentCandidate
          ? `${prepareForReviewState.currentCandidate.id}@v${prepareForReviewState.currentCandidate.version}`
          : undefined,
        reviewReadinessLabel: candidate.approvalState !== 'approved'
          ? 'Blocked'
          : candidate.analyzerHandoff.metadata.executableEligibility === 'unsupported'
            ? 'Unsupported'
            : candidate.analyzerHandoff.metadata.executableEligibility === 'nonExecutablePreview'
              ? 'Preview only'
              : 'Ready for review',
        handoffStatusLabel: reviewDesignState?.currentReview?.status === 'completed'
          ? 'Review recorded as completed'
          : reviewDesignState?.currentReview?.status === 'launched'
            ? 'Analyzer Workspace opened'
            : candidate.approvalState === 'approved' && candidate.analyzerHandoff.metadata.executableEligibility === 'executable'
              ? 'Ready to launch Analyzer Workspace'
              : 'Analyzer Workspace launch blocked',
        reviewStatusLabel: (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) > 0
          ? 'Results Attached'
          : (reviewDesignState?.currentReview?.availableResults?.length ?? 0) > 0
            ? 'Analyzer Results Available'
            : reviewDesignState?.currentReview?.status === 'completed'
              ? 'Awaiting Analyzer Results'
              : reviewDesignState?.currentReview?.status === 'launched'
                ? 'Review Launched'
                : 'Review Not Started',
        completionStatusLabel: reviewDesignState?.currentReview?.status === 'completed'
          ? 'Review completed'
          : 'Review not completed',
        analyzerId: request.targetAnalyzer,
        analyzerProfileId: request.targetAnalyzerProfile,
        readinessDiagnostics: candidate.approvalState === 'approved'
          ? candidate.materializationDiagnostics
          : ['Approve a review candidate before launching review.'],
        ownershipMessages: [
          'Analyzer Workspace owns validation.',
          'Design Studio does not validate itself.',
          'Review Design launches review only.',
          'Validation remains analyzer-owned.',
        ],
        nextStepGuidance: (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) > 0
          ? 'Analyzer results attached. Continue to Refinement Studio.'
          : (reviewDesignState?.currentReview?.availableResults?.length ?? 0) > 0
            ? 'Attach analyzer results to continue refinement.'
            : reviewDesignState?.currentReview?.status === 'completed'
              ? 'Review completed, but no analyzer results are attached yet.'
              : reviewDesignState?.currentReview?.status === 'launched'
                ? 'Complete review in Analyzer Workspace and return here.'
                : candidate.approvalState === 'approved' && candidate.analyzerHandoff.metadata.executableEligibility === 'executable'
                  ? 'Open Analyzer Workspace to review the design.'
                  : candidate.approvalState !== 'approved'
                    ? 'Approve a review candidate before launching review.'
                    : candidate.analyzerHandoff.metadata.executableEligibility === 'unsupported'
                      ? 'Review is unsupported for the current candidate.'
                      : 'Review is preview-only for the current candidate.',
        canOpenAnalyzerWorkspace: candidate.approvalState === 'approved'
          && candidate.analyzerHandoff.metadata.executableEligibility === 'executable',
        canMarkReviewCompleted: reviewDesignState?.currentReview?.status === 'launched',
        canAttachAnalyzerResults: (reviewDesignState?.currentReview?.status === 'completed')
          && (reviewDesignState?.currentReview?.availableResults?.length ?? 0) > 0
          && (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) === 0,
        resultStatusLabel: (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) > 0
          ? 'Analyzer results are attached to this iteration.'
          : (reviewDesignState?.currentReview?.availableResults?.length ?? 0) > 0
            ? 'Validation approval not attached yet'
            : 'No analyzer results are attached yet.',
        availableResults: (reviewDesignState?.currentReview?.availableResults ?? []).map<DesignStudioAnalyzerResultSummaryViewModel>((result) => ({
          analyzerSourceLabel: analyzerSourceLabel(result.analyzerSource),
          analyzerRunId: result.analyzerRunId,
          resultReference: result.resultReference,
          scoredAt: result.scoredAt,
          sourceCandidateId: result.sourceCandidateId,
          sourceArtifactVersionFingerprint: [...result.sourceArtifactVersionFingerprint],
          validationResultStatusLabel: validationResultSummaryLabel({
            validationResultStatus: result.validationResultStatus,
            validationApprovalState: result.validationApprovalState,
          }),
          validationApprovalStateLabel: validationStatusLabel(result.validationApprovalState),
          linkedRecommendationCount: result.linkedProposalIds.length,
        })),
      };
      materializeStatus = !prepareForReviewState
        ? 'notStarted'
        : candidate.approvalState === 'approved'
          ? 'approved'
          : candidate.approvalState === 'pendingApproval'
            ? 'ready'
            : 'inProgress';
      handoffStatus = (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) > 0
        ? 'approved'
        : (reviewDesignState?.currentReview?.availableResults?.length ?? 0) > 0
          ? 'ready'
          : reviewDesignState?.currentReview?.status === 'completed'
            ? 'inProgress'
            : reviewDesignState?.currentReview?.status === 'launched'
              ? 'inProgress'
              : prepareForReviewState?.currentCandidate.approvalState === 'approved' && analyzerHandoff.canOpen
                ? 'ready'
                : 'blocked';
    } else {
      materializationReadiness = {
        readinessLabel: 'Needs attention',
        executableEligibility: 'unsupported',
        targetAnalyzer: DEFAULT_TARGET_ANALYZER,
        targetAnalyzerProfile: DEFAULT_TARGET_PROFILE,
        diagnostics: materialization.diagnostics,
        candidateStatusLabel: prepareForReviewState ? approvalStateLabel(prepareForReviewState.currentCandidate.approvalState) : 'Not Started',
        materializationStatus: 'Unsupported',
        nextStepGuidance: prepareForReviewState
          ? 'Review readiness diagnostics before approval.'
          : 'Create a review candidate from the approved draft.',
        canCreateCandidate: !prepareForReviewState,
        canSubmitCandidateForApproval: false,
        canApproveCandidate: false,
      };
      analyzerHandoff = {
        requestId: request.id,
        readinessLabel: 'Analyzer handoff blocked',
        analyzerId: DEFAULT_TARGET_ANALYZER,
        analyzerProfileId: DEFAULT_TARGET_PROFILE,
        canOpen: false,
        diagnostics: materialization.diagnostics,
      };
      reviewDesign = {
        requestId: request.id,
        reviewReadinessLabel: 'Unsupported',
        handoffStatusLabel: 'Analyzer Workspace launch blocked',
        reviewStatusLabel: 'Not started',
        completionStatusLabel: 'Review not completed',
        analyzerId: DEFAULT_TARGET_ANALYZER,
        analyzerProfileId: DEFAULT_TARGET_PROFILE,
        readinessDiagnostics: materialization.diagnostics,
        ownershipMessages: [
          'Analyzer Workspace owns validation.',
          'Design Studio does not validate itself.',
          'Review Design launches review only.',
          'Validation remains analyzer-owned.',
        ],
        nextStepGuidance: 'Review the readiness diagnostics before launching review.',
        canOpenAnalyzerWorkspace: false,
        canMarkReviewCompleted: false,
        canAttachAnalyzerResults: false,
        resultStatusLabel: 'No analyzer results are attached yet.',
        availableResults: [],
      };
      materializeStatus = prepareForReviewState ? 'inProgress' : 'notStarted';
      handoffStatus = 'blocked';
    }
  } else {
    materializationReadiness = {
      readinessLabel: 'Blocked until an approved draft exists',
      executableEligibility: 'unsupported',
      targetAnalyzer: DEFAULT_TARGET_ANALYZER,
      targetAnalyzerProfile: DEFAULT_TARGET_PROFILE,
      diagnostics: ['Draft Studio approval is required before candidate preparation can proceed.'],
      candidateStatusLabel: 'Blocked',
      materializationStatus: 'Blocked',
      nextStepGuidance: 'Approve the Draft before preparing for review.',
      canCreateCandidate: false,
      canSubmitCandidateForApproval: false,
      canApproveCandidate: false,
    };
    analyzerHandoff = {
      requestId: `materialization-request:${threadId}`,
      readinessLabel: 'Analyzer handoff blocked',
      analyzerId: DEFAULT_TARGET_ANALYZER,
      analyzerProfileId: DEFAULT_TARGET_PROFILE,
      canOpen: false,
      diagnostics: ['No approved draft is available for analyzer handoff.'],
    };
    reviewDesign = {
      requestId: `materialization-request:${threadId}`,
      reviewReadinessLabel: 'Blocked',
      handoffStatusLabel: 'Analyzer Workspace launch blocked',
      reviewStatusLabel: 'Not started',
      completionStatusLabel: 'Review not completed',
      analyzerId: DEFAULT_TARGET_ANALYZER,
      analyzerProfileId: DEFAULT_TARGET_PROFILE,
      readinessDiagnostics: ['Approve a review candidate before launching review.'],
      ownershipMessages: [
        'Analyzer Workspace owns validation.',
        'Design Studio does not validate itself.',
        'Review Design launches review only.',
        'Validation remains analyzer-owned.',
      ],
      nextStepGuidance: 'Approve a review candidate before launching review.',
      canOpenAnalyzerWorkspace: false,
      canMarkReviewCompleted: false,
      canAttachAnalyzerResults: false,
      resultStatusLabel: 'No analyzer results are attached yet.',
      availableResults: [],
    };
  }

  const refinementStatus: DesignStudioWorkflowStageStatus = (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) > 0
    ? refinementState?.proposals.length
      ? refinementState.proposals.every((proposal) => proposal.approvalState === 'approved')
        ? 'approved'
        : 'inProgress'
      : 'ready'
    : 'blocked';

  const compareStatus: DesignStudioWorkflowStageStatus = (iterationState?.iterations.length ?? 0) === 0
    ? 'notStarted'
    : (iterationState?.iterations.length ?? 0) >= 2
    ? latestIteration?.approvalCheckpoint.validationApproval.approvalState === 'approved'
      ? 'approved'
      : 'ready'
    : 'inProgress';
  const completionStatus: DesignStudioWorkflowStageStatus = workflowCompletion.state === 'completed'
    ? 'approved'
    : workflowCompletion.state === 'readyForCompletion'
      ? 'ready'
      : workflowCompletion.state === 'reopened'
        ? 'inProgress'
        : workflowCompletion.outstandingItems.length > 0
          ? 'blocked'
          : 'inProgress';

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
          ? 'notStarted'
          : draftState.currentDraft.approvalState === 'approved'
            ? 'approved'
            : 'ready',
    ),
    buildStage('refinement', refinementStatus),
    buildStage(
      'materialize',
      materializeStatus,
      !draftState || draftState.currentDraft.approvalState !== 'approved'
        ? 'Blocked'
        : !prepareForReviewState
          ? 'Not Started'
            : approvalStateLabel(prepareForReviewState.currentCandidate.approvalState),
    ),
    buildStage(
      'previewReview',
      previewReviewStatus,
      previewReview
        ? previewReviewLabel(previewReview.reviewerAction)
        : 'No Preview Package',
    ),
    buildStage(
      'handoff',
      handoffStatus,
      (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) > 0
        ? 'Results Attached'
        : (reviewDesignState?.currentReview?.availableResults?.length ?? 0) > 0
          ? 'Analyzer Results Available'
          : reviewDesignState?.currentReview?.status === 'completed'
            ? 'Awaiting Analyzer Results'
            : reviewDesignState?.currentReview?.status === 'launched'
              ? 'Review Launched'
              : handoffStatus === 'ready'
                ? 'Review Not Started'
              : handoffStatus === 'approved'
                ? 'Completed'
                : handoffStatus === 'inProgress'
                  ? 'Review Launched'
                  : undefined,
    ),
    buildStage(
      'compare',
      compareStatus,
      compareStatus === 'inProgress'
        ? 'Review Result Recorded'
        : undefined,
    ),
    buildStage(
      'completion',
      completionStatus,
      workflowCompletion.state === 'completed'
        ? 'Completed'
        : workflowCompletion.state === 'readyForCompletion'
          ? 'Ready For Completion'
          : workflowCompletion.state === 'reopened'
            ? 'Reopened'
            : 'Active',
    ),
  ];

  const designApprovalState: DesignArtifactApprovalState = draftState?.currentDraft.approvalState
    ?? conceptState?.currentConcept.approvalState
    ?? briefState?.current.approvalState
    ?? 'notSubmitted';
  const materializationApprovalState: DesignArtifactApprovalState = prepareForReviewState?.currentCandidate.approvalState
    ?? latestIteration?.approvalCheckpoint.materializationApproval.approvalState
    ?? 'notSubmitted';
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
  const refinementExperienceViewModel = refinementExperience
    ? (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) > 0 && (refinementState?.proposals.length ?? 0) === 0
      ? {
        ...refinementExperience,
        summary: 'Analyzer results are attached to this iteration. No advisory refinement proposals were returned.',
        emptyState: 'Review completed without attached recommendations.',
      }
      : refinementExperience
    : undefined;
  const conceptReview = conceptState ? buildConceptReview(conceptState) : undefined;
  const draftReview = conceptState && draftState ? buildDraftReview(conceptState, draftState) : undefined;
  const workflowCompletionViewModel: DesignStudioWorkflowCompletionViewModel = {
    state: workflowCompletion.state,
    checklist: workflowCompletion.checklist.map((item) => ({ ...item })),
    outstandingItems: [...workflowCompletion.outstandingItems],
    approvalsSatisfied: workflowCompletion.approvalsSatisfied.map((approvalKind) => buildApprovalCard(approvalKind, 'approved').title),
    deferredRecommendationCount: workflowCompletion.deferredRecommendationCount,
    unresolvedRecommendationCount: workflowCompletion.unresolvedRecommendationCount,
    nextStepGuidance: workflowCompletion.nextStepGuidance,
    completedAt: workflowCompletion.completedAt,
    completedBy: workflowCompletion.completedBy,
    reopenedAt: workflowCompletion.reopenedAt,
    reopenedBy: workflowCompletion.reopenedBy,
    canCompleteIteration: workflowCompletion.state !== 'completed' && workflowCompletion.isEligible,
    canReopenIteration: workflowCompletion.state === 'completed',
  };

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
      previewReview,
      executionReadiness,
      reviewDesign,
      refinementExperience: refinementExperienceViewModel,
      conceptReview,
      draftReview,
      workflowCompletion: workflowCompletionViewModel,
    },
  };
}
