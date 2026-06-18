import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';
import { buildStoryAssessmentReportSnapshot } from '../../analyzer/score/storyAssessmentSnapshot';
import type {
  FixPlanItem,
  GuidedStoryImprovements,
  NormalizedFinding,
  ScoreResult,
  StoryAssessmentReportSnapshot,
} from '../../analyzer/contracts/scorePanel';
import type {
  AnalyzerWorkspaceHandoffPayload,
  DesignStudioAnalyzerResultReference,
} from '../contracts/designStudioModels';
import { collectDraftArtifactVersionIds, loadDraftState } from './draftStore';

interface PersistedAnalyzerWorkspaceReturnPayloads {
  storyAssessment?: StoryAssessmentReportSnapshot;
  guidedStoryImprovements?: GuidedStoryImprovements;
  issues?: NormalizedFinding[];
  fixPlan?: FixPlanItem[];
}

interface PersistedAnalyzerWorkspaceReturnRecord {
  threadId: string;
  requestId: string;
  sourceCandidateId: string;
  sourceArtifactVersionFingerprint: string[];
  analyzerResultId: string;
  analyzerRunId: string;
  analyzerCompletionStatus: 'completed';
  validationResultStatus: DesignStudioAnalyzerResultReference['validationResultStatus'];
  validationApprovalState: DesignStudioAnalyzerResultReference['validationApprovalState'];
  scoredAt: string;
  reportPath: string;
  results: DesignStudioAnalyzerResultReference[];
  payloads: PersistedAnalyzerWorkspaceReturnPayloads;
}

export interface PersistedAnalyzerWorkspaceReturn {
  contract: PersistedAnalyzerWorkspaceReturnRecord;
}

function candidateKey(candidateId: string): string {
  return crypto.createHash('md5').update(candidateId).digest('hex').slice(0, 16);
}

function returnsDir(context: vscode.ExtensionContext): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'analyzer-returns');
}

function manifestPath(context: vscode.ExtensionContext, candidateId: string): string {
  return path.join(returnsDir(context), candidateKey(candidateId), 'return.json');
}

function readPersistedReturn(filePath: string): PersistedAnalyzerWorkspaceReturn | undefined {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8')) as PersistedAnalyzerWorkspaceReturn;
  } catch {
    return undefined;
  }
}

function writePersistedReturn(filePath: string, value: PersistedAnalyzerWorkspaceReturn): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(value, null, 2), 'utf8');
}

function sortUnique(values: string[]): string[] {
  return [...new Set(values)].sort((left, right) => left.localeCompare(right));
}

function buildAnalyzerRunId(input: {
  candidateId: string;
  requestId: string;
  analyzerId: string;
  analyzerProfileId: string;
  scoredAt: string;
}): string {
  const digest = crypto
    .createHash('sha256')
    .update(JSON.stringify(input))
    .digest('hex')
    .slice(0, 16);
  return `analyzer-run:${digest}`;
}

function buildAnalyzerResultId(input: {
  analyzerRunId: string;
  sourceCandidateId: string;
  scoredAt: string;
}): string {
  const digest = crypto
    .createHash('sha256')
    .update(JSON.stringify(input))
    .digest('hex')
    .slice(0, 16);
  return `analyzer-result:${digest}`;
}

function buildReference(input: {
  analyzerResultId: string;
  analyzerRunId: string;
  analyzerSource: DesignStudioAnalyzerResultReference['analyzerSource'];
  resultReference: string;
  scoredAt: string;
  sourceCandidateId: string;
  sourceArtifactVersionFingerprint: string[];
  validationResultStatus: DesignStudioAnalyzerResultReference['validationResultStatus'];
  validationApprovalState: DesignStudioAnalyzerResultReference['validationApprovalState'];
  findingReferenceIds: string[];
  recommendationReferenceIds: string[];
  provenance: DesignStudioAnalyzerResultReference['provenance'];
}): DesignStudioAnalyzerResultReference {
  return {
    analyzerResultId: input.analyzerResultId,
    analyzerSource: input.analyzerSource,
    analyzerRunId: input.analyzerRunId,
    resultReference: input.resultReference,
    scoredAt: input.scoredAt,
    sourceCandidateId: input.sourceCandidateId,
    sourceArtifactVersionFingerprint: [...input.sourceArtifactVersionFingerprint],
    analyzerCompletionStatus: 'completed',
    validationResultStatus: input.validationResultStatus,
    validationApprovalState: input.validationApprovalState,
    findingReferenceIds: [...input.findingReferenceIds],
    recommendationReferenceIds: [...input.recommendationReferenceIds],
    linkedProposalIds: [],
    provenance: {
      ...input.provenance,
      notes: input.provenance.notes ? [...input.provenance.notes] : undefined,
    },
  };
}

function collectGuidedImprovementIds(guidedStoryImprovements: GuidedStoryImprovements | undefined): string[] {
  if (!guidedStoryImprovements) {
    return [];
  }

  return [
    ...guidedStoryImprovements.highPriorityImprovements.map((item) => item.id),
    ...guidedStoryImprovements.mediumPriorityImprovements.map((item) => item.id),
  ];
}

function collectStoryAssessmentRecommendationIds(snapshot: StoryAssessmentReportSnapshot): string[] {
  return snapshot.pages.flatMap((page) => page.recommendations.map((recommendation) => recommendation.id));
}

function collectFixPlanFindingIds(fixPlan: FixPlanItem[] | undefined): string[] {
  return fixPlan?.flatMap((item) => item.sourceFindingIds) ?? [];
}

function buildResultReferences(input: {
  analyzerResultId: string;
  analyzerRunId: string;
  scoreResult: ScoreResult;
  sourceCandidateId: string;
  sourceArtifactVersionFingerprint: string[];
  provenance: DesignStudioAnalyzerResultReference['provenance'];
  storyAssessment: StoryAssessmentReportSnapshot;
}): DesignStudioAnalyzerResultReference[] {
  const references: DesignStudioAnalyzerResultReference[] = [];
  const validationResultStatus: DesignStudioAnalyzerResultReference['validationResultStatus'] = 'validated';
  const validationApprovalState: DesignStudioAnalyzerResultReference['validationApprovalState'] = 'notSubmitted';
  const guidedRecommendationIds = collectGuidedImprovementIds(input.scoreResult.guidedStoryImprovements);
  const storyAssessmentRecommendationIds = collectStoryAssessmentRecommendationIds(input.storyAssessment);
  const issueFindingIds = input.scoreResult.normalizedFindings?.map((finding) => finding.id) ?? [];
  const fixPlanFindingIds = collectFixPlanFindingIds(input.scoreResult.fixPlan);

  if (storyAssessmentRecommendationIds.length > 0) {
    references.push(buildReference({
      analyzerResultId: input.analyzerResultId,
      analyzerRunId: input.analyzerRunId,
      analyzerSource: 'storyAssessment',
      resultReference: `story-assessment:${input.scoreResult.scoredAt}`,
      scoredAt: input.scoreResult.scoredAt,
      sourceCandidateId: input.sourceCandidateId,
      sourceArtifactVersionFingerprint: input.sourceArtifactVersionFingerprint,
      validationResultStatus,
      validationApprovalState,
      findingReferenceIds: storyAssessmentRecommendationIds,
      recommendationReferenceIds: storyAssessmentRecommendationIds,
      provenance: input.provenance,
    }));
  }

  if (guidedRecommendationIds.length > 0) {
    references.push(buildReference({
      analyzerResultId: input.analyzerResultId,
      analyzerRunId: input.analyzerRunId,
      analyzerSource: 'guidedStoryImprovements',
      resultReference: `guided-story:${input.scoreResult.scoredAt}`,
      scoredAt: input.scoreResult.scoredAt,
      sourceCandidateId: input.sourceCandidateId,
      sourceArtifactVersionFingerprint: input.sourceArtifactVersionFingerprint,
      validationResultStatus,
      validationApprovalState,
      findingReferenceIds: guidedRecommendationIds,
      recommendationReferenceIds: guidedRecommendationIds,
      provenance: input.provenance,
    }));
  }

  if (issueFindingIds.length > 0 || references.length === 0) {
    references.push(buildReference({
      analyzerResultId: input.analyzerResultId,
      analyzerRunId: input.analyzerRunId,
      analyzerSource: 'issues',
      resultReference: `issues:${input.scoreResult.scoredAt}`,
      scoredAt: input.scoreResult.scoredAt,
      sourceCandidateId: input.sourceCandidateId,
      sourceArtifactVersionFingerprint: input.sourceArtifactVersionFingerprint,
      validationResultStatus,
      validationApprovalState,
      findingReferenceIds: issueFindingIds,
      recommendationReferenceIds: issueFindingIds,
      provenance: input.provenance,
    }));
  }

  if ((input.scoreResult.fixPlan?.length ?? 0) > 0) {
    references.push(buildReference({
      analyzerResultId: input.analyzerResultId,
      analyzerRunId: input.analyzerRunId,
      analyzerSource: 'fixPlan',
      resultReference: `fix-plan:${input.scoreResult.scoredAt}`,
      scoredAt: input.scoreResult.scoredAt,
      sourceCandidateId: input.sourceCandidateId,
      sourceArtifactVersionFingerprint: input.sourceArtifactVersionFingerprint,
      validationResultStatus,
      validationApprovalState,
      findingReferenceIds: fixPlanFindingIds,
      recommendationReferenceIds: input.scoreResult.fixPlan?.map((item) => item.id) ?? [],
      provenance: input.provenance,
    }));
  }

  return references;
}

export async function recordAnalyzerWorkspaceReturn(
  context: vscode.ExtensionContext,
  input: {
    handoff: AnalyzerWorkspaceHandoffPayload;
    scoreResult: ScoreResult;
  },
): Promise<PersistedAnalyzerWorkspaceReturnRecord> {
  const draftState = await loadDraftState(context, input.handoff.threadId);
  const sourceArtifactVersionFingerprint = sortUnique(
    draftState
      ? collectDraftArtifactVersionIds(draftState)
      : input.handoff.sourceDesignArtifactVersionReferences,
  );
  const analyzerRunId = buildAnalyzerRunId({
    candidateId: input.handoff.candidateId,
    requestId: input.handoff.requestId,
    analyzerId: input.handoff.analyzerId,
    analyzerProfileId: input.handoff.analyzerProfileId,
    scoredAt: input.scoreResult.scoredAt,
  });
  const analyzerResultId = buildAnalyzerResultId({
    analyzerRunId,
    sourceCandidateId: input.handoff.candidateId,
    scoredAt: input.scoreResult.scoredAt,
  });
  const storyAssessment = buildStoryAssessmentReportSnapshot(input.scoreResult);
  const provenance: DesignStudioAnalyzerResultReference['provenance'] = {
    source: 'analyzerWorkspace',
    requestId: input.handoff.requestId,
    timestamp: input.scoreResult.scoredAt,
    notes: [
      'Returned from Analyzer Workspace through the real analyzer return path.',
      'Design Studio remains read-only with respect to analyzer findings and validation approval.',
    ],
  };
  const results = buildResultReferences({
    analyzerResultId,
    analyzerRunId,
    scoreResult: input.scoreResult,
    sourceCandidateId: input.handoff.candidateId,
    sourceArtifactVersionFingerprint,
    provenance,
    storyAssessment,
  });

  const contract: PersistedAnalyzerWorkspaceReturnRecord = {
    threadId: input.handoff.threadId,
    requestId: input.handoff.requestId,
    sourceCandidateId: input.handoff.candidateId,
    sourceArtifactVersionFingerprint,
    analyzerResultId,
    analyzerRunId,
    analyzerCompletionStatus: 'completed',
    validationResultStatus: 'validated',
    validationApprovalState: 'notSubmitted',
    scoredAt: input.scoreResult.scoredAt,
    reportPath: input.scoreResult.reportPath,
    results,
    payloads: {
      storyAssessment,
      guidedStoryImprovements: input.scoreResult.guidedStoryImprovements,
      issues: input.scoreResult.normalizedFindings ?? [],
      fixPlan: input.scoreResult.fixPlan ?? [],
    },
  };

  writePersistedReturn(manifestPath(context, input.handoff.candidateId), { contract });
  return contract;
}

export async function loadAnalyzerWorkspaceReturn(
  context: vscode.ExtensionContext,
  candidateId: string,
): Promise<PersistedAnalyzerWorkspaceReturnRecord | undefined> {
  return readPersistedReturn(manifestPath(context, candidateId))?.contract;
}

export async function loadAnalyzerWorkspaceReturnPayloads(
  context: vscode.ExtensionContext,
  candidateId: string,
): Promise<PersistedAnalyzerWorkspaceReturnPayloads | undefined> {
  return readPersistedReturn(manifestPath(context, candidateId))?.contract.payloads;
}

export async function validateAnalyzerWorkspaceReturnResults(
  context: vscode.ExtensionContext,
  results: DesignStudioAnalyzerResultReference[],
): Promise<PersistedAnalyzerWorkspaceReturnRecord> {
  const candidateId = results[0]?.sourceCandidateId;
  if (!candidateId) {
    throw new Error('Analyzer return validation requires a source candidate id.');
  }

  const contract = await loadAnalyzerWorkspaceReturn(context, candidateId);
  if (!contract) {
    throw new Error('No persisted Analyzer Workspace return exists for the active review candidate.');
  }

  const persistedKeys = new Set(contract.results.map((result) => `${result.analyzerRunId}::${result.resultReference}`));
  for (const result of results) {
    if (result.analyzerResultId !== contract.analyzerResultId) {
      throw new Error('Analyzer return validation requires a matching analyzer result id.');
    }

    if (!persistedKeys.has(`${result.analyzerRunId}::${result.resultReference}`)) {
      throw new Error('Analyzer return validation requires persisted analyzer result references.');
    }
  }

  return contract;
}
