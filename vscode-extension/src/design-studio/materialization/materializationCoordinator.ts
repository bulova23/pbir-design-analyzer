import type * as vscode from 'vscode';
import type { MaterializationRequest } from '../contracts/designStudioModels';
import {
  buildApprovedDraftPrimaryLineage,
  loadDraftState,
} from '../state/draftStore';
import { evaluateAnalyzerSurfaceCompatibility } from './analyzerSurfaceCompatibility';
import { mapMaterializedSurfaceCandidate } from './materializationMapper';

export interface MaterializationRequestValidationResult {
  ok: boolean;
  diagnostics: string[];
}

export interface MaterializationSideEffectState {
  analyzerHandoffExecuted: false;
  analyzerWorkspaceOpened: false;
  pbirFilesCreated: false;
  reportMutationOccurred: false;
  deliveryTriggered: false;
  providerExecutionTriggered: false;
}

export interface ApprovedDraftMaterializationRequestOptions {
  threadId: string;
  requestId: string;
  targetSurfaceType: MaterializationRequest['targetSurfaceType'];
  targetAnalyzer: MaterializationRequest['targetAnalyzer'];
  targetAnalyzerProfile: MaterializationRequest['targetAnalyzerProfile'];
  handoffContext?: MaterializationRequest['handoffContext'];
}

export type MaterializationGatewayResult =
  | {
    ok: true;
    candidate: NonNullable<ReturnType<typeof mapMaterializedSurfaceCandidate>>;
    diagnostics: string[];
    sideEffects: MaterializationSideEffectState;
  }
  | {
    ok: false;
    diagnostics: string[];
    sideEffects: MaterializationSideEffectState;
  };

const SIDE_EFFECT_STATE: MaterializationSideEffectState = {
  analyzerHandoffExecuted: false,
  analyzerWorkspaceOpened: false,
  pbirFilesCreated: false,
  reportMutationOccurred: false,
  deliveryTriggered: false,
  providerExecutionTriggered: false,
};

export async function createApprovedDraftMaterializationRequest(
  context: vscode.ExtensionContext,
  options: ApprovedDraftMaterializationRequestOptions,
): Promise<MaterializationRequest> {
  const draftState = await loadDraftState(context, options.threadId);
  if (!draftState) {
    throw new Error(`No Draft Studio state exists for thread ${options.threadId}.`);
  }

  const sourceLineage = buildApprovedDraftPrimaryLineage(draftState);
  const now = new Date().toISOString();

  return {
    id: options.requestId,
    threadId: options.threadId,
    kind: 'materializationRequest',
    materializationMode: 'draftToSurfaceCandidate',
    version: 1,
    lifecycleState: 'approved',
    approvalState: 'approved',
    approvalKind: 'materializationApproval',
    createdAt: now,
    updatedAt: now,
    authorSource: 'system',
    provenance: {
      source: 'system',
      timestamp: now,
      notes: ['Approved draft lineage was resolved from persisted Draft Studio state.'],
    },
    sourceArtifactIds: [draftState.currentDraft.id],
    sourceLineage,
    targetSurfaceType: options.targetSurfaceType,
    targetAnalyzer: options.targetAnalyzer,
    targetAnalyzerProfile: options.targetAnalyzerProfile,
    handoffContext: {
      repositoryBackedPath: options.handoffContext?.repositoryBackedPath,
      snapshotReference: options.handoffContext?.snapshotReference,
      degradedMappings: [...(options.handoffContext?.degradedMappings ?? [])],
      omittedEvidence: [...(options.handoffContext?.omittedEvidence ?? [])],
    },
  };
}

function isValidTimestamp(value: string | undefined): boolean {
  return typeof value === 'string' && !Number.isNaN(Date.parse(value));
}

function hasUniqueLineageEntries(request: MaterializationRequest): boolean {
  return new Set(request.sourceLineage.map((entry) => `${entry.artifactId}|${entry.artifactVersionId}|${entry.sourceRole}`)).size
    === request.sourceLineage.length;
}

function hasMatchingSourceArtifactIds(request: MaterializationRequest): boolean {
  const artifactIds = [...new Set(request.sourceArtifactIds)].sort((left, right) => left.localeCompare(right));
  const lineageIds = [...new Set(request.sourceLineage.map((entry) => entry.artifactId))].sort((left, right) => left.localeCompare(right));
  return artifactIds.length === lineageIds.length && artifactIds.every((artifactId, index) => artifactId === lineageIds[index]);
}

function hasValidModeLineage(request: MaterializationRequest): boolean {
  switch (request.materializationMode) {
    case 'conceptToStructurePreview':
      return request.sourceLineage.some((entry) =>
        ['reportConcept', 'pageConcept', 'navigationConcept', 'kpiHierarchyConcept'].includes(entry.artifactKind));
    case 'draftToSurfaceCandidate':
      return request.sourceLineage.some((entry) =>
        entry.artifactKind === 'draftReportArtifact' && entry.sourceRole === 'primary');
    case 'refinementProposalToCandidateComparison':
      return request.sourceLineage.some((entry) =>
        entry.artifactKind === 'refinementProposal' && entry.sourceRole === 'comparisonProposal')
        && request.sourceLineage.some((entry) =>
          entry.artifactKind === 'draftReportArtifact' && entry.sourceRole === 'comparisonBase');
    default:
      return false;
  }
}

function hasValidHandoffContext(request: MaterializationRequest): boolean {
  const context = request.handoffContext;
  if (!context) {
    return false;
  }

  if (!Array.isArray(context.degradedMappings) || !context.degradedMappings.every((entry) => typeof entry === 'string')) {
    return false;
  }

  if (!Array.isArray(context.omittedEvidence) || !context.omittedEvidence.every((entry) => typeof entry === 'string')) {
    return false;
  }

  if (!context.snapshotReference) {
    return true;
  }

  return typeof context.snapshotReference.snapshotId === 'string'
    && context.snapshotReference.snapshotId.trim().length > 0
    && typeof context.snapshotReference.rootPath === 'string'
    && context.snapshotReference.rootPath.trim().length > 0
    && typeof context.snapshotReference.sourceLocation === 'string'
    && context.snapshotReference.sourceLocation.trim().length > 0;
}

export function validateMaterializationRequestSemantics(
  request: MaterializationRequest,
): MaterializationRequestValidationResult {
  const diagnostics: string[] = [];

  if (request.approvalKind !== 'materializationApproval') {
    diagnostics.push('Materialization request approvalKind must be materializationApproval.');
  }
  if (request.lifecycleState !== 'approved') {
    diagnostics.push('Materialization request lifecycleState must be approved.');
  }
  if (request.approvalState !== 'approved') {
    diagnostics.push('Materialization request approvalState must be approved.');
  }
  if (!Number.isInteger(request.version) || request.version <= 0) {
    diagnostics.push('Materialization request version must be positive.');
  }
  if (!isValidTimestamp(request.createdAt)) {
    diagnostics.push('Materialization request createdAt must be a valid timestamp.');
  }
  if (!isValidTimestamp(request.updatedAt)) {
    diagnostics.push('Materialization request updatedAt must be a valid timestamp.');
  }
  if (request.provenance.timestamp && !isValidTimestamp(request.provenance.timestamp)) {
    diagnostics.push('Materialization request provenance timestamp must be a valid timestamp when present.');
  }
  if (request.targetAnalyzer.trim().length === 0) {
    diagnostics.push('Materialization request targetAnalyzer must be non-empty.');
  }
  if (request.targetAnalyzerProfile.trim().length === 0) {
    diagnostics.push('Materialization request targetAnalyzerProfile must be non-empty.');
  }
  if (request.sourceArtifactIds.length === 0 || new Set(request.sourceArtifactIds).size !== request.sourceArtifactIds.length) {
    diagnostics.push('Materialization request sourceArtifactIds must be unique and non-empty.');
  }
  if (!hasUniqueLineageEntries(request)) {
    diagnostics.push('Materialization request sourceLineage entries must be unique.');
  }
  if (!request.sourceLineage.every((entry) => isValidTimestamp(entry.approvalTimestamp))) {
    diagnostics.push('Materialization request sourceLineage approval timestamps must be valid.');
  }
  if (!request.sourceLineage.every((entry) => entry.approvalState === 'approved')) {
    diagnostics.push('Materialization request sourceLineage entries must reference approved source artifacts.');
  }
  if (!hasMatchingSourceArtifactIds(request)) {
    diagnostics.push('Materialization request sourceArtifactIds must correspond exactly to sourceLineage artifactIds.');
  }
  if (!hasValidModeLineage(request)) {
    diagnostics.push(`Materialization request sourceLineage does not satisfy ${request.materializationMode} requirements.`);
  }
  if (!hasValidHandoffContext(request)) {
    diagnostics.push('Materialization request handoffContext must provide string-only degradation/evidence diagnostics and valid snapshot metadata when present.');
  }

  const candidate = mapMaterializedSurfaceCandidate(request);
  if (!candidate) {
    diagnostics.push(`Unsupported target surface family: ${String(request.targetSurfaceType)}.`);
  } else {
    const compatibility = evaluateAnalyzerSurfaceCompatibility(
      candidate.derivedSurface,
      request.targetAnalyzer,
      request.targetAnalyzerProfile,
    );
    if (!compatibility.ok) {
      diagnostics.push(...compatibility.diagnostics);
    }
  }

  return diagnostics.length > 0
    ? { ok: false, diagnostics }
    : { ok: true, diagnostics: [] };
}

export function materializeDesignStudioRequest(request: MaterializationRequest): MaterializationGatewayResult {
  const validation = validateMaterializationRequestSemantics(request);
  if (!validation.ok) {
    return {
      ok: false,
      diagnostics: validation.diagnostics,
      sideEffects: SIDE_EFFECT_STATE,
    };
  }

  const candidate = mapMaterializedSurfaceCandidate(request);
  if (!candidate) {
    return {
      ok: false,
      diagnostics: [`Unsupported target surface family: ${String(request.targetSurfaceType)}.`],
      sideEffects: SIDE_EFFECT_STATE,
    };
  }

  return {
    ok: true,
    candidate,
    diagnostics: candidate.materializationDiagnostics,
    sideEffects: SIDE_EFFECT_STATE,
  };
}
