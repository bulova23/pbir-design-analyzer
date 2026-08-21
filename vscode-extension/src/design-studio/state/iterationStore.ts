import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';
import {
  buildValidationApprovalEvidence,
  getRecommendationState,
  hasAnalyzerOwnedValidationApproval,
  isUnresolvedRecommendationState,
} from '../contracts/designStudioModels';
import type {
  DesignArtifactApprovalKind,
  ClosedLoopIterationComparison,
  DesignStudioAnalyzerResultReference,
  DesignArtifactApprovalState,
  DesignArtifactProvenance,
  DesignArtifactValidationLinkage,
  DesignIterationRecord,
  IterationCompletionChecklistItem,
  IterationWorkflowCompletion,
  DraftLayoutArtifact,
  DraftNavigationArtifact,
  DraftPageArtifact,
  DraftReportArtifact,
  IterationAnalyzerResultLink,
  IterationApprovalState,
  IterationComparisonSnapshot,
  IterationGuardrails,
  IterationMaterializedCandidateLink,
  IterationRefinementProposalLink,
  MaterializedSurfaceCandidate,
  RefinementProposal,
  RefinementSourceAnalyzerOutput,
  ReportConcept,
} from '../contracts/designStudioModels';
import {
  collectDraftArtifactVersionIds,
  loadDraftState,
} from './draftStore';
import { loadConceptState } from './conceptStore';
import { loadDesignBriefState } from './designBriefStore';
import { loadPrepareForReviewState } from './prepareForReviewStore';
import {
  attachAnalyzerResultLineage,
  ingestFixPlanItems,
  ingestGuidedStoryImprovements,
  ingestIssues,
  ingestStoryAssessmentOutput,
  loadRefinementState,
} from './refinementStore';
import { loadReviewDesignState, markAnalyzerResultsAttached } from './reviewDesignStore';
import { buildIterationComparison } from '../presentation/iterationExperience';
import {
  loadAnalyzerWorkspaceReturn,
  loadAnalyzerWorkspaceReturnPayloads,
  validateAnalyzerWorkspaceReturnResults,
} from './analyzerWorkspaceReturnStore';

export interface IterationState {
  threadId: string;
  iterations: DesignIterationRecord[];
}

type PersistedIterationState = IterationState;

export interface RecordIterationInput {
  threadId: string;
  previousIterationId?: string;
  sourceArtifactVersionIds: string[];
  concept?: ReportConcept;
  draft?: DraftReportArtifact;
  pageArtifacts?: DraftPageArtifact[];
  layoutArtifacts?: DraftLayoutArtifact[];
  navigationArtifacts?: DraftNavigationArtifact[];
  materializedCandidate?: MaterializedSurfaceCandidate;
  analyzerOutputs: RefinementSourceAnalyzerOutput[];
  refinementProposals: RefinementProposal[];
  validationApproval?: {
    approvalState: DesignArtifactApprovalState;
    provenance: Pick<DesignArtifactProvenance, 'source'>;
    validationLinkage?: DesignArtifactValidationLinkage;
  };
}

export type IterationCompletionEvaluation = IterationWorkflowCompletion;
export type AtomicAnalyzerResultAttachment =
  | { ok: true; iterationState: IterationState }
  | { ok: false; error: string };

function threadKey(threadId: string): string {
  return crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'threads', threadKey(threadId));
}

function manifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'closed-loop.json');
}

function reviewDesignManifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'review-design.json');
}

function refinementManifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'refinement-studio.json');
}

function readPersistedState(filePath: string): PersistedIterationState | undefined {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8')) as PersistedIterationState;
  } catch {
    return undefined;
  }
}

function writePersistedState(filePath: string, state: PersistedIterationState): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(state, null, 2), 'utf8');
}

function snapshotFile(filePath: string): string | undefined {
  try {
    return fs.readFileSync(filePath, 'utf8');
  } catch {
    return undefined;
  }
}

function restoreFile(filePath: string, snapshot: string | undefined): void {
  if (snapshot === undefined) {
    try {
      fs.unlinkSync(filePath);
    } catch {
      // Nothing to restore if the file never existed or was already removed.
    }
    return;
  }

  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, snapshot, 'utf8');
}

function sortUnique(values: string[]): string[] {
  return [...new Set(values)].sort((left, right) => left.localeCompare(right));
}

function matchesVersionId<T extends { id: string; version: number }>(artifact: T, versionId: string): boolean {
  return `${artifact.id}@v${artifact.version}` === versionId;
}

function validateAttachableAnalyzerResults(
  candidate: MaterializedSurfaceCandidate,
  results: DesignStudioAnalyzerResultReference[],
): void {
  if (results.length === 0) {
    throw new Error('No analyzer results are available to attach.');
  }

  const expectedFingerprint = sortUnique(candidate.sourceLineage.map((entry) => entry.artifactVersionId));
  for (const result of results) {
    if (typeof result.sourceCandidateId !== 'string' || result.sourceCandidateId.trim().length === 0) {
      throw new Error('Analyzer result attachment requires preserved source candidate lineage.');
    }
    if (result.sourceCandidateId !== candidate.id) {
      throw new Error('Analyzer result attachment must match the active review candidate lineage.');
    }

    const fingerprint = sortUnique(result.sourceArtifactVersionFingerprint ?? []);
    if (fingerprint.length === 0) {
      throw new Error('Analyzer result attachment requires a preserved source artifact/version fingerprint.');
    }
    if (
      expectedFingerprint.some((value) => !fingerprint.includes(value))
    ) {
      throw new Error('Analyzer result attachment must preserve the active review candidate source artifact/version fingerprint.');
    }
  }
}

async function validateIterationInput(
  context: vscode.ExtensionContext,
  existing: IterationState | undefined,
  input: RecordIterationInput,
): Promise<void> {
  if (
    input.validationApproval?.approvalState === 'approved'
    && !hasAnalyzerOwnedValidationApproval({
      approvalKind: 'validationApproval',
      provenance: input.validationApproval.provenance,
      validationLinkage: input.validationApproval.validationLinkage,
    })
  ) {
    throw new Error('Validation approval requires analyzer-owned provenance.');
  }

  const draftState = await loadDraftState(context, input.threadId);
  if (!draftState) {
    throw new Error(`No Draft Studio state exists for thread ${input.threadId}.`);
  }

  const expectedFingerprint = sortUnique(collectDraftArtifactVersionIds(draftState));
  const providedFingerprint = sortUnique(input.sourceArtifactVersionIds);
  if (
    expectedFingerprint.length !== providedFingerprint.length
    || expectedFingerprint.some((value, index) => value !== providedFingerprint[index])
  ) {
    throw new Error('Iteration source artifact versions do not match the persisted Draft Studio state.');
  }

  if (input.previousIterationId && !existing?.iterations.some((iteration) => iteration.id === input.previousIterationId)) {
    throw new Error('Previous iteration must exist before a follow-on iteration can be recorded.');
  }

  if (input.concept && !matchesVersionId(draftState.concept, `${input.concept.id}@v${input.concept.version}`)) {
    throw new Error('Iteration concept snapshot must match the persisted Draft Studio state.');
  }
  if (input.draft && !matchesVersionId(draftState.currentDraft, `${input.draft.id}@v${input.draft.version}`)) {
    throw new Error('Iteration draft snapshot must match the persisted Draft Studio state.');
  }
  if ((input.pageArtifacts ?? []).some((artifact) =>
    !draftState.pageArtifacts.some((persisted) => matchesVersionId(persisted, `${artifact.id}@v${artifact.version}`))
  )) {
    throw new Error('Iteration page artifact snapshots must match the persisted Draft Studio state.');
  }
  if ((input.layoutArtifacts ?? []).some((artifact) =>
    !draftState.layoutArtifacts.some((persisted) => matchesVersionId(persisted, `${artifact.id}@v${artifact.version}`))
  )) {
    throw new Error('Iteration layout artifact snapshots must match the persisted Draft Studio state.');
  }
  if ((input.navigationArtifacts ?? []).some((artifact) =>
    !draftState.navigationArtifacts.some((persisted) => matchesVersionId(persisted, `${artifact.id}@v${artifact.version}`))
  )) {
    throw new Error('Iteration navigation artifact snapshots must match the persisted Draft Studio state.');
  }

  if (input.materializedCandidate) {
    const candidateLineage = sortUnique(input.materializedCandidate.sourceLineage.map((entry) => entry.artifactVersionId));
    if (candidateLineage.some((versionId) => !providedFingerprint.includes(versionId))) {
      throw new Error('Materialized candidate lineage must match the iteration source artifact versions.');
    }
  }

  if (input.materializedCandidate && input.analyzerOutputs.some((output) =>
    output.sourceCandidateId !== input.materializedCandidate?.id
    || sortUnique(output.sourceArtifactVersionFingerprint ?? []).join('|') !== providedFingerprint.join('|')
  )) {
    throw new Error('Analyzer outputs must reference the iteration materialized candidate lineage.');
  }

  const refinementState = input.refinementProposals.length > 0
    ? await loadRefinementState(context, input.threadId)
    : undefined;
  const persistedProposals = new Map((refinementState?.proposals ?? []).map((proposal) => [proposal.id, proposal]));
  for (const proposal of input.refinementProposals) {
    const persisted = persistedProposals.get(proposal.id);
    if (!persisted) {
      throw new Error('Iteration refinement proposals must exist in persisted Refinement Studio state.');
    }

    const matchingOutput = input.analyzerOutputs.find((output) =>
      output.analyzerRunId === proposal.sourceAnalyzerOutput.analyzerRunId
      && output.resultReference === proposal.sourceAnalyzerOutput.resultReference
    );
    if (
      !matchingOutput
      || proposal.sourceAnalyzerOutput.sourceCandidateId !== input.materializedCandidate?.id
      || sortUnique(proposal.sourceAnalyzerOutput.sourceArtifactVersionFingerprint ?? []).join('|') !== providedFingerprint.join('|')
      || proposal.sourceAnalyzerOutput.analyzerRunId !== persisted.sourceAnalyzerOutput.analyzerRunId
      || proposal.sourceAnalyzerOutput.resultReference !== persisted.sourceAnalyzerOutput.resultReference
    ) {
      throw new Error('Refinement proposals must preserve analyzer candidate lineage and source artifact fingerprints.');
    }
  }

  if (input.validationApproval?.validationLinkage) {
    const linkage = input.validationApproval.validationLinkage;
    const matchingOutput = input.analyzerOutputs.find((output) =>
      output.analyzerRunId === linkage.analyzerRunId
      && output.resultReference === linkage.resultReference
    );
    if (
      !matchingOutput
      || linkage.sourceCandidateId !== input.materializedCandidate?.id
      || sortUnique(linkage.sourceArtifactVersionFingerprint ?? []).join('|') !== providedFingerprint.join('|')
    ) {
      throw new Error('Validation approval must reference an analyzer result recorded against the iteration materialized candidate lineage.');
    }
  }
}

function summarizeMaterializedCandidate(
  candidate?: MaterializedSurfaceCandidate,
): IterationMaterializedCandidateLink | undefined {
  if (!candidate) {
    return undefined;
  }

  return {
    candidateId: candidate.id,
    sourceLineage: candidate.sourceLineage.map((entry) => entry.artifactVersionId),
    targetSurfaceType: candidate.targetSurfaceType,
    analyzerHandoffReference: candidate.analyzerHandoff.reference.kind,
    materializationMode: candidate.materializationMode,
  };
}

function summarizeAnalyzerOutputs(
  analyzerOutputs: RefinementSourceAnalyzerOutput[],
  validationApproval?: RecordIterationInput['validationApproval'],
): IterationAnalyzerResultLink[] {
  return analyzerOutputs.map((output) => ({
    analyzerSource: output.analyzerSource,
    analyzerRunId: output.analyzerRunId,
    resultReference: output.resultReference,
    scoredAt: output.scoredAt,
    validationResultStatus: validationApproval?.validationLinkage?.analyzerRunId === output.analyzerRunId
      ? validationApproval.validationLinkage.validationResultStatus
      : undefined,
  }));
}

function summarizeRefinementProposals(
  proposals: RefinementProposal[],
): IterationRefinementProposalLink[] {
  return proposals.map((proposal) => ({
    proposalId: proposal.id,
    approvalState: proposal.approvalState,
    recommendationState: getRecommendationState(proposal),
    suggestedDesignChange: proposal.suggestedDesignChange,
    expectedImpact: proposal.expectedImpact,
    linkedFindingIds: [...proposal.linkedFindingIds],
  }));
}

function summarizeApprovals(input: RecordIterationInput): IterationApprovalState {
  const designApprovalState: DesignArtifactApprovalState = input.concept?.approvalState === 'approved'
    && input.draft?.approvalState === 'approved'
    ? 'approved'
    : input.concept || input.draft
      ? 'pendingApproval'
      : 'notSubmitted';

  const materializationApprovalState: DesignArtifactApprovalState = input.materializedCandidate?.approvalState ?? 'notSubmitted';

  const refinementApprovalState: DesignArtifactApprovalState = input.refinementProposals.length === 0
    ? 'notSubmitted'
    : input.refinementProposals.every((proposal) => getRecommendationState(proposal) === 'approved')
      ? 'approved'
      : input.refinementProposals.every((proposal) => getRecommendationState(proposal) === 'rejected')
        ? 'rejected'
        : 'pendingApproval';

  if (
    input.validationApproval?.approvalState === 'approved'
    && !hasAnalyzerOwnedValidationApproval({
      approvalKind: 'validationApproval',
      provenance: input.validationApproval.provenance,
      validationLinkage: input.validationApproval.validationLinkage,
    })
  ) {
    throw new Error('Validation approval requires analyzer-owned provenance.');
  }

  return {
    designApproval: {
      approvalKind: 'designApproval',
      approvalState: designApprovalState,
    },
    materializationApproval: {
      approvalKind: 'materializationApproval',
      approvalState: materializationApprovalState,
    },
    refinementApproval: {
      approvalKind: 'refinementApproval',
      approvalState: refinementApprovalState,
    },
    validationApproval: {
      approvalKind: 'validationApproval',
      approvalState: input.validationApproval?.approvalState ?? 'notSubmitted',
      owner: input.validationApproval?.provenance.source === 'analyzerWorkspace'
        ? 'analyzerWorkspace'
        : undefined,
      analyzerRunId: input.validationApproval?.validationLinkage?.analyzerRunId,
      resultReference: input.validationApproval?.validationLinkage?.resultReference,
      sourceCandidateId: input.validationApproval?.validationLinkage?.sourceCandidateId,
      sourceArtifactVersionFingerprint: input.validationApproval?.validationLinkage?.sourceArtifactVersionFingerprint,
      validationResultStatus: input.validationApproval?.validationLinkage?.validationResultStatus,
    },
  };
}

function buildComparisonSnapshot(input: RecordIterationInput): IterationComparisonSnapshot {
  return {
    concept: input.concept
      ? {
        summary: input.concept.summary,
        pageTitles: input.concept.pageConcepts.map((page) => page.title),
        navigationPattern: input.concept.navigationStructure.pattern,
      }
      : undefined,
    draft: input.draft
      ? {
        summary: input.draft.summary,
        pageStructureSummaries: (input.pageArtifacts ?? []).map((artifact) => artifact.structureSummary),
        layoutTitles: (input.layoutArtifacts ?? []).map((artifact) => artifact.title),
        navigationFrameworks: (input.navigationArtifacts ?? []).map((artifact) => artifact.frameworkType),
      }
      : undefined,
    analyzerOutputs: summarizeAnalyzerOutputs(input.analyzerOutputs, input.validationApproval).map((output) => ({
      resultReference: output.resultReference,
      analyzerRunId: output.analyzerRunId,
      analyzerSource: output.analyzerSource,
      validationResultStatus: output.validationResultStatus,
    })),
    recommendations: summarizeRefinementProposals(input.refinementProposals).map((proposal) => ({
      proposalId: proposal.proposalId,
      suggestedDesignChange: proposal.suggestedDesignChange,
      expectedImpact: proposal.expectedImpact,
      approvalState: proposal.approvalState,
      recommendationState: proposal.recommendationState,
    })),
    validationStatus: input.validationApproval?.validationLinkage?.validationResultStatus
      ?? input.validationApproval?.approvalState
      ?? 'notSubmitted',
  };
}

function createSummary(snapshot: IterationComparisonSnapshot): string {
  const conceptSummary = snapshot.concept?.summary ?? 'No concept snapshot recorded';
  const draftSummary = snapshot.draft?.summary ?? 'No draft snapshot recorded';
  return `${conceptSummary} | ${draftSummary}`;
}

function buildGuardrails(): IterationGuardrails {
  return {
    autoOptimizationTriggered: false,
    analyzerExecutionTriggered: false,
    reportMutationTriggered: false,
    pbirFilesGenerated: false,
  };
}

function buildCompletionChecklist(input: {
  briefApproved: boolean;
  conceptApproved: boolean;
  draftApproved: boolean;
  candidateApproved: boolean;
  reviewCompleted: boolean;
  analyzerResultsAttached: boolean;
  refinementReviewed: boolean;
  validationApprovalRecorded: boolean;
}): IterationCompletionChecklistItem[] {
  return [
    { id: 'briefApproved', label: 'Design Brief approved', satisfied: input.briefApproved, required: true },
    { id: 'conceptApproved', label: 'Concept approved', satisfied: input.conceptApproved, required: true },
    { id: 'draftApproved', label: 'Draft approved', satisfied: input.draftApproved, required: true },
    { id: 'candidateApproved', label: 'Review candidate approved', satisfied: input.candidateApproved, required: true },
    { id: 'reviewCompleted', label: 'Review Design completed', satisfied: input.reviewCompleted, required: true },
    { id: 'analyzerResultsAttached', label: 'Analyzer results attached', satisfied: input.analyzerResultsAttached, required: true },
    { id: 'refinementReviewed', label: 'Refinement reviewed', satisfied: input.refinementReviewed, required: true },
    { id: 'validationApprovalRecorded', label: 'Validation approval status recorded', satisfied: input.validationApprovalRecorded, required: false },
  ];
}

function createWorkflowCompletion(input: {
  state: IterationWorkflowCompletion['state'];
  checklist: IterationCompletionChecklistItem[];
  outstandingItems: string[];
  approvalsSatisfied: DesignArtifactApprovalKind[];
  deferredRecommendationCount: number;
  unresolvedRecommendationCount: number;
  completedAt?: string;
  completedBy?: IterationWorkflowCompletion['completedBy'];
  reopenedAt?: string;
  reopenedBy?: IterationWorkflowCompletion['reopenedBy'];
  history?: IterationWorkflowCompletion['history'];
}): IterationWorkflowCompletion {
  const isEligible = input.checklist.every((item) => item.required ? item.satisfied : true);
  let nextStepGuidance = 'Complete required workflow stages before closing this iteration.';
  if (input.state === 'completed') {
    nextStepGuidance = 'Iteration completed. You may reopen if additional refinement is required.';
  } else if (isEligible) {
    nextStepGuidance = 'This iteration is ready for completion.';
  }

  return {
    state: input.state,
    isEligible,
    checklist: input.checklist.map((item) => ({ ...item })),
    outstandingItems: [...input.outstandingItems],
    approvalsSatisfied: [...input.approvalsSatisfied],
    deferredRecommendationCount: input.deferredRecommendationCount,
    unresolvedRecommendationCount: input.unresolvedRecommendationCount,
    nextStepGuidance,
    completedAt: input.completedAt,
    completedBy: input.completedBy,
    reopenedAt: input.reopenedAt,
    reopenedBy: input.reopenedBy,
    history: [...(input.history ?? [])],
  };
}

function deriveWorkflowCompletion(input: {
  designApprovalState: DesignArtifactApprovalState;
  materializationApprovalState: DesignArtifactApprovalState;
  validationApprovalState: DesignArtifactApprovalState;
  refinementProposals: IterationRefinementProposalLink[];
  reviewCompleted: boolean;
  analyzerResultsAttached: boolean;
  refinementReviewed: boolean;
  previous?: IterationWorkflowCompletion;
}): IterationWorkflowCompletion {
  const checklist = buildCompletionChecklist({
    briefApproved: input.designApprovalState === 'approved',
    conceptApproved: input.designApprovalState === 'approved',
    draftApproved: input.designApprovalState === 'approved',
    candidateApproved: input.materializationApprovalState === 'approved',
    reviewCompleted: input.reviewCompleted,
    analyzerResultsAttached: input.analyzerResultsAttached,
    refinementReviewed: input.refinementReviewed,
    validationApprovalRecorded: input.validationApprovalState === 'approved',
  });
  const outstandingItems: string[] = [];
  if (input.materializationApprovalState !== 'approved') {
    outstandingItems.push('Review candidate approval is still required.');
  }
  if (!input.reviewCompleted) {
    outstandingItems.push('Review Design must be completed before the iteration can be closed.');
  }
  if (!input.analyzerResultsAttached) {
    outstandingItems.push('Analyzer results must be attached before the iteration can be closed.');
  }
  if (!input.refinementReviewed) {
    outstandingItems.push('Refinement Studio recommendations must be reviewed before the iteration can be closed.');
  }
  const deferredRecommendationCount = input.refinementProposals.filter((proposal) => proposal.recommendationState === 'deferred').length;
  const unresolvedRecommendationCount = input.refinementProposals.filter((proposal) => isUnresolvedRecommendationState(proposal.recommendationState ?? 'proposed')).length;
  const approvalsSatisfied: DesignArtifactApprovalKind[] = [];
  if (input.designApprovalState === 'approved') {
    approvalsSatisfied.push('designApproval');
  }
  if (input.materializationApprovalState === 'approved') {
    approvalsSatisfied.push('materializationApproval');
  }
  if (input.refinementProposals.length > 0 && input.refinementProposals.every((proposal) => proposal.recommendationState === 'approved')) {
    approvalsSatisfied.push('refinementApproval');
  }
  if (input.validationApprovalState === 'approved') {
    approvalsSatisfied.push('validationApproval');
  }

  const isEligible = checklist.every((item) => item.satisfied || !item.required);
  const previous = input.previous;
  let state: IterationWorkflowCompletion['state'];
  if (previous?.state === 'completed') {
    state = 'completed';
  } else if (previous?.state === 'reopened') {
    state = isEligible ? 'reopened' : 'active';
  } else if (isEligible) {
    state = 'readyForCompletion';
  } else {
    state = 'active';
  }

  return createWorkflowCompletion({
    state,
    checklist,
    outstandingItems,
    approvalsSatisfied,
    deferredRecommendationCount,
    unresolvedRecommendationCount,
    completedAt: previous?.completedAt,
    completedBy: previous?.completedBy,
    reopenedAt: previous?.reopenedAt,
    reopenedBy: previous?.reopenedBy,
    history: previous?.history,
  });
}

async function buildRecordIterationInputFromCurrentState(
  context: vscode.ExtensionContext,
  threadId: string,
  existing?: IterationState,
): Promise<RecordIterationInput> {
  const [draftState, prepareForReviewState, refinementState] = await Promise.all([
    loadDraftState(context, threadId),
    loadPrepareForReviewState(context, threadId),
    loadRefinementState(context, threadId),
  ]);

  if (!draftState) {
    throw new Error(`No Draft Studio state exists for thread ${threadId}.`);
  }

  const sourceArtifactVersionIds = sortUnique(collectDraftArtifactVersionIds(draftState));
  const latestIteration = existing?.iterations.at(-1);
  const latestFingerprint = sortUnique(latestIteration?.sourceArtifactVersionIds ?? []);
  const sameFingerprint = latestFingerprint.length === sourceArtifactVersionIds.length
    && latestFingerprint.every((entry, index) => entry === sourceArtifactVersionIds[index]);

  return {
    threadId,
    previousIterationId: latestIteration?.id,
    sourceArtifactVersionIds,
    concept: draftState.concept,
    draft: draftState.currentDraft,
    pageArtifacts: draftState.pageArtifacts,
    layoutArtifacts: draftState.layoutArtifacts,
    navigationArtifacts: draftState.navigationArtifacts,
    materializedCandidate: prepareForReviewState?.currentCandidate,
    analyzerOutputs: refinementState?.proposals.map((proposal) => proposal.sourceAnalyzerOutput) ?? [],
    refinementProposals: refinementState?.proposals ?? [],
    validationApproval: sameFingerprint
      ? latestIteration?.approvalCheckpoint.validationApproval.approvalState === 'approved'
        ? {
          approvalState: latestIteration.approvalCheckpoint.validationApproval.approvalState,
          provenance: { source: latestIteration.approvalCheckpoint.validationApproval.owner ?? 'system' },
          validationLinkage: {
            analyzerRunId: latestIteration.approvalCheckpoint.validationApproval.analyzerRunId,
            resultReference: latestIteration.approvalCheckpoint.validationApproval.resultReference,
            sourceCandidateId: latestIteration.approvalCheckpoint.validationApproval.sourceCandidateId,
            sourceArtifactVersionFingerprint: latestIteration.approvalCheckpoint.validationApproval.sourceArtifactVersionFingerprint,
            validationResultStatus: latestIteration.approvalCheckpoint.validationApproval.validationResultStatus,
          },
        }
        : undefined
      : undefined,
  };
}

function replaceLatestIteration(
  state: IterationState,
  updater: (iteration: DesignIterationRecord) => DesignIterationRecord,
): IterationState {
  if (state.iterations.length === 0) {
    throw new Error('No iterations exist for this design thread.');
  }

  return {
    ...state,
    iterations: state.iterations.map((iteration, index) =>
      index === state.iterations.length - 1 ? updater(iteration) : iteration),
  };
}

export async function loadIterationState(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<IterationState | undefined> {
  return readPersistedState(manifestPath(context, threadId));
}

export async function recordIteration(
  context: vscode.ExtensionContext,
  input: RecordIterationInput,
): Promise<IterationState> {
  const existing = await loadIterationState(context, input.threadId);
  const reviewDesignState = await loadReviewDesignState(context, input.threadId);
  await validateIterationInput(context, existing, input);
  const version = (existing?.iterations.at(-1)?.version ?? 0) + 1;
  const now = new Date().toISOString();
  const comparisonSnapshot = buildComparisonSnapshot(input);
  const approvalCheckpoint = summarizeApprovals(input);
  const previousWorkflowCompletion = existing?.iterations.at(-1)?.workflowCompletion;
  const workflowCompletion = deriveWorkflowCompletion({
    designApprovalState: approvalCheckpoint.designApproval.approvalState,
    materializationApprovalState: approvalCheckpoint.materializationApproval.approvalState,
    validationApprovalState: approvalCheckpoint.validationApproval.approvalState,
    refinementProposals: summarizeRefinementProposals(input.refinementProposals),
    reviewCompleted: reviewDesignState?.currentReview?.status === 'completed',
    analyzerResultsAttached: (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) > 0,
    refinementReviewed: input.refinementProposals.length === 0
      ? (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) > 0
      : input.refinementProposals.every((proposal) => proposal.lifecycleState !== 'proposed'),
    previous: previousWorkflowCompletion,
  });
  const next: DesignIterationRecord = {
    id: `design-iteration:${input.threadId}:${version}`,
    threadId: input.threadId,
    kind: 'designIterationRecord',
    version,
    lifecycleState: 'reviewed',
    createdAt: now,
    updatedAt: now,
    authorSource: 'system',
    provenance: {
      source: 'system',
      timestamp: now,
      notes: ['Closed-loop iteration records remain explicit and audit-only.'],
    },
    previousIterationId: input.previousIterationId,
    sourceArtifactVersionIds: [...input.sourceArtifactVersionIds],
    materializedCandidate: summarizeMaterializedCandidate(input.materializedCandidate),
    analyzerResults: summarizeAnalyzerOutputs(input.analyzerOutputs, input.validationApproval),
    refinementProposals: summarizeRefinementProposals(input.refinementProposals),
    approvalCheckpoint,
    comparisonSnapshot,
    guardrails: buildGuardrails(),
    workflowCompletion,
    comparisonSummary: createSummary(comparisonSnapshot),
  };

  const state: IterationState = {
    threadId: input.threadId,
    iterations: [...(existing?.iterations ?? []), next],
  };

  writePersistedState(manifestPath(context, input.threadId), state);
  return state;
}

export async function evaluateIterationCompletion(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<IterationCompletionEvaluation> {
  const existing = await loadIterationState(context, threadId);
  const latestIteration = existing?.iterations.at(-1);
  const [briefState, conceptState, draftState, prepareForReviewState, reviewDesignState, refinementState] = await Promise.all([
    loadDesignBriefState(context, threadId),
    loadConceptState(context, threadId),
    loadDraftState(context, threadId),
    loadPrepareForReviewState(context, threadId),
    loadReviewDesignState(context, threadId),
    loadRefinementState(context, threadId),
  ]);

  return deriveWorkflowCompletion({
    designApprovalState: draftState?.currentDraft.approvalState
      ?? conceptState?.currentConcept.approvalState
      ?? briefState?.current.approvalState
      ?? 'notSubmitted',
    materializationApprovalState: prepareForReviewState?.currentCandidate.approvalState ?? 'notSubmitted',
    validationApprovalState: latestIteration?.approvalCheckpoint.validationApproval.approvalState ?? 'notSubmitted',
    refinementProposals: (refinementState?.proposals ?? []).map((proposal) => ({
      proposalId: proposal.id,
      approvalState: proposal.approvalState,
      recommendationState: getRecommendationState(proposal),
      suggestedDesignChange: proposal.suggestedDesignChange,
      expectedImpact: proposal.expectedImpact,
      linkedFindingIds: [...proposal.linkedFindingIds],
    })),
    reviewCompleted: reviewDesignState?.currentReview?.status === 'completed',
    analyzerResultsAttached: (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) > 0,
    refinementReviewed: (reviewDesignState?.currentReview?.attachedResults?.length ?? 0) === 0
      ? false
      : (refinementState?.proposals.length ?? 0) === 0
        ? true
        : (refinementState?.proposals ?? []).every((proposal) => proposal.lifecycleState !== 'proposed'),
    previous: latestIteration?.workflowCompletion,
  });
}

function createAnalyzerOutputFromResult(
  result: DesignStudioAnalyzerResultReference,
): RefinementSourceAnalyzerOutput {
  return {
    analyzerSource: result.analyzerSource,
    analyzerRunId: result.analyzerRunId,
    resultReference: result.resultReference,
    reportPath: '',
    scoredAt: result.scoredAt,
    sourceArtifactVersionIds: [...result.sourceArtifactVersionFingerprint],
    sourceCandidateId: result.sourceCandidateId,
    sourceArtifactVersionFingerprint: [...result.sourceArtifactVersionFingerprint],
    payload: {},
  };
}

async function ingestAnalyzerWorkspaceReturnPayloads(
  context: vscode.ExtensionContext,
  threadId: string,
  results: DesignStudioAnalyzerResultReference[],
): Promise<void> {
  const candidateId = results[0]?.sourceCandidateId;
  if (!candidateId) {
    return;
  }

  const persistedReturn = await loadAnalyzerWorkspaceReturn(context, candidateId);
  if (!persistedReturn) {
    return;
  }

  const contract = await validateAnalyzerWorkspaceReturnResults(context, results);
  const payloads = await loadAnalyzerWorkspaceReturnPayloads(context, contract.sourceCandidateId);
  if (!payloads) {
    throw new Error('Analyzer result attachment requires persisted Analyzer Workspace payloads.');
  }

  const existingRefinement = await loadRefinementState(context, threadId);
  const existingKeys = new Set((existingRefinement?.proposals ?? []).map((proposal) =>
    `${proposal.sourceAnalyzerOutput.analyzerRunId}::${proposal.sourceAnalyzerOutput.resultReference}`));
  const sourceVersionIds = [...contract.sourceArtifactVersionFingerprint];

  for (const result of results) {
    const key = `${result.analyzerRunId}::${result.resultReference}`;
    if (existingKeys.has(key)) {
      continue;
    }

    switch (result.analyzerSource) {
      case 'storyAssessment':
        if (payloads.storyAssessment) {
          await ingestStoryAssessmentOutput(context, threadId, {
            analyzerRunId: result.analyzerRunId,
            resultReference: result.resultReference,
            sourceArtifactVersionIds: sourceVersionIds,
            reportPath: contract.reportPath,
            scoredAt: result.scoredAt,
            storyAssessment: payloads.storyAssessment,
          });
        }
        break;
      case 'guidedStoryImprovements':
        if (payloads.guidedStoryImprovements) {
          await ingestGuidedStoryImprovements(context, threadId, {
            analyzerRunId: result.analyzerRunId,
            resultReference: result.resultReference,
            sourceArtifactVersionIds: sourceVersionIds,
            reportPath: contract.reportPath,
            scoredAt: result.scoredAt,
            guidedStoryImprovements: payloads.guidedStoryImprovements,
          });
        }
        break;
      case 'issues':
        await ingestIssues(context, threadId, {
          analyzerRunId: result.analyzerRunId,
          resultReference: result.resultReference,
          sourceArtifactVersionIds: sourceVersionIds,
          reportPath: contract.reportPath,
          scoredAt: result.scoredAt,
          issues: payloads.issues ?? [],
        });
        break;
      case 'fixPlan':
        if (payloads.fixPlan) {
          await ingestFixPlanItems(context, threadId, {
            analyzerRunId: result.analyzerRunId,
            resultReference: result.resultReference,
            sourceArtifactVersionIds: sourceVersionIds,
            reportPath: contract.reportPath,
            scoredAt: result.scoredAt,
            fixPlanItems: payloads.fixPlan,
          });
        }
        break;
      case 'crossPageNarrative':
        break;
    }
  }
}

export async function attachAnalyzerResultsAtomically(
  context: vscode.ExtensionContext,
  threadId: string,
  input: {
    requestId: string;
    candidate: MaterializedSurfaceCandidate;
  },
): Promise<AtomicAnalyzerResultAttachment> {
  const reviewState = await loadReviewDesignState(context, threadId);
  const availableResults = reviewState?.currentReview?.availableResults ?? [];

  try {
    validateAttachableAnalyzerResults(input.candidate, availableResults);
  } catch (error) {
    return {
      ok: false,
      error: error instanceof Error ? error.message : 'Analyzer result attachment failed.',
    };
  }

  const snapshots = new Map<string, string | undefined>([
    [refinementManifestPath(context, threadId), snapshotFile(refinementManifestPath(context, threadId))],
    [reviewDesignManifestPath(context, threadId), snapshotFile(reviewDesignManifestPath(context, threadId))],
    [manifestPath(context, threadId), snapshotFile(manifestPath(context, threadId))],
  ]);

  try {
    await ingestAnalyzerWorkspaceReturnPayloads(context, threadId, availableResults);
    await attachAnalyzerResultLineage(context, threadId, availableResults);
    await markAnalyzerResultsAttached(context, threadId, input);
    const iterationState = await attachAvailableAnalyzerResults(context, threadId);
    return {
      ok: true,
      iterationState,
    };
  } catch (error) {
    for (const [filePath, snapshot] of snapshots.entries()) {
      restoreFile(filePath, snapshot);
    }

    return {
      ok: false,
      error: error instanceof Error ? error.message : 'Analyzer result attachment failed.',
    };
  }
}

export async function attachAvailableAnalyzerResults(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<IterationState> {
  const [existing, draftState, prepareForReviewState, refinementState, reviewDesignState] = await Promise.all([
    loadIterationState(context, threadId),
    loadDraftState(context, threadId),
    loadPrepareForReviewState(context, threadId),
    loadRefinementState(context, threadId),
    loadReviewDesignState(context, threadId),
  ]);

  if (!draftState) {
    throw new Error(`No Draft Studio state exists for thread ${threadId}.`);
  }

  const attachedResults = reviewDesignState?.currentReview?.attachedResults ?? [];
  if (attachedResults.length === 0) {
    throw new Error('No analyzer results are available to attach.');
  }

  const candidate = prepareForReviewState?.currentCandidate;
  if (!candidate) {
    throw new Error('No approved review candidate exists for analyzer result attachment.');
  }

  validateAttachableAnalyzerResults(candidate, attachedResults);
  const validationApprovalResult = attachedResults.find((result) => result.validationApprovalState === 'approved');
  const matchingProposals = (refinementState?.proposals ?? []).filter((proposal) =>
    attachedResults.some((result) =>
      result.analyzerRunId === proposal.sourceAnalyzerOutput.analyzerRunId
      && result.resultReference === proposal.sourceAnalyzerOutput.resultReference));

  return recordIteration(context, {
    threadId,
    previousIterationId: existing?.iterations.at(-1)?.id,
    sourceArtifactVersionIds: sortUnique(collectDraftArtifactVersionIds(draftState)),
    concept: draftState.concept,
    draft: draftState.currentDraft,
    pageArtifacts: draftState.pageArtifacts,
    layoutArtifacts: draftState.layoutArtifacts,
    navigationArtifacts: draftState.navigationArtifacts,
    materializedCandidate: candidate,
    analyzerOutputs: attachedResults.map(createAnalyzerOutputFromResult),
    refinementProposals: matchingProposals,
    validationApproval: validationApprovalResult
      ? {
        approvalState: 'approved',
        provenance: { source: 'analyzerWorkspace' },
        validationLinkage: buildValidationApprovalEvidence({
          analyzerRunId: validationApprovalResult.analyzerRunId,
          resultReference: validationApprovalResult.resultReference,
          sourceCandidateId: validationApprovalResult.sourceCandidateId,
          sourceArtifactVersionFingerprint: [...validationApprovalResult.sourceArtifactVersionFingerprint],
          validationResultStatus: validationApprovalResult.validationResultStatus,
        }),
      }
      : undefined,
  });
}

export async function completeIteration(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<IterationState> {
  let state = await loadIterationState(context, threadId);
  if (!state) {
    state = await recordIteration(context, await buildRecordIterationInputFromCurrentState(context, threadId));
  }

  const latest = state.iterations.at(-1);
  if (!latest) {
    throw new Error('No iterations exist for this design thread.');
  }

  const evaluation = await evaluateIterationCompletion(context, threadId);
  if (!evaluation.isEligible) {
    throw new Error('Iteration is not ready for completion.');
  }

  if (latest.workflowCompletion.state === 'completed') {
    return state;
  }

  const now = new Date().toISOString();
  const nextState = replaceLatestIteration(state, (iteration) => ({
    ...iteration,
    updatedAt: now,
    workflowCompletion: createWorkflowCompletion({
      ...evaluation,
      state: 'completed',
      completedAt: now,
      completedBy: 'user',
      history: [
        ...evaluation.history,
        { action: 'completed', actor: 'user', timestamp: now },
      ],
    }),
  }));
  writePersistedState(manifestPath(context, threadId), nextState);
  return nextState;
}

export async function reopenIteration(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<IterationState> {
  const state = await loadIterationState(context, threadId);
  const latest = state?.iterations.at(-1);
  if (!state || !latest) {
    throw new Error('No iterations exist for this design thread.');
  }

  const now = new Date().toISOString();
  const nextState = replaceLatestIteration(state, (iteration) => ({
    ...iteration,
    updatedAt: now,
    workflowCompletion: createWorkflowCompletion({
      ...iteration.workflowCompletion,
      state: 'reopened',
      reopenedAt: now,
      reopenedBy: 'user',
      history: [
        ...iteration.workflowCompletion.history,
        { action: 'reopened', actor: 'user', timestamp: now },
      ],
    }),
  }));
  writePersistedState(manifestPath(context, threadId), nextState);
  return nextState;
}

export async function compareIterations(
  context: vscode.ExtensionContext,
  threadId: string,
  baseIterationId: string,
  candidateIterationId: string,
): Promise<ClosedLoopIterationComparison> {
  const state = await loadIterationState(context, threadId);
  const base = state?.iterations.find((iteration) => iteration.id === baseIterationId);
  const candidate = state?.iterations.find((iteration) => iteration.id === candidateIterationId);

  if (!base || !candidate) {
    throw new Error('Both iterations must exist before comparison.');
  }

  return buildIterationComparison(base, candidate);
}
