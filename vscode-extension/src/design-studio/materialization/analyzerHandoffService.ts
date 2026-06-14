import type {
  AnalyzerWorkspaceHandoffPayload,
  MaterializationHandoffEligibility,
  MaterializationHandoffReference,
  MaterializedSurfaceCandidate,
} from '../contracts/designStudioModels';
import type { MaterializationSideEffectState } from './materializationCoordinator';
import { resolveMaterializedCandidateHandoff } from './materializationHandoffResolver';

export interface AnalyzerHandoffWorkspaceLauncher {
  openAnalyzerWorkspace(payload: AnalyzerWorkspaceHandoffPayload): Promise<void>;
}

export interface AnalyzerHandoffResult {
  ok: boolean;
  eligibility: MaterializationHandoffEligibility;
  diagnostics: string[];
  payload?: AnalyzerWorkspaceHandoffPayload;
  reference: MaterializationHandoffReference;
  sideEffects: MaterializationSideEffectState | {
    analyzerHandoffExecuted: boolean;
    analyzerWorkspaceOpened: boolean;
    pbirFilesCreated: false;
    reportMutationOccurred: false;
    deliveryTriggered: false;
    providerExecutionTriggered: false;
  };
}

const BLOCKED_SIDE_EFFECTS: MaterializationSideEffectState = {
  analyzerHandoffExecuted: false,
  analyzerWorkspaceOpened: false,
  pbirFilesCreated: false,
  reportMutationOccurred: false,
  deliveryTriggered: false,
  providerExecutionTriggered: false,
};

export class AnalyzerHandoffService {
  constructor(
    private readonly launcher: AnalyzerHandoffWorkspaceLauncher,
  ) {}

  async handoffCandidate(candidate: MaterializedSurfaceCandidate): Promise<AnalyzerHandoffResult> {
    const resolution = resolveMaterializedCandidateHandoff(candidate);
    if (resolution.eligibility !== 'executable') {
      return {
        ok: false,
        eligibility: resolution.eligibility,
        diagnostics: resolution.diagnostics,
        reference: resolution.reference,
        sideEffects: BLOCKED_SIDE_EFFECTS,
      };
    }

    const payload: AnalyzerWorkspaceHandoffPayload = {
      candidateId: candidate.id,
      candidateLineage: candidate.sourceLineage.map((entry) => ({ ...entry })),
      candidateProvenance: {
        ...candidate.provenance,
        notes: candidate.provenance.notes ? [...candidate.provenance.notes] : undefined,
      },
      candidateProvenanceTrace: candidate.provenanceTrace.map((entry) => ({ ...entry })),
      sourceDesignArtifactReferences: [...candidate.sourceArtifactIds],
      sourceDesignArtifactVersionReferences: candidate.sourceLineage.map((entry) => entry.artifactVersionId),
      materializationDiagnostics: [...candidate.materializationDiagnostics],
      analyzerId: resolution.metadata.targetAnalyzer,
      analyzerProfileId: resolution.metadata.targetAnalyzerProfile,
      surfaceFamily: resolution.metadata.targetSurfaceType,
      executableEligibility: resolution.metadata.executableEligibility,
      handoffReference: resolution.reference,
      handoffDiagnostics: [...candidate.materializationDiagnostics],
      compatibilityStatus: 'compatible',
      compatibilityDiagnostics: [],
    };

    await this.launcher.openAnalyzerWorkspace(payload);

    return {
      ok: true,
      eligibility: 'executable',
      diagnostics: resolution.diagnostics,
      payload,
      reference: resolution.reference,
      sideEffects: {
        analyzerHandoffExecuted: true,
        analyzerWorkspaceOpened: true,
        pbirFilesCreated: false,
        reportMutationOccurred: false,
        deliveryTriggered: false,
        providerExecutionTriggered: false,
      },
    };
  }
}
