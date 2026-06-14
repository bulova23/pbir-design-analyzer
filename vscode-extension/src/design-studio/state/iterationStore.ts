import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';
import {
  hasAnalyzerOwnedValidationApproval,
} from '../contracts/designStudioModels';
import type {
  ClosedLoopIterationComparison,
  DesignArtifactApprovalState,
  DesignArtifactProvenance,
  DesignArtifactValidationLinkage,
  DesignIterationRecord,
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
import { loadRefinementState } from './refinementStore';
import { buildIterationComparison } from '../presentation/iterationExperience';

export interface IterationState {
  threadId: string;
  iterations: DesignIterationRecord[];
}

interface PersistedIterationState extends IterationState {}

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

function threadKey(threadId: string): string {
  return crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'threads', threadKey(threadId));
}

function manifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'closed-loop.json');
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

function sortUnique(values: string[]): string[] {
  return [...new Set(values)].sort((left, right) => left.localeCompare(right));
}

function matchesVersionId<T extends { id: string; version: number }>(artifact: T, versionId: string): boolean {
  return `${artifact.id}@v${artifact.version}` === versionId;
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
    : input.refinementProposals.every((proposal) => proposal.approvalState === 'approved')
      ? 'approved'
      : input.refinementProposals.some((proposal) => proposal.approvalState === 'rejected')
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
  await validateIterationInput(context, existing, input);
  const version = (existing?.iterations.at(-1)?.version ?? 0) + 1;
  const now = new Date().toISOString();
  const comparisonSnapshot = buildComparisonSnapshot(input);
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
    approvalCheckpoint: summarizeApprovals(input),
    comparisonSnapshot,
    guardrails: buildGuardrails(),
    comparisonSummary: createSummary(comparisonSnapshot),
  };

  const state: IterationState = {
    threadId: input.threadId,
    iterations: [...(existing?.iterations ?? []), next],
  };

  writePersistedState(manifestPath(context, input.threadId), state);
  return state;
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
