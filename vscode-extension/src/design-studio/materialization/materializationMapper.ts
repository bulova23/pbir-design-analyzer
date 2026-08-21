import type {
  MaterializationAnalyzerHandoffContract,
  MaterializationHandoffReference,
  MaterializationProvenanceEntry,
  MaterializationRequest,
  MaterializedSurfaceCandidate,
} from '../contracts/designStudioModels';
import { buildAnalyzableSurface } from '../../analyzer/surfaces/catalog';
import type { AnalyzableSurface } from '../../analyzer/surfaces/types';

function buildDerivedSurface(request: MaterializationRequest): AnalyzableSurface | undefined {
  switch (request.targetSurfaceType) {
    case 'pbirReport':
      return buildAnalyzableSurface('pbirReport', {
        displayName: `Design Studio candidate for ${request.materializationMode}`,
        sourceLocation: `design-studio://materialization/${request.threadId}/${request.id}`,
      });
    case 'fabricApp':
      return buildAnalyzableSurface('fabricApp', {
        displayName: `Design Studio candidate for ${request.materializationMode}`,
        sourceLocation: `design-studio://materialization/${request.threadId}/${request.id}`,
      });
    case 'screenshotBundle':
      return buildAnalyzableSurface('screenshotBundle', {
        displayName: `Design Studio candidate for ${request.materializationMode}`,
        sourceLocation: `design-studio://materialization/${request.threadId}/${request.id}`,
      });
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
  derivedSurface: AnalyzableSurface,
): MaterializationAnalyzerHandoffContract {
  const reference: MaterializationHandoffReference = request.handoffContext.snapshotReference
    ? {
      kind: 'snapshotBackedSurface',
      snapshotId: request.handoffContext.snapshotReference.snapshotId,
      rootPath: request.handoffContext.snapshotReference.rootPath,
      sourceLocation: request.handoffContext.snapshotReference.sourceLocation,
    }
    : request.handoffContext.repositoryBackedPath
      ? {
        kind: 'repositoryBackedSurface',
        repositoryPath: request.handoffContext.repositoryBackedPath,
      }
      : {
        kind: 'syntheticPreview',
        previewSourceLocation: derivedSurface.sourceLocation,
      };

  const diagnostics: string[] = [];
  if (reference.kind === 'syntheticPreview') {
    diagnostics.push('Synthetic design-studio preview candidates are not executable analyzer handoffs.');
    diagnostics.push('No repository-backed path or snapshot reference is available for analyzer execution.');
  }
  if (reference.kind === 'snapshotBackedSurface') {
    diagnostics.push('Snapshot-backed analyzer handoffs remain preview-only until Analyzer Workspace supports snapshot runtime execution.');
    diagnostics.push('No snapshot runtime execution path is currently available in Analyzer Workspace.');
  }

  return {
    metadata: {
      target: 'analyzerWorkspace',
      requestId: request.id,
      candidateId,
      targetSurfaceType: request.targetSurfaceType,
      targetAnalyzer: request.targetAnalyzer,
      targetAnalyzerProfile: request.targetAnalyzerProfile,
      executableEligibility: reference.kind === 'repositoryBackedSurface' ? 'executable' : 'nonExecutablePreview',
      executionState: 'notStarted',
      workspaceOpenState: 'notOpened',
    },
    reference,
    diagnostics,
  };
}

function buildDiagnostics(request: MaterializationRequest, handoff: MaterializationAnalyzerHandoffContract): string[] {
  const mode = request.materializationMode;
  const modeNote = {
    conceptToStructurePreview: 'Concept-to-structure preview produced a derived candidate record only.',
    draftToSurfaceCandidate: 'Draft-to-surface candidate materialization produced candidate metadata only.',
    refinementProposalToCandidateComparison: 'Refinement proposal comparison produced a derived candidate record only.',
  }[mode];

  return [
    modeNote,
    ...request.handoffContext.degradedMappings.map((entry) => `Mapping degradation: ${entry}`),
    ...request.handoffContext.omittedEvidence.map((entry) => `Omitted evidence: ${entry}`),
    ...handoff.diagnostics,
    'No PBIR files were created.',
    'No analyzer handoff was executed.',
    'No analyzer workspace was opened.',
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
  const analyzerHandoff = buildAnalyzerHandoff(request, candidateId, derivedSurface);
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
    materializationDiagnostics: buildDiagnostics(request, analyzerHandoff),
    provenanceTrace: buildProvenanceTrace(request, now),
    handoffContext: {
      repositoryBackedPath: request.handoffContext.repositoryBackedPath,
      snapshotReference: request.handoffContext.snapshotReference
        ? { ...request.handoffContext.snapshotReference }
        : undefined,
      degradedMappings: [...request.handoffContext.degradedMappings],
      omittedEvidence: [...request.handoffContext.omittedEvidence],
    },
    analyzerHandoff,
  };
}
