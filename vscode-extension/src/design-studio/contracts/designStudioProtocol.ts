import {
  DESIGN_STUDIO_ARTIFACT_KINDS,
  DESIGN_STUDIO_APPROVAL_STATES,
  DESIGN_STUDIO_MATERIALIZATION_MODES,
  DESIGN_STUDIO_SOURCE_ROLES,
} from './designStudioModels';
import {
  DESIGN_STUDIO_WORKFLOW_STAGE_IDS,
  DESIGN_STUDIO_WORKFLOW_STAGE_STATUSES,
} from './designStudioShell';
import type {
  DesignBrief,
  DesignIterationRecord,
  DesignStudioArtifactKind,
  MaterializationRequest,
  RefinementProposal,
} from './designStudioModels';
import type { DesignStudioWorkspaceViewModel } from './designStudioShell';

const MATERIALIZATION_TARGET_SURFACE_TYPES = ['pbirReport', 'fabricApp', 'screenshotBundle'] as const;
const MATERIALIZATION_TARGET_ANALYZERS = ['pbirDesignReview', 'fabricAppReadiness', 'fabricAppReview'] as const;
const MATERIALIZATION_TARGET_PROFILES = [
  'default',
  'migrationReadiness',
  'executive',
  'consultant',
  'governance',
  'accessibility',
  'fabricAppQuality',
] as const;

export const DESIGN_STUDIO_PROTOCOL_VERSION = 1;
export const DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION = 1;

export const DESIGN_STUDIO_HOST_MESSAGE_TYPES = [
  'studioState',
  'artifactSaved',
  'artifactProposed',
  'artifactApproved',
  'materializationRequested',
  'iterationComparison',
  'analyzerHandoffOpened',
] as const;

export const DESIGN_STUDIO_WEBVIEW_MESSAGE_TYPES = [
  'webviewReady',
  'loadStudioState',
  'saveArtifact',
  'proposeArtifact',
  'approveArtifact',
  'requestMaterialization',
  'compareIterations',
  'openAnalyzerHandoff',
  'setRefinementProposalState',
] as const;

export type DesignStudioHostMessageType = typeof DESIGN_STUDIO_HOST_MESSAGE_TYPES[number];
export type DesignStudioWebviewMessageType = typeof DESIGN_STUDIO_WEBVIEW_MESSAGE_TYPES[number];

interface DesignStudioEnvelope {
  protocolVersion: typeof DESIGN_STUDIO_PROTOCOL_VERSION;
  schemaVersion: typeof DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readString(value: Record<string, unknown>, key: string): string | undefined {
  return typeof value[key] === 'string' ? value[key] as string : undefined;
}

function readNumber(value: Record<string, unknown>, key: string): number | undefined {
  return typeof value[key] === 'number' ? value[key] as number : undefined;
}

function readBoolean(value: Record<string, unknown>, key: string): boolean | undefined {
  return typeof value[key] === 'boolean' ? value[key] as boolean : undefined;
}

function isArtifactKind(value: string): value is DesignStudioArtifactKind {
  return (DESIGN_STUDIO_ARTIFACT_KINDS as readonly string[]).includes(value);
}

function isApprovalState(value: string): boolean {
  return (DESIGN_STUDIO_APPROVAL_STATES as readonly string[]).includes(value);
}

function isMaterializationMode(value: string): boolean {
  return (DESIGN_STUDIO_MATERIALIZATION_MODES as readonly string[]).includes(value);
}

function isSourceRole(value: string): boolean {
  return (DESIGN_STUDIO_SOURCE_ROLES as readonly string[]).includes(value);
}

function isArtifactId(value: string): boolean {
  return value.trim().length > 0 && value.includes(':');
}

function isArtifactVersionId(value: string): boolean {
  return /^[^@\s]+@v\d+$/.test(value);
}

function isNonEmptyStringArray(value: unknown): value is string[] {
  return Array.isArray(value)
    && value.length > 0
    && value.every((entry) => typeof entry === 'string' && entry.trim().length > 0);
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((entry) => typeof entry === 'string');
}

function isMaterializationSurfaceType(value: string): boolean {
  return (MATERIALIZATION_TARGET_SURFACE_TYPES as readonly string[]).includes(value);
}

function isMaterializationAnalyzer(value: string): boolean {
  return (MATERIALIZATION_TARGET_ANALYZERS as readonly string[]).includes(value);
}

function isMaterializationProfile(value: string): boolean {
  return (MATERIALIZATION_TARGET_PROFILES as readonly string[]).includes(value);
}

function isSourceLineage(value: unknown): boolean {
  return Array.isArray(value) && value.every((entry) => {
    if (!isRecord(entry)) {
      return false;
    }

    const artifactId = readString(entry, 'artifactId');
    const artifactKind = readString(entry, 'artifactKind');
    const artifactVersionId = readString(entry, 'artifactVersionId');
    const sourceRole = readString(entry, 'sourceRole');
    const approvalState = readString(entry, 'approvalState');
    const approvalTimestamp = readString(entry, 'approvalTimestamp');

    return !!artifactId
      && isArtifactId(artifactId)
      && !!artifactKind
      && isArtifactKind(artifactKind)
      && !!artifactVersionId
      && isArtifactVersionId(artifactVersionId)
      && !!sourceRole
      && isSourceRole(sourceRole)
      && !!approvalState
      && isApprovalState(approvalState)
      && !!approvalTimestamp;
  });
}

function isValidTimestamp(value: unknown): boolean {
  return typeof value === 'string' && !Number.isNaN(Date.parse(value));
}

function hasUniqueSourceArtifactIds(value: string[]): boolean {
  return value.length > 0 && new Set(value).size === value.length;
}

function hasMatchingSourceArtifactIds(
  artifactIds: string[],
  sourceLineage: MaterializationRequest['sourceLineage'],
): boolean {
  const normalizedArtifactIds = [...new Set(artifactIds)].sort((left, right) => left.localeCompare(right));
  const lineageIds = [...new Set(sourceLineage.map((entry) => entry.artifactId))].sort((left, right) => left.localeCompare(right));

  return normalizedArtifactIds.length === lineageIds.length
    && normalizedArtifactIds.every((artifactId, index) => artifactId === lineageIds[index]);
}

function hasUniqueSourceLineage(
  sourceLineage: MaterializationRequest['sourceLineage'],
): boolean {
  return new Set(sourceLineage.map((entry) => `${entry.artifactId}|${entry.artifactVersionId}|${entry.sourceRole}`)).size
    === sourceLineage.length;
}

function hasValidModeLineage(
  materializationMode: MaterializationRequest['materializationMode'],
  sourceLineage: MaterializationRequest['sourceLineage'],
): boolean {
  switch (materializationMode) {
    case 'conceptToStructurePreview':
      return sourceLineage.some((entry) =>
        ['reportConcept', 'pageConcept', 'navigationConcept', 'kpiHierarchyConcept'].includes(entry.artifactKind));
    case 'draftToSurfaceCandidate':
      return sourceLineage.some((entry) =>
        entry.artifactKind === 'draftReportArtifact' && entry.sourceRole === 'primary');
    case 'refinementProposalToCandidateComparison':
      return sourceLineage.some((entry) =>
        entry.artifactKind === 'refinementProposal' && entry.sourceRole === 'comparisonProposal')
        && sourceLineage.some((entry) =>
          entry.artifactKind === 'draftReportArtifact' && entry.sourceRole === 'comparisonBase');
    default:
      return false;
  }
}

function isMaterializationRequestPayload(value: unknown): value is MaterializationRequest {
  if (!isRecord(value)) {
    return false;
  }

  const artifactIds = Array.isArray(value.sourceArtifactIds) ? value.sourceArtifactIds : undefined;
  const materializationMode = readString(value, 'materializationMode');
  const targetSurfaceType = readString(value, 'targetSurfaceType');
  const targetAnalyzer = readString(value, 'targetAnalyzer');
  const targetAnalyzerProfile = readString(value, 'targetAnalyzerProfile');
  const handoffContext = isRecord(value.handoffContext) ? value.handoffContext : undefined;
  const degradedMappings = Array.isArray(handoffContext?.degradedMappings) ? handoffContext.degradedMappings : undefined;
  const omittedEvidence = Array.isArray(handoffContext?.omittedEvidence) ? handoffContext.omittedEvidence : undefined;
  const snapshotReference = isRecord(handoffContext?.snapshotReference) ? handoffContext.snapshotReference : undefined;
  const hasBaseShape = typeof value.id === 'string'
    && isArtifactId(value.id)
    && typeof value.threadId === 'string'
    && typeof value.kind === 'string'
    && value.kind === 'materializationRequest'
    && !!materializationMode
    && isMaterializationMode(materializationMode)
    && typeof value.version === 'number'
    && typeof value.lifecycleState === 'string'
    && typeof value.approvalState === 'string'
    && isApprovalState(value.approvalState)
    && isRecord(value.provenance)
    && Array.isArray(artifactIds)
    && artifactIds.length > 0
    && artifactIds.every((artifactId) => typeof artifactId === 'string' && isArtifactId(artifactId))
    && isSourceLineage(value.sourceLineage)
    && !!targetSurfaceType
    && isMaterializationSurfaceType(targetSurfaceType)
    && !!targetAnalyzer
    && isMaterializationAnalyzer(targetAnalyzer)
    && !!targetAnalyzerProfile
    && isMaterializationProfile(targetAnalyzerProfile)
    && !!handoffContext
    && Array.isArray(degradedMappings)
    && degradedMappings.every((entry) => typeof entry === 'string')
    && Array.isArray(omittedEvidence)
    && omittedEvidence.every((entry) => typeof entry === 'string')
    && (
      !snapshotReference
      || (
        typeof snapshotReference.snapshotId === 'string'
        && typeof snapshotReference.rootPath === 'string'
        && typeof snapshotReference.sourceLocation === 'string'
      )
    );

  if (!hasBaseShape) {
    return false;
  }

  const request = value as unknown as MaterializationRequest;

  return request.approvalKind === 'materializationApproval'
    && request.lifecycleState === 'approved'
    && request.approvalState === 'approved'
    && Number.isInteger(request.version)
    && request.version > 0
    && isValidTimestamp(request.createdAt)
    && isValidTimestamp(request.updatedAt)
    && (!request.provenance.timestamp || isValidTimestamp(request.provenance.timestamp))
    && hasUniqueSourceArtifactIds(request.sourceArtifactIds)
    && hasUniqueSourceLineage(request.sourceLineage)
    && request.sourceLineage.every((entry) => entry.approvalState === 'approved' && isValidTimestamp(entry.approvalTimestamp))
    && hasMatchingSourceArtifactIds(request.sourceArtifactIds, request.sourceLineage)
    && hasValidModeLineage(request.materializationMode, request.sourceLineage);
}

function isNestedCurrentBrief(value: unknown, threadId: string): boolean {
  if (!isRecord(value)) {
    return false;
  }

  const briefThreadId = readString(value, 'threadId');
  const briefId = readString(value, 'id');
  const briefKind = readString(value, 'kind');

  return !!briefThreadId
    && briefThreadId === threadId
    && !!briefId
    && isArtifactId(briefId)
    && (briefKind === undefined || briefKind === 'designBrief');
}

function isIterationValidationApproval(value: unknown): boolean {
  if (!isRecord(value)) {
    return false;
  }

  return readString(value, 'approvalKind') === 'validationApproval'
    && !!readString(value, 'approvalState')
    && isApprovalState(readString(value, 'approvalState')!);
}

function isIterationApprovalCheckpoint(value: unknown): boolean {
  if (!isRecord(value)) {
    return false;
  }

  return isIterationValidationApproval(value.validationApproval);
}

function isIterationGuardrails(value: unknown): boolean {
  if (!isRecord(value)) {
    return false;
  }

  return readBoolean(value, 'autoOptimizationTriggered') !== undefined
    && readBoolean(value, 'analyzerExecutionTriggered') !== undefined
    && readBoolean(value, 'reportMutationTriggered') !== undefined
    && readBoolean(value, 'pbirFilesGenerated') !== undefined;
}

function isNestedIterationRecord(value: unknown, threadId: string): boolean {
  if (!isRecord(value)) {
    return false;
  }

  return readString(value, 'threadId') === threadId
    && readString(value, 'kind') === 'designIterationRecord'
    && !!readString(value, 'id')
    && isArtifactId(readString(value, 'id')!)
    && isNonEmptyStringArray(value.sourceArtifactVersionIds)
    && value.sourceArtifactVersionIds.every((entry) => isArtifactVersionId(entry))
    && isIterationApprovalCheckpoint(value.approvalCheckpoint)
    && isIterationGuardrails(value.guardrails);
}

function isNestedRefinementProposal(value: unknown, threadId: string): boolean {
  if (!isRecord(value)) {
    return false;
  }

  const proposalThreadId = readString(value, 'threadId');
  const proposalId = readString(value, 'id');
  const proposalKind = readString(value, 'kind');

  return !!proposalThreadId
    && proposalThreadId === threadId
    && !!proposalId
    && isArtifactId(proposalId)
    && proposalKind === 'refinementProposal';
}

function isWorkflowStageId(value: string): boolean {
  return (DESIGN_STUDIO_WORKFLOW_STAGE_IDS as readonly string[]).includes(value);
}

function isWorkflowStageStatus(value: string): boolean {
  return (DESIGN_STUDIO_WORKFLOW_STAGE_STATUSES as readonly string[]).includes(value);
}

function isWorkspaceStatePayload(value: unknown): value is DesignStudioWorkspaceViewModel {
  if (!isRecord(value)) {
    return false;
  }

  const reportLabel = readString(value, 'reportLabel');
  const currentStage = readString(value, 'currentStage');
  const currentStageSummary = isRecord(value.currentStageSummary) ? value.currentStageSummary : undefined;
  const stages = Array.isArray(value.stages) ? value.stages : undefined;
  const approvalCards = Array.isArray(value.approvalCards) ? value.approvalCards : undefined;

  if (!reportLabel || !currentStage || !isWorkflowStageId(currentStage) || !currentStageSummary || !stages || !approvalCards) {
    return false;
  }

  if (
    typeof currentStageSummary.title !== 'string'
    || typeof currentStageSummary.description !== 'string'
  ) {
    return false;
  }

  if (!stages.every((stage) => {
    if (!isRecord(stage)) {
      return false;
    }

    const id = readString(stage, 'id');
    const label = readString(stage, 'label');
    const status = readString(stage, 'status');
    const readinessLabel = readString(stage, 'readinessLabel');
    const title = readString(stage, 'title');
    const description = readString(stage, 'description');

    return !!id
      && isWorkflowStageId(id)
      && !!label
      && !!status
      && isWorkflowStageStatus(status)
      && !!readinessLabel
      && !!title
      && !!description;
  })) {
    return false;
  }

  if (!approvalCards.every((card) => {
    if (!isRecord(card)) {
      return false;
    }

    const kind = readString(card, 'kind');
    const title = readString(card, 'title');
    const approvalState = readString(card, 'approvalState');
    const owner = readString(card, 'owner');
    const unlock = readString(card, 'unlock');

    return !!kind
      && ['designApproval', 'materializationApproval', 'refinementApproval', 'validationApproval'].includes(kind)
      && !!title
      && !!approvalState
      && isApprovalState(approvalState)
      && !!owner
      && !!unlock
      && isStringArray(card.nonEffects);
  })) {
    return false;
  }

  if (value.materializationReadiness !== undefined) {
    const readiness = isRecord(value.materializationReadiness) ? value.materializationReadiness : undefined;
    const executableEligibility = readiness ? readString(readiness, 'executableEligibility') : undefined;

    if (
      !readiness
      || !readString(readiness, 'readinessLabel')
      || !executableEligibility
      || !['executable', 'nonExecutablePreview', 'unsupported'].includes(executableEligibility)
      || !readString(readiness, 'targetAnalyzer')
      || !readString(readiness, 'targetAnalyzerProfile')
      || !isStringArray(readiness.diagnostics)
    ) {
      return false;
    }
  }

  if (value.analyzerHandoff !== undefined) {
    const handoff = isRecord(value.analyzerHandoff) ? value.analyzerHandoff : undefined;
    if (
      !handoff
      || !readString(handoff, 'requestId')
      || !readString(handoff, 'readinessLabel')
      || !readString(handoff, 'analyzerId')
      || !readString(handoff, 'analyzerProfileId')
      || readBoolean(handoff, 'canOpen') === undefined
      || !isStringArray(handoff.diagnostics)
    ) {
      return false;
    }
  }

  if (value.refinementExperience !== undefined) {
    const experience = isRecord(value.refinementExperience) ? value.refinementExperience : undefined;
    const groups = Array.isArray(experience?.groups) ? experience.groups : undefined;
    if (
      !experience
      || !readString(experience, 'title')
      || !readString(experience, 'summary')
      || !groups
      || (experience.emptyState !== undefined && !readString(experience, 'emptyState'))
    ) {
      return false;
    }

    const validGroupIds = ['story', 'layout', 'kpi', 'navigation', 'structure'];
    const validActions = ['approve', 'reject', 'defer'];
    if (!groups.every((group) => {
      if (!isRecord(group)) {
        return false;
      }

      const proposals = Array.isArray(group.proposals) ? group.proposals : undefined;
      if (
        !readString(group, 'id')
        || !validGroupIds.includes(readString(group, 'id')!)
        || !readString(group, 'title')
        || !readString(group, 'summary')
        || !proposals
      ) {
        return false;
      }

      return proposals.every((proposal) => {
        if (!isRecord(proposal)) {
          return false;
        }

        const comparison = isRecord(proposal.comparison) ? proposal.comparison : undefined;
        const availableActions = Array.isArray(proposal.availableActions) ? proposal.availableActions : undefined;
        const approvalState = readString(proposal, 'approvalState');

        return !!readString(proposal, 'id')
          && !!readString(proposal, 'title')
          && !!readString(proposal, 'summary')
          && !!readString(proposal, 'recommendation')
          && !!readString(proposal, 'rationale')
          && !!readString(proposal, 'expectedImpact')
          && !!approvalState
          && isApprovalState(approvalState)
          && !!readString(proposal, 'sourceAnalyzerLabel')
          && isStringArray(proposal.affectedArtifacts)
          && isStringArray(proposal.supportingEvidence)
          && !!comparison
          && !!readString(comparison, 'originalDesignIntent')
          && !!readString(comparison, 'currentDesignState')
          && !!readString(comparison, 'proposedRefinement')
          && !!availableActions
          && availableActions.every((action) => typeof action === 'string' && validActions.includes(action));
      });
    })) {
      return false;
    }
  }

  if (value.conceptReview !== undefined) {
    const conceptReview = isRecord(value.conceptReview) ? value.conceptReview : undefined;
    const chapterStructure = Array.isArray(conceptReview?.chapterStructure) ? conceptReview.chapterStructure : undefined;
    const kpiHierarchy = Array.isArray(conceptReview?.kpiHierarchy) ? conceptReview.kpiHierarchy : undefined;
    const navigationStructure = Array.isArray(conceptReview?.navigationStructure) ? conceptReview.navigationStructure : undefined;
    const analyticalFlow = Array.isArray(conceptReview?.analyticalFlow) ? conceptReview.analyticalFlow : undefined;

    if (
      !conceptReview
      || !readString(conceptReview, 'title')
      || !readString(conceptReview, 'summary')
      || !readString(conceptReview, 'selectedConceptLabel')
      || !chapterStructure
      || !kpiHierarchy
      || !navigationStructure
      || !analyticalFlow
    ) {
      return false;
    }

    if (!chapterStructure.every((chapter) => isRecord(chapter) && !!readString(chapter, 'title') && !!readString(chapter, 'objective'))) {
      return false;
    }

    if (!kpiHierarchy.every((node) => {
      if (!isRecord(node)) {
        return false;
      }

      const level = readString(node, 'level');
      return !!readString(node, 'label')
        && !!level
        && ['primary', 'supporting', 'diagnostic'].includes(level)
        && readNumber(node, 'depth') !== undefined;
    })) {
      return false;
    }

    if (!navigationStructure.every((node) => isRecord(node) && !!readString(node, 'label') && readNumber(node, 'depth') !== undefined)) {
      return false;
    }

    if (!analyticalFlow.every((step) => isRecord(step) && !!readString(step, 'label') && !!readString(step, 'objective'))) {
      return false;
    }
  }

  if (value.draftReview !== undefined) {
    const draftReview = isRecord(value.draftReview) ? value.draftReview : undefined;
    const draftPages = Array.isArray(draftReview?.draftPages) ? draftReview.draftPages : undefined;
    const draftLayouts = Array.isArray(draftReview?.draftLayouts) ? draftReview.draftLayouts : undefined;
    const draftNavigation = Array.isArray(draftReview?.draftNavigation) ? draftReview.draftNavigation : undefined;

    if (
      !draftReview
      || !readString(draftReview, 'title')
      || !readString(draftReview, 'summary')
      || !readString(draftReview, 'draftStatusLabel')
      || !draftPages
      || !draftLayouts
      || !draftNavigation
    ) {
      return false;
    }

    if (!draftPages.every((page) => isRecord(page) && !!readString(page, 'title') && !!readString(page, 'structureSummary') && isStringArray(page.kpiPlacement))) {
      return false;
    }

    if (!draftLayouts.every((layout) => isRecord(layout) && !!readString(layout, 'title') && !!readString(layout, 'layoutType') && isStringArray(layout.zones))) {
      return false;
    }

    if (!draftNavigation.every((item) => isRecord(item) && !!readString(item, 'label') && !!readString(item, 'pageTitle'))) {
      return false;
    }
  }

  return true;
}

function isStudioStatePayload(value: unknown): value is DesignStudioStudioState {
  if (!isRecord(value)) {
    return false;
  }

  const threadId = readString(value, 'threadId');
  if (!threadId) {
    return false;
  }

  const currentBrief = value.currentBrief;
  const iterationHistory = value.iterationHistory;
  const pendingRefinementProposals = value.pendingRefinementProposals;

  if (currentBrief !== undefined && !isNestedCurrentBrief(currentBrief, threadId)) {
    return false;
  }

  if (!Array.isArray(iterationHistory) || !iterationHistory.every((entry) => isNestedIterationRecord(entry, threadId))) {
    return false;
  }

  if (!Array.isArray(pendingRefinementProposals)
    || !pendingRefinementProposals.every((entry) => isNestedRefinementProposal(entry, threadId))) {
    return false;
  }

  if (value.workspace !== undefined && !isWorkspaceStatePayload(value.workspace)) {
    return false;
  }

  return true;
}

function hasCompatibleEnvelope(value: Record<string, unknown>): boolean {
  return value.protocolVersion === DESIGN_STUDIO_PROTOCOL_VERSION
    && value.schemaVersion === DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION;
}

function buildVersionMismatchMessage(protocolVersion: unknown, schemaVersion: unknown): string {
  return `Design Studio protocol mismatch. Expected protocol ${DESIGN_STUDIO_PROTOCOL_VERSION} / schema ${DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION}, received protocol ${String(protocolVersion)} / schema ${String(schemaVersion)}.`;
}

export interface DesignStudioStudioState {
  threadId: string;
  currentBrief?: DesignBrief;
  iterationHistory: DesignIterationRecord[];
  pendingRefinementProposals: RefinementProposal[];
  workspace?: DesignStudioWorkspaceViewModel;
}

export type DesignStudioHostToWebviewMessagePayload =
  | (DesignStudioEnvelope & { type: 'studioState'; state: DesignStudioStudioState })
  | (DesignStudioEnvelope & { type: 'artifactSaved'; artifactKind: DesignStudioArtifactKind; artifactId: string; version: number })
  | (DesignStudioEnvelope & { type: 'artifactProposed'; artifactKind: DesignStudioArtifactKind; artifactId: string })
  | (DesignStudioEnvelope & { type: 'artifactApproved'; artifactKind: DesignStudioArtifactKind; artifactId: string; version: number })
  | (DesignStudioEnvelope & { type: 'materializationRequested'; request: MaterializationRequest })
  | (DesignStudioEnvelope & { type: 'iterationComparison'; iterationId: string; summary: string })
  | (DesignStudioEnvelope & { type: 'analyzerHandoffOpened'; requestId: string; target: 'analyzerWorkspace' });

export type DesignStudioWebviewToHostMessagePayload =
  | (DesignStudioEnvelope & { type: 'webviewReady' })
  | (DesignStudioEnvelope & { type: 'loadStudioState'; threadId: string })
  | (DesignStudioEnvelope & { type: 'saveArtifact'; artifactKind: DesignStudioArtifactKind; artifact: unknown })
  | (DesignStudioEnvelope & { type: 'proposeArtifact'; artifactKind: DesignStudioArtifactKind; artifactId: string })
  | (DesignStudioEnvelope & { type: 'approveArtifact'; artifactKind: DesignStudioArtifactKind; artifactId: string })
  | (DesignStudioEnvelope & { type: 'requestMaterialization'; request: MaterializationRequest })
  | (DesignStudioEnvelope & { type: 'compareIterations'; baseIterationId: string; candidateIterationId: string })
  | (DesignStudioEnvelope & { type: 'openAnalyzerHandoff'; requestId: string })
  | (DesignStudioEnvelope & { type: 'setRefinementProposalState'; proposalId: string; action: 'approve' | 'reject' | 'defer' });

export function withDesignStudioEnvelope<T extends { type: string }>(message: T): T & DesignStudioEnvelope {
  return {
    ...message,
    protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
    schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
  };
}

export function parseDesignStudioHostMessage(value: unknown):
  | { ok: true; message: DesignStudioHostToWebviewMessagePayload }
  | { ok: false; error: string } {
  if (!isRecord(value)) {
    return { ok: false, error: 'Design Studio host message must be an object.' };
  }

  if (!hasCompatibleEnvelope(value)) {
    return {
      ok: false,
      error: buildVersionMismatchMessage(value.protocolVersion, value.schemaVersion),
    };
  }

  const type = readString(value, 'type');
  if (!type) {
    return { ok: false, error: 'Design Studio host message is missing a type.' };
  }

  switch (type) {
    case 'studioState':
      return isStudioStatePayload(value.state)
        ? { ok: true, message: withDesignStudioEnvelope({ type, state: value.state as unknown as DesignStudioStudioState }) }
        : { ok: false, error: 'Design Studio studioState host message has an invalid nested state payload.' };
    case 'artifactSaved':
    case 'artifactApproved': {
      const artifactKind = readString(value, 'artifactKind');
      const artifactId = readString(value, 'artifactId');
      const version = readNumber(value, 'version');
      return artifactKind && isArtifactKind(artifactKind) && artifactId && version !== undefined
        ? { ok: true, message: withDesignStudioEnvelope({ type, artifactKind, artifactId, version }) }
        : { ok: false, error: `Design Studio ${type} host message is missing required fields.` };
    }
    case 'artifactProposed': {
      const artifactKind = readString(value, 'artifactKind');
      const artifactId = readString(value, 'artifactId');
      return artifactKind && isArtifactKind(artifactKind) && artifactId
        ? { ok: true, message: withDesignStudioEnvelope({ type, artifactKind, artifactId }) }
        : { ok: false, error: 'Design Studio artifactProposed host message is missing required fields.' };
    }
    case 'materializationRequested':
      return isMaterializationRequestPayload(value.request)
        ? { ok: true, message: withDesignStudioEnvelope({ type, request: value.request as unknown as MaterializationRequest }) }
        : { ok: false, error: 'Design Studio materializationRequested host message has an invalid request payload.' };
    case 'iterationComparison': {
      const iterationId = readString(value, 'iterationId');
      const summary = readString(value, 'summary');
      return iterationId && summary
        ? { ok: true, message: withDesignStudioEnvelope({ type, iterationId, summary }) }
        : { ok: false, error: 'Design Studio iterationComparison host message is missing required fields.' };
    }
    case 'analyzerHandoffOpened': {
      const requestId = readString(value, 'requestId');
      const target = readString(value, 'target');
      return requestId && target === 'analyzerWorkspace'
        ? { ok: true, message: withDesignStudioEnvelope({ type, requestId, target }) }
        : { ok: false, error: 'Design Studio analyzerHandoffOpened host message is missing required fields.' };
    }
    default:
      return { ok: false, error: `Unsupported Design Studio host message type: ${type}.` };
  }
}

export function parseDesignStudioWebviewMessage(value: unknown):
  | { ok: true; message: DesignStudioWebviewToHostMessagePayload }
  | { ok: false; error: string } {
  if (!isRecord(value)) {
    return { ok: false, error: 'Design Studio webview message must be an object.' };
  }

  if (!hasCompatibleEnvelope(value)) {
    return {
      ok: false,
      error: buildVersionMismatchMessage(value.protocolVersion, value.schemaVersion),
    };
  }

  const type = readString(value, 'type');
  if (!type) {
    return { ok: false, error: 'Design Studio webview message is missing a type.' };
  }

  switch (type) {
    case 'webviewReady':
      return { ok: true, message: withDesignStudioEnvelope({ type }) };
    case 'loadStudioState': {
      const threadId = readString(value, 'threadId');
      return threadId
        ? { ok: true, message: withDesignStudioEnvelope({ type, threadId }) }
        : { ok: false, error: 'Design Studio loadStudioState webview message is missing threadId.' };
    }
    case 'saveArtifact': {
      const artifactKind = readString(value, 'artifactKind');
      return artifactKind && isArtifactKind(artifactKind) && 'artifact' in value
        ? { ok: true, message: withDesignStudioEnvelope({ type, artifactKind, artifact: value.artifact }) }
        : { ok: false, error: 'Design Studio saveArtifact webview message is missing required fields.' };
    }
    case 'proposeArtifact':
    case 'approveArtifact': {
      const artifactKind = readString(value, 'artifactKind');
      const artifactId = readString(value, 'artifactId');
      return artifactKind && isArtifactKind(artifactKind) && artifactId
        ? { ok: true, message: withDesignStudioEnvelope({ type, artifactKind, artifactId }) }
        : { ok: false, error: `Design Studio ${type} webview message is missing required fields.` };
    }
    case 'requestMaterialization':
      return isMaterializationRequestPayload(value.request)
        ? { ok: true, message: withDesignStudioEnvelope({ type, request: value.request as unknown as MaterializationRequest }) }
        : { ok: false, error: 'Design Studio requestMaterialization webview message has an invalid request payload.' };
    case 'compareIterations': {
      const baseIterationId = readString(value, 'baseIterationId');
      const candidateIterationId = readString(value, 'candidateIterationId');
      return baseIterationId && candidateIterationId
        ? { ok: true, message: withDesignStudioEnvelope({ type, baseIterationId, candidateIterationId }) }
        : { ok: false, error: 'Design Studio compareIterations webview message is missing required fields.' };
    }
    case 'openAnalyzerHandoff': {
      const requestId = readString(value, 'requestId');
      return requestId
        ? { ok: true, message: withDesignStudioEnvelope({ type, requestId }) }
        : { ok: false, error: 'Design Studio openAnalyzerHandoff webview message is missing requestId.' };
    }
    case 'setRefinementProposalState': {
      const proposalId = readString(value, 'proposalId');
      const action = readString(value, 'action');
      return proposalId && (action === 'approve' || action === 'reject' || action === 'defer')
        ? { ok: true, message: withDesignStudioEnvelope({ type, proposalId, action }) }
        : { ok: false, error: 'Design Studio setRefinementProposalState webview message is missing required fields.' };
    }
    default:
      return { ok: false, error: `Unsupported Design Studio webview message type: ${type}.` };
  }
}
