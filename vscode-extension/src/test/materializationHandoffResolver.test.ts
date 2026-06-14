import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { MaterializationRequest } from '../design-studio/contracts/designStudioModels';
import { materializeDesignStudioRequest } from '../design-studio/materialization/materializationCoordinator';
import { resolveMaterializedCandidateHandoff } from '../design-studio/materialization/materializationHandoffResolver';

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

describe('materializationHandoffResolver', () => {
  it('marks synthetic design-studio candidates as non-executable previews', () => {
    const result = materializeDesignStudioRequest(buildRequest());

    expect(result.ok).toBe(true);
    if (!result.ok) {
      throw new Error('Expected materialization success.');
    }

    const handoff = resolveMaterializedCandidateHandoff(result.candidate);
    expect(handoff.eligibility).toBe('nonExecutablePreview');
    expect(handoff.reference.kind).toBe('syntheticPreview');
    expect(handoff.metadata.executionState).toBe('notStarted');
    expect(handoff.diagnostics).toEqual(expect.arrayContaining([
      'Synthetic design-studio preview candidates are not executable analyzer handoffs.',
      'No repository-backed path or snapshot reference is available for analyzer execution.',
    ]));
    expect(handoff.sideEffects).toEqual({
      analyzerHandoffExecuted: false,
      analyzerWorkspaceOpened: false,
      pbirFilesCreated: false,
      reportMutationOccurred: false,
      deliveryTriggered: false,
      providerExecutionTriggered: false,
    });
  });

  it('marks repository-backed candidates executable only when a supported reference exists', () => {
    const reportDir = fs.mkdtempSync(path.join(os.tmpdir(), 'design-studio-pbir-'));
    const pbirReportPath = path.join(reportDir, 'Sales.Report');
    fs.mkdirSync(pbirReportPath, { recursive: true });
    fs.writeFileSync(path.join(pbirReportPath, 'definition.pbir'), '{}', 'utf8');

    try {
      const result = materializeDesignStudioRequest(buildRequest({
        handoffContext: {
          repositoryBackedPath: pbirReportPath,
          degradedMappings: [],
          omittedEvidence: [],
        },
      }));

      expect(result.ok).toBe(true);
      if (!result.ok) {
        throw new Error('Expected materialization success.');
      }

      const handoff = resolveMaterializedCandidateHandoff(result.candidate);
      expect(handoff.eligibility).toBe('executable');
      expect(handoff.reference).toMatchObject({
        kind: 'repositoryBackedSurface',
        repositoryPath: pbirReportPath,
      });
      expect(handoff.diagnostics).not.toContain('Synthetic design-studio preview candidates are not executable analyzer handoffs.');
      expect(handoff.sideEffects.analyzerWorkspaceOpened).toBe(false);
    } finally {
      fs.rmSync(reportDir, { recursive: true, force: true });
    }
  });

  it('downgrades snapshot-backed candidates to preview-only until a real runtime path exists', () => {
    const result = materializeDesignStudioRequest(buildRequest({
      handoffContext: {
        snapshotReference: {
          snapshotId: 'snapshot-1',
          rootPath: '/virtual/snapshots/report-1',
          sourceLocation: '/virtual/snapshots/report-1/Sales.Report',
        },
        degradedMappings: [],
        omittedEvidence: [],
      },
    }));

    expect(result.ok).toBe(true);
    if (!result.ok) {
      throw new Error('Expected materialization success.');
    }

    const handoff = resolveMaterializedCandidateHandoff(result.candidate);
    expect(handoff.eligibility).toBe('nonExecutablePreview');
    expect(handoff.reference).toEqual({
      kind: 'snapshotBackedSurface',
      snapshotId: 'snapshot-1',
      rootPath: '/virtual/snapshots/report-1',
      sourceLocation: '/virtual/snapshots/report-1/Sales.Report',
    });
    expect(handoff.diagnostics).toEqual(expect.arrayContaining([
      'Snapshot-backed analyzer handoffs remain preview-only until Analyzer Workspace supports snapshot runtime execution.',
      'No snapshot runtime execution path is currently available in Analyzer Workspace.',
    ]));
    expect(handoff.sideEffects.analyzerHandoffExecuted).toBe(false);
    expect(handoff.sideEffects.analyzerWorkspaceOpened).toBe(false);
  });
});
