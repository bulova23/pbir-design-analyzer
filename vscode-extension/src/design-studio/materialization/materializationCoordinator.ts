import { getSupportedAnalyzersForSurface } from '../../analyzer/analyzers/registry';
import type { MaterializationRequest } from '../contracts/designStudioModels';
import { mapMaterializedSurfaceCandidate } from './materializationMapper';

export interface MaterializationRequestValidationResult {
  ok: boolean;
  diagnostics: string[];
}

export interface MaterializationSideEffectState {
  analyzerHandoffExecuted: false;
  pbirFilesCreated: false;
  reportMutationOccurred: false;
  deliveryTriggered: false;
  providerExecutionTriggered: false;
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
  pbirFilesCreated: false,
  reportMutationOccurred: false,
  deliveryTriggered: false,
  providerExecutionTriggered: false,
};

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

  const candidate = mapMaterializedSurfaceCandidate(request);
  if (!candidate) {
    diagnostics.push(`Unsupported target surface family: ${String(request.targetSurfaceType)}.`);
  } else {
    const supportedAnalyzers = getSupportedAnalyzersForSurface(candidate.derivedSurface);
    const analyzerMatch = supportedAnalyzers.find((entry) => entry.analyzerType === request.targetAnalyzer);
    if (!analyzerMatch) {
      diagnostics.push(`Target analyzer ${request.targetAnalyzer} is not supported for ${request.targetSurfaceType}.`);
    } else if (!analyzerMatch.profiles.includes(request.targetAnalyzerProfile)) {
      diagnostics.push(`Target analyzer profile ${request.targetAnalyzerProfile} is not supported for ${request.targetAnalyzer}.`);
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
