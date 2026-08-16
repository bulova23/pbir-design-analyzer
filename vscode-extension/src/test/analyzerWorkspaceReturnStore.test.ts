import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import type { ScoreResult } from '../analyzer/contracts/scorePanel';
import type { AnalyzerWorkspaceHandoffPayload, MaterializedSurfaceCandidate } from '../design-studio/contracts/designStudioModels';
import { recordAnalyzerWorkspaceReturn } from '../design-studio/state/analyzerWorkspaceReturnStore';
import {
  attachAnalyzerResultsAtomically,
  loadIterationState,
} from '../design-studio/state/iterationStore';
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
  collectDraftArtifactVersionIds,
  generateDraftArtifacts,
  submitDraftForApproval,
  type DraftState,
} from '../design-studio/state/draftStore';
import {
  approveReviewCandidate,
  createReviewCandidate,
  loadPrepareForReviewState,
  submitReviewCandidateForApproval,
} from '../design-studio/state/prepareForReviewStore';
import { loadRefinementState } from '../design-studio/state/refinementStore';
import {
  loadReviewDesignState,
  markReviewCompleted,
  recordReviewLaunch,
  syncDiscoveredAnalyzerResults,
} from '../design-studio/state/reviewDesignStore';

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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-analyzer-return-test-'));
}

function createThreadId(reportPath: string): string {
  return `design-studio:${crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16)}`;
}

function analyzerReturnManifestPath(rootPath: string, candidateId: string): string {
  const key = crypto.createHash('md5').update(candidateId).digest('hex').slice(0, 16);
  return path.join(rootPath, 'design-studio', 'analyzer-returns', key, 'return.json');
}

async function createApprovedDraftWorkflow(context: ExtensionContext, threadId: string): Promise<DraftState> {
  await saveDesignBriefDraft(context, threadId, {
    audience: 'Sales leaders',
    businessObjective: 'Reduce missed renewals',
    keyDecisions: ['Which regions need intervention first'],
    primaryKpis: ['Renewal rate', 'Gross margin'],
    dimensions: ['Region', 'Segment'],
    intendedStory: 'Lead with risk, then explain the drivers and next actions.',
    successCriteria: ['Leader can decide the next action within five minutes'],
    reportType: 'dashboard',
    navigationExpectations: 'Overview first, then regional detail.',
  });
  await submitDesignBriefForApproval(context, threadId);
  await approveDesignBrief(context, threadId);
  const conceptState = await generateConceptArtifacts(context, threadId);
  await selectConceptBaseline(context, threadId, conceptState.currentConcept.alternateConcepts[0].id);
  await submitConceptBaselineForApproval(context, threadId);
  await approveConceptBaseline(context, threadId);
  await generateDraftArtifacts(context, threadId);
  await submitDraftForApproval(context, threadId);
  return approveDraftArtifacts(context, threadId);
}

function makeHandoffPayload(
  threadId: string,
  requestId: string,
  candidate: MaterializedSurfaceCandidate,
  reportPath: string,
): AnalyzerWorkspaceHandoffPayload {
  return {
    threadId,
    requestId,
    candidateId: candidate.id,
    candidateLineage: candidate.sourceLineage.map((entry) => ({ ...entry })),
    candidateProvenance: { ...candidate.provenance },
    candidateProvenanceTrace: candidate.provenanceTrace.map((entry) => ({ ...entry })),
    sourceDesignArtifactReferences: [...candidate.sourceArtifactIds],
    sourceDesignArtifactVersionReferences: candidate.sourceLineage.map((entry) => entry.artifactVersionId),
    materializationDiagnostics: [...candidate.materializationDiagnostics],
    analyzerId: candidate.analyzerHandoff.metadata.targetAnalyzer,
    analyzerProfileId: candidate.analyzerHandoff.metadata.targetAnalyzerProfile,
    surfaceFamily: candidate.targetSurfaceType,
    executableEligibility: 'executable',
    handoffReference: { kind: 'repositoryBackedSurface', repositoryPath: reportPath },
    handoffDiagnostics: [...candidate.materializationDiagnostics],
    compatibilityStatus: 'compatible',
    compatibilityDiagnostics: [],
  };
}

function makeScoreResult(reportPath: string, pageName: string): ScoreResult {
  return {
    reportPath,
    scoredAt: '2026-06-17T19:45:00.000Z',
    compositeScore: 81,
    gestaltScore: 82,
    cognitiveLoadScore: 79,
    dataInkScore: 80,
    accessibilityScore: 78,
    visualBestPracticesScore: 83,
    stephenFewScore: 81,
    enterpriseGovernanceScore: 84,
    tufteScore: 77,
    graphicalPerceptionScore: 79,
    densityScore: 76,
    narrativeScore: 82,
    feedback: {},
    pageCount: 1,
    recommendations: [],
    normalizedFindings: [
      {
        id: 'finding-real-return-1',
        title: 'Navigation drift',
        summary: 'Users do not move cleanly from summary to detail.',
        severity: 'high',
        confidence: 0.92,
        scope: 'crossPage',
        detectionType: 'deterministic',
        affectedPages: [pageName],
        impactArea: 'navigation',
        frameworkImpact: ['narrative'],
        recommendation: 'Reduce the number of branches and clarify the handoff from summary to detail.',
        sourceKind: 'issues',
        sourceSection: 'issues',
        evidence: [
          {
            kind: 'storyAssessment',
            label: 'Narrative breakdown',
            pageName,
          },
        ],
      },
    ],
    fixPlan: [],
  };
}

describe('analyzerWorkspaceReturnStore', () => {
  it('discovers a real analyzer return for the active review candidate without seeded injection', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Real Analyzer Return.Report.pbir';
    const threadId = createThreadId(reportPath);
    const draftState = await createApprovedDraftWorkflow(context, threadId);
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';

    await createReviewCandidate(context, { threadId, reportPath });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedReview = await approveReviewCandidate(context, threadId);
    await recordReviewLaunch(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      analyzerId: approvedReview.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedReview.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });

    await recordAnalyzerWorkspaceReturn(context, {
      handoff: makeHandoffPayload(threadId, approvedReview.currentRequest.id, approvedReview.currentCandidate, reportPath),
      scoreResult: makeScoreResult(reportPath, pageName),
    });

    await syncDiscoveredAnalyzerResults(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });

    const reviewState = await loadReviewDesignState(context, threadId);
    expect(reviewState?.currentReview?.availableResults).toEqual([
      expect.objectContaining({
        analyzerSource: 'issues',
        sourceCandidateId: approvedReview.currentCandidate.id,
        analyzerCompletionStatus: 'completed',
        validationResultStatus: 'validated',
        validationApprovalState: 'notSubmitted',
        findingReferenceIds: ['finding-real-return-1'],
      }),
    ]);
  });

  it('rejects discovered analyzer returns when candidate lineage is invalid', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Invalid Analyzer Return.Report.pbir';
    const threadId = createThreadId(reportPath);
    const draftState = await createApprovedDraftWorkflow(context, threadId);
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';

    await createReviewCandidate(context, { threadId, reportPath });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedReview = await approveReviewCandidate(context, threadId);
    await recordReviewLaunch(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      analyzerId: approvedReview.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedReview.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });

    await recordAnalyzerWorkspaceReturn(context, {
      handoff: makeHandoffPayload(threadId, approvedReview.currentRequest.id, approvedReview.currentCandidate, reportPath),
      scoreResult: makeScoreResult(reportPath, pageName),
    });

    const manifestPath = analyzerReturnManifestPath(context.globalStorageUri.fsPath, approvedReview.currentCandidate.id);
    const persisted = JSON.parse(fs.readFileSync(manifestPath, 'utf8')) as {
      contract: {
        results: Array<{ sourceCandidateId: string }>;
      };
    };
    persisted.contract.results[0]!.sourceCandidateId = `${approvedReview.currentCandidate.id}:different`;
    fs.writeFileSync(manifestPath, JSON.stringify(persisted, null, 2), 'utf8');

    await expect(syncDiscoveredAnalyzerResults(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    })).rejects.toThrow('active review candidate');
  });

  it('attaches real analyzer returns atomically, ingests refinement proposals, and records iteration lineage', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Attach Real Analyzer Return.Report.pbir';
    const threadId = createThreadId(reportPath);
    const draftState = await createApprovedDraftWorkflow(context, threadId);
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';

    await createReviewCandidate(context, { threadId, reportPath });
    await submitReviewCandidateForApproval(context, threadId);
    const approvedReview = await approveReviewCandidate(context, threadId);
    await recordReviewLaunch(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
      analyzerId: approvedReview.currentRequest.targetAnalyzer,
      analyzerProfileId: approvedReview.currentRequest.targetAnalyzerProfile,
    });
    await markReviewCompleted(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });

    await recordAnalyzerWorkspaceReturn(context, {
      handoff: makeHandoffPayload(threadId, approvedReview.currentRequest.id, approvedReview.currentCandidate, reportPath),
      scoreResult: makeScoreResult(reportPath, pageName),
    });
    await syncDiscoveredAnalyzerResults(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });

    const attached = await attachAnalyzerResultsAtomically(context, threadId, {
      requestId: approvedReview.currentRequest.id,
      candidate: approvedReview.currentCandidate,
    });

    expect(attached.ok).toBe(true);

    const refinementState = await loadRefinementState(context, threadId);
    expect(refinementState?.proposals).toEqual(expect.arrayContaining([
      expect.objectContaining({
        recommendationState: 'proposed',
        sourceAnalyzerOutput: expect.objectContaining({
          analyzerRunId: expect.any(String),
          resultReference: expect.stringContaining('issues:'),
          sourceCandidateId: approvedReview.currentCandidate.id,
          sourceArtifactVersionFingerprint: expect.arrayContaining(collectDraftArtifactVersionIds(draftState)),
        }),
      }),
    ]));

    const reviewState = await loadReviewDesignState(context, threadId);
    expect(reviewState?.currentReview?.attachedResults).toHaveLength(1);

    const iterationState = await loadIterationState(context, threadId);
    expect(iterationState?.iterations.at(-1)).toEqual(expect.objectContaining({
      analyzerResults: expect.arrayContaining([
        expect.objectContaining({
          analyzerRunId: expect.any(String),
          resultReference: expect.stringContaining('issues:'),
          analyzerSource: 'issues',
        }),
      ]),
      comparisonSnapshot: expect.objectContaining({
        recommendations: expect.arrayContaining([
          expect.objectContaining({
            proposalId: refinementState?.proposals[0]?.id,
            recommendationState: 'proposed',
          }),
        ]),
      }),
    }));
    expect(iterationState?.iterations.at(-1)?.workflowCompletion.checklist).toEqual(expect.arrayContaining([
      expect.objectContaining({ label: 'Analyzer results attached', satisfied: true }),
      expect.objectContaining({ label: 'Validation approval status recorded', satisfied: false }),
    ]));
    expect((await loadPrepareForReviewState(context, threadId))?.currentCandidate?.id).toBe(approvedReview.currentCandidate.id);
  });
});
