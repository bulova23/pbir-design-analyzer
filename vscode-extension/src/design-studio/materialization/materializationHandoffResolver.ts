import { detectAnalyzableSurface } from '../../analyzer/surfaces/discovery';
import type {
  MaterializationAnalyzerHandoffContract,
  MaterializationHandoffEligibility,
  MaterializationHandoffReference,
  MaterializedSurfaceCandidate,
} from '../contracts/designStudioModels';
import { evaluateAnalyzerSurfaceCompatibility } from './analyzerSurfaceCompatibility';
import type { MaterializationSideEffectState } from './materializationCoordinator';

export interface MaterializedCandidateHandoffResolution {
  eligibility: MaterializationHandoffEligibility;
  reference: MaterializationHandoffReference;
  metadata: MaterializationAnalyzerHandoffContract['metadata'];
  diagnostics: string[];
  sideEffects: MaterializationSideEffectState;
}

const HANDOFF_SIDE_EFFECT_STATE: MaterializationSideEffectState = {
  analyzerHandoffExecuted: false,
  analyzerWorkspaceOpened: false,
  pbirFilesCreated: false,
  reportMutationOccurred: false,
  deliveryTriggered: false,
  providerExecutionTriggered: false,
};

function buildUnsupportedResolution(
  candidate: MaterializedSurfaceCandidate,
  reference: MaterializationHandoffReference,
  diagnostics: string[],
): MaterializedCandidateHandoffResolution {
  return {
    eligibility: 'unsupported',
    reference,
    metadata: {
      ...candidate.analyzerHandoff.metadata,
      executableEligibility: 'unsupported',
    },
    diagnostics,
    sideEffects: HANDOFF_SIDE_EFFECT_STATE,
  };
}

export function resolveMaterializedCandidateHandoff(
  candidate: MaterializedSurfaceCandidate,
): MaterializedCandidateHandoffResolution {
  const compatibility = evaluateAnalyzerSurfaceCompatibility(
    candidate.derivedSurface,
    candidate.analyzerHandoff.metadata.targetAnalyzer,
    candidate.analyzerHandoff.metadata.targetAnalyzerProfile,
  );

  if (!compatibility.ok) {
    return buildUnsupportedResolution(candidate, {
      kind: 'unsupported',
      reason: compatibility.diagnostics.join(' '),
    }, compatibility.diagnostics);
  }

  const snapshotReference = candidate.handoffContext.snapshotReference;
  if (snapshotReference) {
    return {
      eligibility: 'nonExecutablePreview',
      reference: {
        kind: 'snapshotBackedSurface',
        snapshotId: snapshotReference.snapshotId,
        rootPath: snapshotReference.rootPath,
        sourceLocation: snapshotReference.sourceLocation,
      },
      metadata: {
        ...candidate.analyzerHandoff.metadata,
        executableEligibility: 'nonExecutablePreview',
      },
      diagnostics: [...candidate.analyzerHandoff.diagnostics],
      sideEffects: HANDOFF_SIDE_EFFECT_STATE,
    };
  }

  const repositoryBackedPath = candidate.handoffContext.repositoryBackedPath;
  if (repositoryBackedPath) {
    const discovery = detectAnalyzableSurface(repositoryBackedPath);
    if (discovery.status === 'supported' && discovery.surface.surfaceType === candidate.targetSurfaceType) {
      const repoCompatibility = evaluateAnalyzerSurfaceCompatibility(
        discovery.surface,
        candidate.analyzerHandoff.metadata.targetAnalyzer,
        candidate.analyzerHandoff.metadata.targetAnalyzerProfile,
      );

      if (repoCompatibility.ok) {
        return {
          eligibility: 'executable',
          reference: {
            kind: 'repositoryBackedSurface',
            repositoryPath: repositoryBackedPath,
          },
          metadata: {
            ...candidate.analyzerHandoff.metadata,
            executableEligibility: 'executable',
          },
          diagnostics: candidate.analyzerHandoff.diagnostics.filter((diagnostic) =>
            diagnostic !== 'Synthetic design-studio preview candidates are not executable analyzer handoffs.'
            && diagnostic !== 'No repository-backed path or snapshot reference is available for analyzer execution.'),
          sideEffects: HANDOFF_SIDE_EFFECT_STATE,
        };
      }

      return buildUnsupportedResolution(candidate, {
        kind: 'unsupported',
        reason: repoCompatibility.diagnostics.join(' '),
      }, repoCompatibility.diagnostics);
    }

    return buildUnsupportedResolution(candidate, {
      kind: 'unsupported',
      reason: `Repository-backed path ${repositoryBackedPath} does not resolve to a supported ${candidate.targetSurfaceType} surface.`,
    }, [`Repository-backed path ${repositoryBackedPath} does not resolve to a supported ${candidate.targetSurfaceType} surface.`]);
  }

  return {
    eligibility: 'nonExecutablePreview',
    reference: {
      kind: 'syntheticPreview',
      previewSourceLocation: candidate.derivedSurface.sourceLocation,
    },
    metadata: {
      ...candidate.analyzerHandoff.metadata,
      executableEligibility: 'nonExecutablePreview',
    },
    diagnostics: [...candidate.analyzerHandoff.diagnostics],
    sideEffects: HANDOFF_SIDE_EFFECT_STATE,
  };
}
