import type {
  MaterializationAnalyzerHandoffMetadata,
  MaterializationMode,
  MaterializationProvenanceEntry,
  MaterializationRequest,
  MaterializedSurfaceCandidate,
} from '../contracts/designStudioModels';
import type { AnalyzableSurface } from '../../analyzer/surfaces/types';

function buildDerivedSurface(request: MaterializationRequest): AnalyzableSurface | undefined {
  switch (request.targetSurfaceType) {
    case 'pbirReport':
      return {
        surfaceType: 'pbirReport',
        displayName: `Design Studio candidate for ${request.materializationMode}`,
        sourceLocation: `design-studio://materialization/${request.threadId}/${request.id}`,
        availableEvidenceKinds: ['pbirMetadata', 'interaction', 'navigation', 'semanticModel', 'portability'],
        availableAnalyzerTypes: ['pbirDesignReview', 'fabricAppReadiness'],
        availableAnalyzerProfiles: ['default', 'migrationReadiness'],
        analysisCapabilities: ['findings', 'evidence', 'remediation', 'governanceSignals'],
        governanceCapabilities: ['analytics'],
      };
    case 'fabricApp':
      return {
        surfaceType: 'fabricApp',
        displayName: `Design Studio candidate for ${request.materializationMode}`,
        sourceLocation: `design-studio://materialization/${request.threadId}/${request.id}`,
        availableEvidenceKinds: ['typescriptLayout', 'navigation', 'designToken'],
        availableAnalyzerTypes: ['fabricAppReview'],
        availableAnalyzerProfiles: ['default', 'fabricAppQuality'],
        analysisCapabilities: ['findings', 'evidence', 'remediation', 'governanceSignals'],
        governanceCapabilities: ['analytics'],
      };
    case 'screenshotBundle':
      return {
        surfaceType: 'screenshotBundle',
        displayName: `Design Studio candidate for ${request.materializationMode}`,
        sourceLocation: `design-studio://materialization/${request.threadId}/${request.id}`,
        availableEvidenceKinds: ['screenshot'],
        availableAnalyzerTypes: [],
        availableAnalyzerProfiles: ['default'],
        analysisCapabilities: ['evidence'],
        governanceCapabilities: [],
      };
    default:
      return undefined;
  }
}

function buildProvenanceTrace(request: MaterializationRequest, capturedAt: string): MaterializationProvenanceEntry[] {
  return request.sourceLineage.map((entry) => ({
    ...entry,
    capturedAt,
  }));
}

function buildAnalyzerHandoff(
  request: MaterializationRequest,
  candidateId: string,
): MaterializationAnalyzerHandoffMetadata {
  return {
    target: 'analyzerWorkspace',
    requestId: request.id,
    candidateId,
    targetSurfaceType: request.targetSurfaceType,
    targetAnalyzer: request.targetAnalyzer,
    targetAnalyzerProfile: request.targetAnalyzerProfile,
    executionState: 'notStarted',
  };
}

function buildDiagnostics(mode: MaterializationMode): string[] {
  const modeNote = {
    conceptToStructurePreview: 'Concept-to-structure preview produced a derived candidate record only.',
    draftToSurfaceCandidate: 'Draft-to-surface candidate materialization produced candidate metadata only.',
    refinementProposalToCandidateComparison: 'Refinement proposal comparison produced a derived candidate record only.',
  }[mode];

  return [
    modeNote,
    'No PBIR files were created.',
    'No analyzer handoff was executed.',
    'No report mutation occurred.',
  ];
}

export function mapMaterializedSurfaceCandidate(
  request: MaterializationRequest,
): MaterializedSurfaceCandidate | undefined {
  const derivedSurface = buildDerivedSurface(request);
  if (!derivedSurface) {
    return undefined;
  }

  const now = new Date().toISOString();
  const candidateId = `materialized-surface-candidate:${request.threadId}:${request.id}`;
  return {
    id: candidateId,
    threadId: request.threadId,
    kind: 'materializedSurfaceCandidate',
    materializationMode: request.materializationMode,
    version: 1,
    lifecycleState: 'materialized',
    approvalState: 'approved',
    approvalKind: 'materializationApproval',
    createdAt: now,
    updatedAt: now,
    authorSource: 'system',
    provenance: {
      source: 'system',
      timestamp: now,
      notes: [
        `Derived from explicit ${request.materializationMode} materialization.`,
        'Materialization produced an analyzable-surface candidate only.',
      ],
    },
    validationLinkage: {
      comparedIterationId: request.materializationMode === 'refinementProposalToCandidateComparison'
        ? request.sourceArtifactIds.find((artifactId) => artifactId.startsWith('refinement-proposal:'))
        : undefined,
    },
    sourceArtifactIds: [...request.sourceArtifactIds],
    sourceLineage: request.sourceLineage.map((entry) => ({ ...entry })),
    targetSurfaceType: request.targetSurfaceType,
    derivedSurface,
    materializationDiagnostics: buildDiagnostics(request.materializationMode),
    provenanceTrace: buildProvenanceTrace(request, now),
    analyzerHandoff: buildAnalyzerHandoff(request, candidateId),
  };
}
