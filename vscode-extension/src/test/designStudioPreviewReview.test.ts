import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import { buildDesignStudioWorkspace } from '../design-studio/presentation/designStudioWorkspace';
import {
  DESIGN_STUDIO_PREVIEW_REVIEW_SCHEMA_VERSION,
  DesignStudioPreviewReviewSafetyGate,
  loadDesignStudioPreviewReviewState,
  prepareAnalyzerCandidateMetadata,
  recordPreviewReviewHandoff,
  setPreviewReviewAction,
  type DesignStudioPreviewReviewInput,
} from '../design-studio/state/previewReviewStore';
import { DesignStudioExecutionReadinessSafetyGate } from '../design-studio/state/executionReadinessStore';

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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-design-studio-preview-review-test-'));
}

function createThreadId(reportPath: string): string {
  return `design-studio:${crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16)}`;
}

function hash(seed: string): string {
  return crypto.createHash('sha256').update(seed).digest('hex');
}

function createPreviewReviewInput(overrides: Partial<DesignStudioPreviewReviewInput> = {}): DesignStudioPreviewReviewInput {
  return {
    schemaVersion: DESIGN_STUDIO_PREVIEW_REVIEW_SCHEMA_VERSION,
    previewReviewId: 'designStudioPreviewReview:phase27',
    previewPackage: {
      packageId: 'pbirPreviewPackage:phase27',
      schemaVersion: 'pbir-preview-package/v1',
      packageHash: hash('package'),
      generatedUtc: '2026-06-27T11:30:00.000Z',
      metadataOnly: true,
      localOnly: true,
      containsPhysicalFileContent: false,
      zipCreated: false,
      deployableArtifactsAllowed: false,
      summary: {
        fileCount: 5,
        warningCount: 1,
        rejectedArtifactCount: 0,
      },
      fileInventory: [
        {
          artifactType: 'previewMarkdown',
          relativePath: 'pbir-local-writer/v1/preview/report-preview.md',
          reference: 'local-preview-output/pbir-local-writer/v1/preview/report-preview.md',
          contentType: 'text/markdown',
          hashSha256: hash('preview-md'),
          byteLength: 120,
        },
        {
          artifactType: 'previewJson',
          relativePath: 'pbir-local-writer/v1/preview/report-preview.json',
          reference: 'local-preview-output/pbir-local-writer/v1/preview/report-preview.json',
          contentType: 'application/json',
          hashSha256: hash('preview-json'),
          byteLength: 240,
        },
        {
          artifactType: 'canonicalIrJson',
          relativePath: 'pbir-local-writer/v1/ir/canonical-pbir-ir.json',
          reference: 'local-preview-output/pbir-local-writer/v1/ir/canonical-pbir-ir.json',
          contentType: 'application/json',
          hashSha256: hash('ir-json'),
          byteLength: 360,
        },
        {
          artifactType: 'previewManifestJson',
          relativePath: 'pbir-local-writer/v1/manifests/pbir-preview-manifest.json',
          reference: 'local-preview-output/pbir-local-writer/v1/manifests/pbir-preview-manifest.json',
          contentType: 'application/json',
          hashSha256: hash('manifest-json'),
          byteLength: 480,
        },
        {
          artifactType: 'diagnosticsMarkdown',
          relativePath: 'pbir-local-writer/v1/diagnostics/local-write-diagnostics.md',
          reference: 'local-preview-output/pbir-local-writer/v1/diagnostics/local-write-diagnostics.md',
          contentType: 'text/markdown',
          hashSha256: hash('diagnostics-md'),
          byteLength: 80,
        },
      ],
      hashInventory: [
        {
          hashKind: 'package',
          referenceId: 'pbirPreviewPackage:phase27',
          hashSha256: hash('package'),
          description: 'Package hash',
        },
      ],
      lineage: {
        previewPackageRef: 'pbirPreviewPackage:phase27',
        generationManifestRef: 'generationManifest:phase27',
        pbirIrRef: 'pbirIr:phase27',
        previewManifestRef: 'pbirPreviewManifest:phase27',
        sourceWriteManifestRef: 'pbirLocalWriteManifest:phase27',
        immutableLineage: [
          'generationManifest:phase27',
          'pbirIr:phase27',
          'pbirPreviewManifest:phase27',
          'pbirPreviewPackage:phase27',
        ],
      },
      rollbackMetadata: {
        rollbackPlanRef: 'rollback:phase27',
        rollbackPlanHash: hash('rollback'),
        actionCount: 5,
        automaticRollbackExecuted: false,
      },
      warnings: ['Deployable PBIR artifacts remain forbidden.'],
      rejectedArtifacts: [],
    },
    reviewHandoff: {
      handoffId: 'pbirReviewHandoff:phase27',
      schemaVersion: 'pbir-review-handoff/v1',
      reviewTarget: 'DesignStudio',
      reviewReadiness: 'readyForDesignReview',
      requiredReviewerAction: 'Review local preview outputs in Design Studio before any future execution planning.',
      previewPackageReference: {
        packageId: 'pbirPreviewPackage:phase27',
        schemaVersion: 'pbir-preview-package/v1',
        packageHash: hash('package'),
      },
      pbirIrReference: {
        irId: 'pbirIr:phase27',
        schemaVersion: 'pbir-ir/v1',
        contentHash: hash('ir'),
      },
      analyzerWorkspaceBoundary: {
        validationOccurred: false,
        automaticValidationRequested: false,
        automaticValidationAllowed: false,
        workspaceLaunchRequested: false,
        validationStatus: 'No Analyzer Workspace validation has occurred.',
      },
      deploymentBoundary: {
        deploymentRequested: false,
        deploymentAllowed: false,
      },
      warnings: ['readyForDesignReview does not mean Analyzer validation occurred.'],
    },
    reviewerAction: 'pending',
    reviewerNotes: '',
    readinessState: 'readyForDesignReview',
    warnings: ['Review is advisory until Analyzer Workspace validates a candidate.'],
    boundaryRequests: {
      automaticAnalyzerExecutionRequested: false,
      automaticAnalyzerLaunchRequested: false,
      microsoftSkillsExecutionRequested: false,
      providerInvocationRequested: false,
      apiInvocationRequested: false,
      cliInvocationRequested: false,
      deploymentRequested: false,
    },
    ...overrides,
  };
}

describe('designStudioPreviewReviewStore', () => {
  it('records a review-only Design Studio preview review contract and projects it into workspace state', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Preview Review.Report.pbir';
    const threadId = createThreadId(reportPath);
    const input = createPreviewReviewInput();

    const state = await recordPreviewReviewHandoff(context, threadId, input, '2026-06-27T11:45:00.000Z');
    const loaded = await loadDesignStudioPreviewReviewState(context, threadId);
    const workspace = await buildDesignStudioWorkspace(context, reportPath);

    expect(state.currentReview).toEqual(expect.objectContaining({
      schemaVersion: DESIGN_STUDIO_PREVIEW_REVIEW_SCHEMA_VERSION,
      previewReviewId: 'designStudioPreviewReview:phase27',
      reviewerAction: 'pending',
      readinessState: 'readyForDesignReview',
    }));
    expect(loaded?.currentReview?.previewPackage.summary.fileCount).toBe(5);
    expect(workspace.workspace.previewReview).toEqual(expect.objectContaining({
      previewReviewId: 'designStudioPreviewReview:phase27',
      readinessState: 'readyForDesignReview',
      reviewerAction: 'pending',
      requiredReviewerAction: 'Review local preview outputs in Design Studio before any future execution planning.',
      canMarkReviewed: true,
      canRequestRevision: true,
      canDeferReview: true,
      canPrepareAnalyzerCandidateMetadata: true,
    }));
    expect(workspace.workspace.previewReview?.references).toEqual(expect.objectContaining({
      previewMarkdown: 'local-preview-output/pbir-local-writer/v1/preview/report-preview.md',
      previewJson: 'local-preview-output/pbir-local-writer/v1/preview/report-preview.json',
      canonicalIr: 'local-preview-output/pbir-local-writer/v1/ir/canonical-pbir-ir.json',
      previewManifest: 'local-preview-output/pbir-local-writer/v1/manifests/pbir-preview-manifest.json',
      diagnostics: 'local-preview-output/pbir-local-writer/v1/diagnostics/local-write-diagnostics.md',
      reviewHandoff: 'pbirReviewHandoff:phase27',
    }));
    expect(workspace.workspace.executionReadiness).toEqual(expect.objectContaining({
      schemaVersion: 'design-studio-execution-readiness/v1',
      readinessSummary: 'readyForDesignReview',
      readinessLabel: 'Ready for Design Review',
    }));
    expect(workspace.workspace.executionReadiness?.stageSummaries.map((stage) => stage.stageId)).toEqual([
      'architecture',
      'planning',
      'generation',
      'runtime',
      'skills',
      'review',
    ]);
    expect(workspace.workspace.executionReadiness?.stageSummaries.find((stage) => stage.stageId === 'generation')?.items).toEqual(expect.arrayContaining([
      { label: 'Preview Package readiness', value: 'Packaged' },
      { label: 'Preview Review status', value: 'Pending' },
    ]));
    expect(workspace.workspace.executionReadiness?.warningSummaries).toEqual(expect.arrayContaining([
      expect.objectContaining({
        category: 'unsupportedCapability',
        message: 'PBIR generation is not implemented.',
      }),
      expect.objectContaining({
        category: 'unsupportedCapability',
        message: 'Microsoft Skills execution is not implemented.',
      }),
    ]));
    expect(workspace.workspace.executionReadiness?.trustBoundary).toEqual(expect.objectContaining({
      executionAllowed: false,
      providerInvocationAllowed: false,
      microsoftSkillsExecutionAllowed: false,
      deploymentAllowed: false,
      automaticAnalyzerLaunchAllowed: false,
    }));
  });

  it('updates reviewer actions without granting validation, mutation, execution, or deployment authority', async () => {
    const context = makeContext(makeTempDir());
    const threadId = 'design-studio:preview-review-actions';
    await recordPreviewReviewHandoff(context, threadId, createPreviewReviewInput(), '2026-06-27T11:45:00.000Z');

    const reviewed = await setPreviewReviewAction(context, threadId, {
      previewReviewId: 'designStudioPreviewReview:phase27',
      reviewerAction: 'markedReviewed',
      reviewerNotes: 'Preview package inspected.',
      reviewerId: 'consultant',
    }, '2026-06-27T12:00:00.000Z');
    const analyzerCandidate = await prepareAnalyzerCandidateMetadata(context, threadId, {
      previewReviewId: 'designStudioPreviewReview:phase27',
      reviewerNotes: 'Metadata can be used for a future manual Analyzer candidate.',
      reviewerId: 'consultant',
    }, '2026-06-27T12:05:00.000Z');

    expect(reviewed.currentReview?.reviewerAction).toBe('markedReviewed');
    expect(reviewed.currentReview?.reviewTimestamp).toBe('2026-06-27T12:00:00.000Z');
    expect(analyzerCandidate.currentReview).toEqual(expect.objectContaining({
      reviewerAction: 'analyzerCandidateMetadataPrepared',
      readinessState: 'readyForAnalyzerCandidateMetadata',
      analyzerCandidateMetadata: expect.objectContaining({
        prepared: true,
        analyzerExecutionRequested: false,
        analyzerLaunchRequested: false,
        validationOccurred: false,
      }),
    }));
    expect(analyzerCandidate.currentReview?.reviewOnlyBoundary).toEqual(expect.objectContaining({
      reportMutationAllowed: false,
      analyzerExecutionAllowed: false,
      microsoftSkillsExecutionAllowed: false,
      providerInvocationAllowed: false,
      deploymentAllowed: false,
      deployablePbirGenerationAllowed: false,
    }));
  });

  it('rejects malformed preview review payloads and forbidden execution boundaries', () => {
    const gate = new DesignStudioPreviewReviewSafetyGate();
    const unsafeInputs = [
      {
        input: createPreviewReviewInput({
          previewPackage: {
            ...createPreviewReviewInput().previewPackage,
            fileInventory: [
              {
                ...createPreviewReviewInput().previewPackage.fileInventory[0],
                relativePath: 'report/report.json',
                reference: 'report/report.json',
              },
            ],
          },
        }),
        reason: 'preview review cannot reference deployable artifact paths: report.json.',
      },
      {
        input: createPreviewReviewInput({
          previewPackage: {
            ...createPreviewReviewInput().previewPackage,
            fileInventory: [
              {
                ...createPreviewReviewInput().previewPackage.fileInventory[0],
                relativePath: 'definition.pbir',
                reference: 'definition.pbir',
              },
            ],
          },
        }),
        reason: 'preview review cannot reference deployable artifact paths: definition.pbir.',
      },
      {
        input: createPreviewReviewInput({
          previewPackage: {
            ...createPreviewReviewInput().previewPackage,
            lineage: {
              ...createPreviewReviewInput().previewPackage.lineage,
              pbirIrRef: '',
            },
          },
        }),
        reason: 'preview review lineage must include preview package, generation manifest, PBIR IR, preview manifest, source write manifest, and immutable lineage references.',
      },
      {
        input: createPreviewReviewInput({
          boundaryRequests: {
            ...createPreviewReviewInput().boundaryRequests,
            automaticAnalyzerExecutionRequested: true,
          },
        }),
        reason: 'automatic Analyzer execution is not allowed from Design Studio preview review.',
      },
      {
        input: createPreviewReviewInput({
          boundaryRequests: {
            ...createPreviewReviewInput().boundaryRequests,
            microsoftSkillsExecutionRequested: true,
            providerInvocationRequested: true,
            apiInvocationRequested: true,
            cliInvocationRequested: true,
            deploymentRequested: true,
          },
        }),
        reason: 'Microsoft Skills, provider, API, CLI, and deployment requests are not allowed from Design Studio preview review.',
      },
    ];

    for (const unsafe of unsafeInputs) {
      expect(gate.validate(unsafe.input)).toEqual(expect.objectContaining({
        isAllowed: false,
        reasons: expect.arrayContaining([unsafe.reason]),
      }));
    }
  });

  it('rejects malformed execution readiness dashboards and forbidden runtime boundary flags', () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Execution Readiness.Report.pbir';
    const threadId = createThreadId(reportPath);
    const gate = new DesignStudioExecutionReadinessSafetyGate();

    return recordPreviewReviewHandoff(context, threadId, createPreviewReviewInput(), '2026-06-27T11:45:00.000Z')
      .then(() => buildDesignStudioWorkspace(context, reportPath))
      .then((workspace) => {
        const dashboard = workspace.workspace.executionReadiness!;

        expect(gate.validate(dashboard).isAllowed).toBe(true);
        expect(gate.validate({
          ...dashboard,
          schemaVersion: 'design-studio-execution-readiness/v2' as 'design-studio-execution-readiness/v1',
        }).reasons).toContain('execution readiness schema version must be design-studio-execution-readiness/v1.');
        expect(gate.validate({
          ...dashboard,
          trustBoundary: {
            ...dashboard.trustBoundary,
            providerInvocationAllowed: true,
          },
        }).reasons).toContain('execution readiness dashboard cannot allow execution, provider invocation, APIs, CLI, deployment, or Analyzer automation.');
      });
  });
});
