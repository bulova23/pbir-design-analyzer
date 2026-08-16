import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import type { MaterializationRequest } from '../design-studio/contracts/designStudioModels';
import {
  createApprovedDraftMaterializationRequest,
  materializeDesignStudioRequest,
  validateMaterializationRequestSemantics,
} from '../design-studio/materialization/materializationCoordinator';
import {
  approveConceptBaseline,
  generateConceptArtifacts,
  selectConceptBaseline,
  submitConceptBaselineForApproval,
} from '../design-studio/state/conceptStore';
import {
  approveDesignBrief,
  saveDesignBriefDraft,
  submitDesignBriefForApproval,
} from '../design-studio/state/designBriefStore';
import {
  approveDraftArtifacts,
  generateDraftArtifacts,
  submitDraftForApproval,
} from '../design-studio/state/draftStore';

function makeContext(tmpDir: string): ExtensionContext {
  return {
    globalStorageUri: { fsPath: tmpDir },
    secrets: {
      get: jest.fn(),
      store: jest.fn(),
      delete: jest.fn(),
    },
  } as unknown as ExtensionContext;
}

function makeTempDir(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-materialization-coordinator-test-'));
}

async function saveApprovedConcept(context: ExtensionContext, threadId: string): Promise<void> {
  await saveDesignBriefDraft(context, threadId, {
    audience: 'Sales leaders',
    businessObjective: 'Reduce missed renewals',
    keyDecisions: ['Which regions need intervention first'],
    primaryKpis: ['Renewal rate', 'At-risk pipeline'],
    dimensions: ['Region', 'Segment'],
    intendedStory: 'Lead with risk, then explain the main drivers and next steps.',
    successCriteria: ['Leader can decide the next action within five minutes'],
    reportType: 'dashboard',
    navigationExpectations: 'Overview first, detail second.',
    consumptionContext: 'Weekly renewal review',
    decisionCadence: 'Weekly',
    narrativeRisksOrConstraints: ['Avoid hiding segment outliers'],
    requiredEvidenceDomains: ['renewal trend', 'pipeline coverage'],
    targetAnalyzableSurfaceFamily: 'pbir',
  });
  await submitDesignBriefForApproval(context, threadId);
  await approveDesignBrief(context, threadId);
  const conceptState = await generateConceptArtifacts(context, threadId);
  await selectConceptBaseline(context, threadId, conceptState.currentConcept.alternateConcepts[0].id);
  await submitConceptBaselineForApproval(context, threadId);
  await approveConceptBaseline(context, threadId);
}

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
    handoffContext: {
      degradedMappings: [],
      omittedEvidence: [],
    },
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

  it('returns analyzer compatibility diagnostics for unsupported analyzer and profile combinations', () => {
    const analyzerResult = validateMaterializationRequestSemantics(buildRequest({
      targetAnalyzer: 'fabricAppReview',
      targetAnalyzerProfile: 'fabricAppQuality',
    }));

    expect(analyzerResult.ok).toBe(false);
    expect(analyzerResult.diagnostics).toContain('Target analyzer fabricAppReview is not supported for pbirReport.');

    const profileResult = validateMaterializationRequestSemantics(buildRequest({
      targetAnalyzer: 'pbirDesignReview',
      targetAnalyzerProfile: 'migrationReadiness',
    }));

    expect(profileResult.ok).toBe(false);
    expect(profileResult.diagnostics).toContain('Target analyzer profile migrationReadiness is not supported for pbirDesignReview.');
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
    expect(result.candidate.analyzerHandoff.metadata.executionState).toBe('notStarted');
    expect(result.sideEffects).toEqual({
      analyzerHandoffExecuted: false,
      analyzerWorkspaceOpened: false,
      pbirFilesCreated: false,
      reportMutationOccurred: false,
      deliveryTriggered: false,
      providerExecutionTriggered: false,
    });
  });

  it('includes mapping degradation and omitted evidence diagnostics without executing handoff', () => {
    const result = materializeDesignStudioRequest(buildRequest({
      handoffContext: {
        degradedMappings: [
          'Page-to-page lineage was reduced to report-level ancestry for the navigation concept.',
        ],
        omittedEvidence: [
          'Semantic model evidence was omitted because no repository-backed path exists yet.',
        ],
      },
    }));

    expect(result.ok).toBe(true);
    if (!result.ok) {
      throw new Error('Expected materialization success.');
    }

    expect(result.diagnostics).toEqual(expect.arrayContaining([
      'Mapping degradation: Page-to-page lineage was reduced to report-level ancestry for the navigation concept.',
      'Omitted evidence: Semantic model evidence was omitted because no repository-backed path exists yet.',
      'Synthetic design-studio preview candidates are not executable analyzer handoffs.',
    ]));
    expect(result.sideEffects.analyzerHandoffExecuted).toBe(false);
    expect(result.sideEffects.analyzerWorkspaceOpened).toBe(false);
    expect(result.sideEffects.pbirFilesCreated).toBe(false);
    expect(result.sideEffects.reportMutationOccurred).toBe(false);
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

  it('rejects draft-to-surface materialization before explicit draft approval', async () => {
    const context = makeContext(makeTempDir());
    await saveApprovedConcept(context, 'thread-materialization-approval');
    await generateDraftArtifacts(context, 'thread-materialization-approval');

    await expect(createApprovedDraftMaterializationRequest(context, {
      threadId: 'thread-materialization-approval',
      requestId: 'request-pending',
      targetSurfaceType: 'pbirReport',
      targetAnalyzer: 'pbirDesignReview',
      targetAnalyzerProfile: 'default',
    })).rejects.toThrow('Draft-to-surface materialization requires an approved draft version.');
  });

  it('builds materialization requests only from approved draft lineage', async () => {
    const context = makeContext(makeTempDir());
    await saveApprovedConcept(context, 'thread-materialization-approved');
    await generateDraftArtifacts(context, 'thread-materialization-approved');
    await submitDraftForApproval(context, 'thread-materialization-approved');
    const approved = await approveDraftArtifacts(context, 'thread-materialization-approved');

    const request = await createApprovedDraftMaterializationRequest(context, {
      threadId: 'thread-materialization-approved',
      requestId: 'request-approved',
      targetSurfaceType: 'pbirReport',
      targetAnalyzer: 'pbirDesignReview',
      targetAnalyzerProfile: 'default',
    });
    const result = materializeDesignStudioRequest(request);

    expect(request.sourceArtifactIds).toEqual([approved.currentDraft.id]);
    expect(request.sourceLineage).toEqual([
      expect.objectContaining({
        artifactId: approved.currentDraft.id,
        artifactVersionId: `${approved.currentDraft.id}@v${approved.currentDraft.version}`,
        approvalState: 'approved',
      }),
    ]);
    expect(result.ok).toBe(true);
  });
});
