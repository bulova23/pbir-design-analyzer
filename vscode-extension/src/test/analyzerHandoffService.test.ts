import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type {
  MaterializationRequest,
  MaterializedSurfaceCandidate,
} from '../design-studio/contracts/designStudioModels';
import { materializeDesignStudioRequest } from '../design-studio/materialization/materializationCoordinator';
import { AnalyzerHandoffService } from '../design-studio/materialization/analyzerHandoffService';
import { buildAnalyzableSurface } from '../analyzer/surfaces/catalog';

function buildRequest(overrides: Partial<MaterializationRequest> = {}): MaterializationRequest {
  return {
    id: 'materialization-request:thread-1',
    threadId: 'thread-1',
    kind: 'materializationRequest',
    materializationMode: 'draftToSurfaceCandidate',
    version: 1,
    lifecycleState: 'approved',
    approvalState: 'approved',
    approvalKind: 'materializationApproval',
    createdAt: '2026-06-13T12:00:00.000Z',
    updatedAt: '2026-06-13T12:05:00.000Z',
    authorSource: 'system',
    provenance: {
      source: 'system',
      timestamp: '2026-06-13T12:05:00.000Z',
      notes: ['Approved for explicit materialization preview only.'],
    },
    sourceArtifactIds: ['draft-report:thread-1'],
    sourceLineage: [
      {
        artifactId: 'draft-report:thread-1',
        artifactKind: 'draftReportArtifact',
        artifactVersionId: 'draft-report:thread-1@v2',
        sourceRole: 'primary',
        approvalState: 'approved',
        approvalTimestamp: '2026-06-13T12:04:00.000Z',
      },
    ],
    targetSurfaceType: 'pbirReport',
    targetAnalyzer: 'pbirDesignReview',
    targetAnalyzerProfile: 'default',
    handoffContext: {
      degradedMappings: [],
      omittedEvidence: [],
    },
    ...overrides,
  };
}

function materializeRequest(overrides: Partial<MaterializationRequest> = {}): MaterializedSurfaceCandidate {
  const result = materializeDesignStudioRequest(buildRequest(overrides));
  if (!result.ok) {
    throw new Error(`Expected materialization success. ${result.diagnostics.join(' | ')}`);
  }

  return result.candidate;
}

describe('AnalyzerHandoffService', () => {
  it('hands off executable candidates as a separate analyzer workspace launch', async () => {
    const reportDir = fs.mkdtempSync(path.join(os.tmpdir(), 'analyzer-handoff-'));
    const pbirReportPath = path.join(reportDir, 'Sales.Report');
    fs.mkdirSync(pbirReportPath, { recursive: true });
    fs.writeFileSync(path.join(pbirReportPath, 'definition.pbir'), '{}', 'utf8');

    try {
      const candidate = materializeRequest({
        handoffContext: {
          repositoryBackedPath: pbirReportPath,
          degradedMappings: [
            'Page-level layout lineage collapsed to report-level ancestry for the navigation summary.',
          ],
          omittedEvidence: [
            'Semantic model evidence remains deferred until the analyzer is run explicitly.',
          ],
        },
      });
      const openAnalyzerWorkspace = jest.fn<Promise<void>, [unknown]>().mockResolvedValue(undefined);
      const service = new AnalyzerHandoffService({ openAnalyzerWorkspace });

      const result = await service.handoffCandidate(candidate);

      expect(result.ok).toBe(true);
      expect(openAnalyzerWorkspace).toHaveBeenCalledTimes(1);
      expect(result.eligibility).toBe('executable');
      expect(result.payload).toEqual(expect.objectContaining({
        candidateId: candidate.id,
        candidateLineage: candidate.sourceLineage,
        candidateProvenance: candidate.provenance,
        sourceDesignArtifactReferences: candidate.sourceArtifactIds,
        sourceDesignArtifactVersionReferences: ['draft-report:thread-1@v2'],
        materializationDiagnostics: candidate.materializationDiagnostics,
        analyzerId: 'pbirDesignReview',
        analyzerProfileId: 'default',
        surfaceFamily: 'pbirReport',
        executableEligibility: 'executable',
      }));
      expect(result.payload?.candidateProvenanceTrace).toEqual(candidate.provenanceTrace);
      expect(result.payload?.handoffReference).toEqual({
        kind: 'repositoryBackedSurface',
        repositoryPath: pbirReportPath,
      });
      expect(result.payload?.handoffDiagnostics).toEqual(expect.arrayContaining([
        'Mapping degradation: Page-level layout lineage collapsed to report-level ancestry for the navigation summary.',
        'Omitted evidence: Semantic model evidence remains deferred until the analyzer is run explicitly.',
      ]));
      expect(result.sideEffects).toEqual({
        analyzerHandoffExecuted: true,
        analyzerWorkspaceOpened: true,
        pbirFilesCreated: false,
        reportMutationOccurred: false,
        deliveryTriggered: false,
        providerExecutionTriggered: false,
      });
      expect(candidate.analyzerHandoff.metadata.executionState).toBe('notStarted');
      expect(candidate.analyzerHandoff.metadata.workspaceOpenState).toBe('notOpened');
    } finally {
      fs.rmSync(reportDir, { recursive: true, force: true });
    }
  });

  it('blocks non-executable preview candidates and preserves diagnostics without opening the analyzer workspace', async () => {
    const candidate = materializeRequest();
    const openAnalyzerWorkspace = jest.fn<Promise<void>, [unknown]>().mockResolvedValue(undefined);
    const service = new AnalyzerHandoffService({ openAnalyzerWorkspace });

    const result = await service.handoffCandidate(candidate);

    expect(result.ok).toBe(false);
    expect(result.eligibility).toBe('nonExecutablePreview');
    expect(result.payload).toBeUndefined();
    expect(result.diagnostics).toEqual(expect.arrayContaining([
      'Synthetic design-studio preview candidates are not executable analyzer handoffs.',
      'No repository-backed path or snapshot reference is available for analyzer execution.',
    ]));
    expect(openAnalyzerWorkspace).not.toHaveBeenCalled();
    expect(result.sideEffects.analyzerHandoffExecuted).toBe(false);
    expect(result.sideEffects.analyzerWorkspaceOpened).toBe(false);
    expect(result.sideEffects.pbirFilesCreated).toBe(false);
    expect(result.sideEffects.reportMutationOccurred).toBe(false);
  });

  it('blocks unsupported candidates through the shared analyzer registry compatibility path', async () => {
    const baseCandidate = materializeRequest({
      handoffContext: {
        snapshotReference: {
          snapshotId: 'snapshot-1',
          rootPath: '/virtual/snapshots/screenshot-bundle',
          sourceLocation: '/virtual/snapshots/screenshot-bundle',
        },
        degradedMappings: [],
        omittedEvidence: [],
      },
    });
    const candidate: MaterializedSurfaceCandidate = {
      ...baseCandidate,
      targetSurfaceType: 'screenshotBundle',
      derivedSurface: buildAnalyzableSurface('screenshotBundle', {
        displayName: 'Screenshot bundle handoff candidate',
        sourceLocation: '/virtual/snapshots/screenshot-bundle',
      }),
      analyzerHandoff: {
        ...baseCandidate.analyzerHandoff,
        metadata: {
          ...baseCandidate.analyzerHandoff.metadata,
          targetSurfaceType: 'screenshotBundle',
          targetAnalyzer: 'pbirDesignReview',
          targetAnalyzerProfile: 'default',
        },
      },
    };
    const openAnalyzerWorkspace = jest.fn<Promise<void>, [unknown]>().mockResolvedValue(undefined);
    const service = new AnalyzerHandoffService({ openAnalyzerWorkspace });

    const result = await service.handoffCandidate(candidate);

    expect(result.ok).toBe(false);
    expect(result.eligibility).toBe('unsupported');
    expect(result.diagnostics).toContain('Target analyzer pbirDesignReview is not supported for screenshotBundle.');
    expect(openAnalyzerWorkspace).not.toHaveBeenCalled();
  });

  it('blocks snapshot-backed candidates as preview-only while preserving lineage, provenance, and diagnostics', async () => {
    const candidate = materializeRequest({
      handoffContext: {
        snapshotReference: {
          snapshotId: 'snapshot-7',
          rootPath: '/virtual/snapshots/report-7',
          sourceLocation: '/virtual/snapshots/report-7/Sales.Report',
        },
        degradedMappings: [
          'Visual target mapping is limited to page-level ancestry in the snapshot.',
        ],
        omittedEvidence: [
          'Live repository navigation remains unavailable for snapshot-backed review inputs.',
        ],
      },
    });
    const openAnalyzerWorkspace = jest.fn<Promise<void>, [unknown]>().mockResolvedValue(undefined);
    const service = new AnalyzerHandoffService({ openAnalyzerWorkspace });

    const result = await service.handoffCandidate(candidate);

    expect(result.ok).toBe(false);
    expect(result.eligibility).toBe('nonExecutablePreview');
    expect(result.payload).toBeUndefined();
    expect(result.reference).toEqual({
      kind: 'snapshotBackedSurface',
      snapshotId: 'snapshot-7',
      rootPath: '/virtual/snapshots/report-7',
      sourceLocation: '/virtual/snapshots/report-7/Sales.Report',
    });
    expect(result.diagnostics).toEqual(expect.arrayContaining([
      'Snapshot-backed analyzer handoffs remain preview-only until Analyzer Workspace supports snapshot runtime execution.',
      'No snapshot runtime execution path is currently available in Analyzer Workspace.',
    ]));
    expect(openAnalyzerWorkspace).not.toHaveBeenCalled();
    expect(result.sideEffects.analyzerHandoffExecuted).toBe(false);
    expect(result.sideEffects.analyzerWorkspaceOpened).toBe(false);
  });
});
