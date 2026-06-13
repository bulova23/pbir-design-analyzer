import type { MaterializationRequest } from '../design-studio/contracts/designStudioModels';
import {
  materializeDesignStudioRequest,
  validateMaterializationRequestSemantics,
} from '../design-studio/materialization/materializationCoordinator';

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
    targetAnalyzer: 'fabricAppReadiness',
    targetAnalyzerProfile: 'migrationReadiness',
    ...overrides,
  };
}

describe('materializationCoordinator', () => {
  it('rejects malformed materialization requests with semantic diagnostics', () => {
    const result = validateMaterializationRequestSemantics(buildRequest({
      version: 0,
      lifecycleState: 'draft',
      approvalKind: 'designApproval',
      createdAt: 'invalid-timestamp',
    }));

    expect(result.ok).toBe(false);
    expect(result.diagnostics).toEqual(expect.arrayContaining([
      'Materialization request version must be positive.',
      'Materialization request lifecycleState must be approved.',
      'Materialization request approvalKind must be materializationApproval.',
      'Materialization request createdAt must be a valid timestamp.',
    ]));
  });

  it('rejects source lineage mismatches and duplicates', () => {
    const result = validateMaterializationRequestSemantics(buildRequest({
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
        {
          artifactId: 'draft-report:thread-1',
          artifactKind: 'draftReportArtifact',
          artifactVersionId: 'draft-report:thread-1@v2',
          sourceRole: 'primary',
          approvalState: 'approved',
          approvalTimestamp: '2026-06-13T12:04:00.000Z',
        },
      ],
    }));

    expect(result.ok).toBe(false);
    expect(result.diagnostics).toContain('Materialization request sourceLineage entries must be unique.');
  });

  it('requires sourceLineage to correspond exactly to sourceArtifactIds', () => {
    const result = validateMaterializationRequestSemantics(buildRequest({
      sourceArtifactIds: ['draft-report:thread-1'],
      sourceLineage: [
        {
          artifactId: 'different-artifact:thread-1',
          artifactKind: 'draftReportArtifact',
          artifactVersionId: 'different-artifact:thread-1@v2',
          sourceRole: 'primary',
          approvalState: 'approved',
          approvalTimestamp: '2026-06-13T12:04:00.000Z',
        },
      ],
    }));

    expect(result.ok).toBe(false);
    expect(result.diagnostics).toContain('Materialization request sourceArtifactIds must correspond exactly to sourceLineage artifactIds.');
  });

  it('fails gracefully for unsupported target surface families', () => {
    const result = materializeDesignStudioRequest(buildRequest({
      targetSurfaceType: 'legacyWorkbook' as MaterializationRequest['targetSurfaceType'],
    }));

    expect(result.ok).toBe(false);
    expect(result.diagnostics).toContain('Unsupported target surface family: legacyWorkbook.');
  });

  it('produces a candidate record only for concept-to-structure preview', () => {
    const result = materializeDesignStudioRequest(buildRequest({
      materializationMode: 'conceptToStructurePreview',
      sourceArtifactIds: ['report-concept:thread-1'],
      sourceLineage: [
        {
          artifactId: 'report-concept:thread-1',
          artifactKind: 'reportConcept',
          artifactVersionId: 'report-concept:thread-1@v3',
          sourceRole: 'primary',
          approvalState: 'approved',
          approvalTimestamp: '2026-06-13T12:04:00.000Z',
        },
      ],
      targetAnalyzer: 'pbirDesignReview',
      targetAnalyzerProfile: 'default',
    }));

    expect(result.ok).toBe(true);
    if (!result.ok) {
      throw new Error('Expected materialization success.');
    }

    expect(result.candidate.kind).toBe('materializedSurfaceCandidate');
    expect(result.candidate.id).not.toBe('report-concept:thread-1');
    expect(result.candidate.derivedSurface.sourceLocation).toBe('design-studio://materialization/thread-1/materialization-request:thread-1');
    expect(result.sideEffects.pbirFilesCreated).toBe(false);
  });

  it('produces metadata-only draft candidates without PBIR creation or analyzer execution', () => {
    const result = materializeDesignStudioRequest(buildRequest());

    expect(result.ok).toBe(true);
    if (!result.ok) {
      throw new Error('Expected materialization success.');
    }

    expect(result.candidate.derivedSurface.surfaceType).toBe('pbirReport');
    expect(result.candidate.analyzerHandoff.executionState).toBe('notStarted');
    expect(result.sideEffects).toEqual({
      analyzerHandoffExecuted: false,
      pbirFilesCreated: false,
      reportMutationOccurred: false,
      deliveryTriggered: false,
      providerExecutionTriggered: false,
    });
  });

  it('preserves refinement proposal lineage for comparison candidates', () => {
    const result = materializeDesignStudioRequest(buildRequest({
      materializationMode: 'refinementProposalToCandidateComparison',
      sourceArtifactIds: ['draft-report:thread-1', 'refinement-proposal:thread-1:storyAssessment:result-1:1'],
      sourceLineage: [
        {
          artifactId: 'draft-report:thread-1',
          artifactKind: 'draftReportArtifact',
          artifactVersionId: 'draft-report:thread-1@v2',
          sourceRole: 'comparisonBase',
          approvalState: 'approved',
          approvalTimestamp: '2026-06-13T12:04:00.000Z',
        },
        {
          artifactId: 'refinement-proposal:thread-1:storyAssessment:result-1:1',
          artifactKind: 'refinementProposal',
          artifactVersionId: 'refinement-proposal:thread-1:storyAssessment:result-1:1@v1',
          sourceRole: 'comparisonProposal',
          approvalState: 'approved',
          approvalTimestamp: '2026-06-13T12:04:30.000Z',
        },
      ],
      targetAnalyzer: 'pbirDesignReview',
      targetAnalyzerProfile: 'default',
    }));

    expect(result.ok).toBe(true);
    if (!result.ok) {
      throw new Error('Expected materialization success.');
    }

    expect(result.candidate.sourceLineage.some((entry) =>
      entry.artifactKind === 'refinementProposal' && entry.sourceRole === 'comparisonProposal')).toBe(true);
    expect(result.candidate.provenanceTrace.some((entry) =>
      entry.artifactId === 'refinement-proposal:thread-1:storyAssessment:result-1:1')).toBe(true);
  });
});
