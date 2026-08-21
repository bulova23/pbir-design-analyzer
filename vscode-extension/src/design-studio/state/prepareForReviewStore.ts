import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';
import type {
  DesignArtifactApprovalState,
  MaterializationRequest,
  MaterializedSurfaceCandidate,
  SourceArtifactLineageEntry,
} from '../contracts/designStudioModels';
import {
  createApprovedDraftMaterializationRequest,
  materializeDesignStudioRequest,
} from '../materialization/materializationCoordinator';
import {
  buildApprovedDraftPrimaryLineage,
  loadDraftState,
} from './draftStore';

export interface PrepareForReviewHistoryEntry {
  version: number;
  savedAt: string;
  request: MaterializationRequest;
  candidate: MaterializedSurfaceCandidate;
}

export interface PrepareForReviewState {
  threadId: string;
  currentRequest: MaterializationRequest;
  currentCandidate: MaterializedSurfaceCandidate;
  history: PrepareForReviewHistoryEntry[];
}

type PersistedPrepareForReviewState = PrepareForReviewState;

export interface CreateReviewCandidateOptions {
  threadId: string;
  reportPath: string;
  targetSurfaceType?: MaterializationRequest['targetSurfaceType'];
  targetAnalyzer?: MaterializationRequest['targetAnalyzer'];
  targetAnalyzerProfile?: MaterializationRequest['targetAnalyzerProfile'];
}

const DEFAULT_TARGET_SURFACE_TYPE: MaterializationRequest['targetSurfaceType'] = 'pbirReport';
const DEFAULT_TARGET_ANALYZER: MaterializationRequest['targetAnalyzer'] = 'pbirDesignReview';
const DEFAULT_TARGET_PROFILE: MaterializationRequest['targetAnalyzerProfile'] = 'default';

function threadKey(threadId: string): string {
  return crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'threads', threadKey(threadId));
}

function manifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'prepare-for-review.json');
}

function readPersistedState(filePath: string): PersistedPrepareForReviewState | undefined {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8')) as PersistedPrepareForReviewState;
  } catch {
    return undefined;
  }
}

function writePersistedState(filePath: string, state: PersistedPrepareForReviewState): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(state, null, 2), 'utf8');
}

function cloneRequestWithApprovalState(
  request: MaterializationRequest,
  approvalState: DesignArtifactApprovalState,
  version: number,
  updatedAt: string,
): MaterializationRequest {
  return {
    ...request,
    version,
    lifecycleState: approvalState === 'approved'
      ? 'approved'
      : approvalState === 'pendingApproval'
        ? 'proposed'
        : 'reviewed',
    approvalState,
    createdAt: request.createdAt,
    updatedAt,
    provenance: {
      ...request.provenance,
      notes: request.provenance.notes ? [...request.provenance.notes] : undefined,
    },
    sourceArtifactIds: [...request.sourceArtifactIds],
    sourceLineage: request.sourceLineage.map((entry) => ({ ...entry })),
    handoffContext: {
      repositoryBackedPath: request.handoffContext.repositoryBackedPath,
      snapshotReference: request.handoffContext.snapshotReference
        ? { ...request.handoffContext.snapshotReference }
        : undefined,
      degradedMappings: [...request.handoffContext.degradedMappings],
      omittedEvidence: [...request.handoffContext.omittedEvidence],
    },
  };
}

function cloneCandidateWithApprovalState(
  candidate: MaterializedSurfaceCandidate,
  approvalState: DesignArtifactApprovalState,
  version: number,
  updatedAt: string,
): MaterializedSurfaceCandidate {
  return {
    ...candidate,
    version,
    lifecycleState: approvalState === 'approved'
      ? 'materialized'
      : approvalState === 'pendingApproval'
        ? 'proposed'
        : 'reviewed',
    approvalState,
    createdAt: candidate.createdAt,
    updatedAt,
    provenance: {
      ...candidate.provenance,
      notes: candidate.provenance.notes ? [...candidate.provenance.notes] : undefined,
    },
    sourceArtifactIds: [...candidate.sourceArtifactIds],
    sourceLineage: candidate.sourceLineage.map((entry) => ({ ...entry })),
    materializationDiagnostics: [...candidate.materializationDiagnostics],
    provenanceTrace: candidate.provenanceTrace.map((entry) => ({ ...entry })),
    handoffContext: {
      repositoryBackedPath: candidate.handoffContext.repositoryBackedPath,
      snapshotReference: candidate.handoffContext.snapshotReference
        ? { ...candidate.handoffContext.snapshotReference }
        : undefined,
      degradedMappings: [...candidate.handoffContext.degradedMappings],
      omittedEvidence: [...candidate.handoffContext.omittedEvidence],
    },
    analyzerHandoff: {
      metadata: { ...candidate.analyzerHandoff.metadata },
      reference: { ...candidate.analyzerHandoff.reference },
      diagnostics: [...candidate.analyzerHandoff.diagnostics],
    },
  };
}

function sameLineage(
  left: SourceArtifactLineageEntry[],
  right: SourceArtifactLineageEntry[],
): boolean {
  if (left.length !== right.length) {
    return false;
  }

  return left.every((entry, index) =>
    entry.artifactId === right[index]?.artifactId
    && entry.artifactVersionId === right[index]?.artifactVersionId
    && entry.artifactKind === right[index]?.artifactKind
    && entry.sourceRole === right[index]?.sourceRole
    && entry.approvalState === right[index]?.approvalState);
}

async function loadApprovedDraftLineage(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<SourceArtifactLineageEntry[]> {
  const draftState = await loadDraftState(context, threadId);
  if (!draftState) {
    throw new Error(`No Draft Studio state exists for thread ${threadId}.`);
  }

  return buildApprovedDraftPrimaryLineage(draftState);
}

async function assertCurrentCandidateMatchesApprovedDraft(
  context: vscode.ExtensionContext,
  threadId: string,
  request: MaterializationRequest,
): Promise<void> {
  const currentLineage = await loadApprovedDraftLineage(context, threadId);
  if (!sameLineage(request.sourceLineage, currentLineage)) {
    throw new Error('The approved draft changed. Create a new review candidate before continuing.');
  }
}

function toState(persisted: PersistedPrepareForReviewState): PrepareForReviewState {
  return {
    ...persisted,
    history: [...persisted.history],
    currentRequest: {
      ...persisted.currentRequest,
      sourceArtifactIds: [...persisted.currentRequest.sourceArtifactIds],
      sourceLineage: persisted.currentRequest.sourceLineage.map((entry) => ({ ...entry })),
      handoffContext: {
        repositoryBackedPath: persisted.currentRequest.handoffContext.repositoryBackedPath,
        snapshotReference: persisted.currentRequest.handoffContext.snapshotReference
          ? { ...persisted.currentRequest.handoffContext.snapshotReference }
          : undefined,
        degradedMappings: [...persisted.currentRequest.handoffContext.degradedMappings],
        omittedEvidence: [...persisted.currentRequest.handoffContext.omittedEvidence],
      },
    },
    currentCandidate: cloneCandidateWithApprovalState(
      persisted.currentCandidate,
      persisted.currentCandidate.approvalState,
      persisted.currentCandidate.version,
      persisted.currentCandidate.updatedAt,
    ),
  };
}

export async function loadPrepareForReviewState(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<PrepareForReviewState | undefined> {
  const persisted = readPersistedState(manifestPath(context, threadId));
  return persisted ? toState(persisted) : undefined;
}

export async function createReviewCandidate(
  context: vscode.ExtensionContext,
  options: CreateReviewCandidateOptions,
): Promise<PrepareForReviewState> {
  const filePath = manifestPath(context, options.threadId);
  const existing = readPersistedState(filePath);
  const approvedRequest = await createApprovedDraftMaterializationRequest(context, {
    threadId: options.threadId,
    requestId: `materialization-request:${options.threadId}`,
    targetSurfaceType: options.targetSurfaceType ?? DEFAULT_TARGET_SURFACE_TYPE,
    targetAnalyzer: options.targetAnalyzer ?? DEFAULT_TARGET_ANALYZER,
    targetAnalyzerProfile: options.targetAnalyzerProfile ?? DEFAULT_TARGET_PROFILE,
    handoffContext: {
      repositoryBackedPath: options.reportPath,
      degradedMappings: [],
      omittedEvidence: [],
    },
  });
  const materialization = materializeDesignStudioRequest(approvedRequest);
  if (!materialization.ok) {
    throw new Error(materialization.diagnostics.join('\n'));
  }

  if (
    existing
    && sameLineage(existing.currentRequest.sourceLineage, approvedRequest.sourceLineage)
    && existing.currentCandidate.approvalState !== 'approved'
  ) {
    throw new Error('A review candidate already exists for the current approved draft.');
  }

  const version = (existing?.currentRequest.version ?? 0) + 1;
  const updatedAt = new Date().toISOString();
  const request = cloneRequestWithApprovalState(approvedRequest, 'notSubmitted', version, updatedAt);
  const candidate = cloneCandidateWithApprovalState(materialization.candidate, 'notSubmitted', version, updatedAt);
  const persisted: PersistedPrepareForReviewState = {
    threadId: options.threadId,
    currentRequest: request,
    currentCandidate: candidate,
    history: [
      ...(existing?.history ?? []),
      {
        version,
        savedAt: updatedAt,
        request,
        candidate,
      },
    ],
  };

  writePersistedState(filePath, persisted);
  return toState(persisted);
}

export async function submitReviewCandidateForApproval(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<PrepareForReviewState> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  if (!existing) {
    throw new Error(`No Prepare For Review state exists for thread ${threadId}.`);
  }

  if (existing.currentCandidate.approvalState === 'approved') {
    throw new Error('The current review candidate is already approved.');
  }

  if (existing.currentCandidate.approvalState === 'pendingApproval') {
    throw new Error('The current review candidate is already pending approval.');
  }

  await assertCurrentCandidateMatchesApprovedDraft(context, threadId, existing.currentRequest);

  const version = existing.currentRequest.version + 1;
  const updatedAt = new Date().toISOString();
  const request = cloneRequestWithApprovalState(existing.currentRequest, 'pendingApproval', version, updatedAt);
  const candidate = cloneCandidateWithApprovalState(existing.currentCandidate, 'pendingApproval', version, updatedAt);
  const persisted: PersistedPrepareForReviewState = {
    ...existing,
    currentRequest: request,
    currentCandidate: candidate,
    history: [
      ...existing.history,
      {
        version,
        savedAt: updatedAt,
        request,
        candidate,
      },
    ],
  };

  writePersistedState(filePath, persisted);
  return toState(persisted);
}

export async function approveReviewCandidate(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<PrepareForReviewState> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  if (!existing) {
    throw new Error(`No Prepare For Review state exists for thread ${threadId}.`);
  }

  if (existing.currentCandidate.approvalState === 'approved') {
    throw new Error('The current review candidate is already approved.');
  }

  if (existing.currentCandidate.approvalState !== 'pendingApproval') {
    throw new Error('The review candidate must be submitted for approval before approval can be recorded.');
  }

  await assertCurrentCandidateMatchesApprovedDraft(context, threadId, existing.currentRequest);

  const version = existing.currentRequest.version + 1;
  const updatedAt = new Date().toISOString();
  const approvedRequest = cloneRequestWithApprovalState(existing.currentRequest, 'approved', version, updatedAt);
  const materialization = materializeDesignStudioRequest(approvedRequest);
  if (!materialization.ok) {
    throw new Error(materialization.diagnostics.join('\n'));
  }

  const candidate = cloneCandidateWithApprovalState(materialization.candidate, 'approved', version, updatedAt);
  const persisted: PersistedPrepareForReviewState = {
    ...existing,
    currentRequest: approvedRequest,
    currentCandidate: candidate,
    history: [
      ...existing.history,
      {
        version,
        savedAt: updatedAt,
        request: approvedRequest,
        candidate,
      },
    ],
  };

  writePersistedState(filePath, persisted);
  return toState(persisted);
}
