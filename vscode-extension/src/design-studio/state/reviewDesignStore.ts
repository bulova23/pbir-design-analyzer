import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';
import type {
  AnalyzerProfileId,
  AnalyzerType,
} from '../../analyzer/analyzers/types';
import type {
  DesignStudioAnalyzerResultReference,
  MaterializedSurfaceCandidate,
} from '../contracts/designStudioModels';
import { loadAnalyzerWorkspaceReturn } from './analyzerWorkspaceReturnStore';

export type ReviewDesignExecutionStatus = 'launched' | 'completed';

export interface ReviewDesignExecutionRecord {
  requestId: string;
  candidateId: string;
  candidateVersionId: string;
  analyzerId: AnalyzerType;
  analyzerProfileId: AnalyzerProfileId;
  status: ReviewDesignExecutionStatus;
  launchedAt: string;
  completedAt?: string;
  sourceArtifactVersionIds: string[];
  availableResults: DesignStudioAnalyzerResultReference[];
  attachedResults: DesignStudioAnalyzerResultReference[];
}

export interface ReviewDesignState {
  threadId: string;
  currentReview?: ReviewDesignExecutionRecord;
  history: ReviewDesignExecutionRecord[];
}

interface PersistedReviewDesignState extends ReviewDesignState {}

function threadKey(threadId: string): string {
  return crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'threads', threadKey(threadId));
}

function manifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'review-design.json');
}

function readPersistedState(filePath: string): PersistedReviewDesignState | undefined {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8')) as PersistedReviewDesignState;
  } catch {
    return undefined;
  }
}

function writePersistedState(filePath: string, state: PersistedReviewDesignState): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(state, null, 2), 'utf8');
}

function createCandidateVersionId(candidate: MaterializedSurfaceCandidate): string {
  return `${candidate.id}@v${candidate.version}`;
}

function createBaseRecord(input: {
  requestId: string;
  candidate: MaterializedSurfaceCandidate;
  analyzerId: AnalyzerType;
  analyzerProfileId: AnalyzerProfileId;
}): ReviewDesignExecutionRecord {
  const now = new Date().toISOString();
  return {
    requestId: input.requestId,
    candidateId: input.candidate.id,
    candidateVersionId: createCandidateVersionId(input.candidate),
    analyzerId: input.analyzerId,
    analyzerProfileId: input.analyzerProfileId,
    status: 'launched',
    launchedAt: now,
    sourceArtifactVersionIds: input.candidate.sourceLineage.map((entry) => entry.artifactVersionId),
    availableResults: [],
    attachedResults: [],
  };
}

function sortUnique(values: string[]): string[] {
  return [...new Set(values)].sort((left, right) => left.localeCompare(right));
}

function cloneResult(result: DesignStudioAnalyzerResultReference): DesignStudioAnalyzerResultReference {
  return {
    ...result,
    sourceArtifactVersionFingerprint: [...result.sourceArtifactVersionFingerprint],
    findingReferenceIds: [...result.findingReferenceIds],
    recommendationReferenceIds: [...result.recommendationReferenceIds],
    linkedProposalIds: [...result.linkedProposalIds],
    provenance: {
      ...result.provenance,
      notes: result.provenance.notes ? [...result.provenance.notes] : undefined,
    },
  };
}

function validateResultLineage(
  candidate: MaterializedSurfaceCandidate,
  results: DesignStudioAnalyzerResultReference[],
): void {
  const expectedFingerprint = sortUnique(candidate.sourceLineage.map((entry) => entry.artifactVersionId));
  for (const result of results) {
    const fingerprint = sortUnique(result.sourceArtifactVersionFingerprint);
    if (result.sourceCandidateId !== candidate.id) {
      throw new Error('Analyzer results must reference the active review candidate.');
    }

    if (
      fingerprint.length === 0
      || expectedFingerprint.some((entry) => !fingerprint.includes(entry))
    ) {
      throw new Error('Analyzer results must preserve the active review candidate fingerprint.');
    }
  }
}

export async function loadReviewDesignState(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<ReviewDesignState | undefined> {
  return readPersistedState(manifestPath(context, threadId));
}

export async function recordReviewLaunch(
  context: vscode.ExtensionContext,
  threadId: string,
  input: {
    requestId: string;
    candidate: MaterializedSurfaceCandidate;
    analyzerId: AnalyzerType;
    analyzerProfileId: AnalyzerProfileId;
  },
): Promise<ReviewDesignState> {
  if (input.candidate.approvalState !== 'approved') {
    throw new Error('Review Design can only launch from an approved review candidate.');
  }

  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const currentReview = createBaseRecord(input);
  const history = existing?.currentReview
    ? [...existing.history, existing.currentReview]
    : existing?.history ?? [];
  const nextState: ReviewDesignState = {
    threadId,
    currentReview,
    history,
  };
  writePersistedState(filePath, nextState);
  return nextState;
}

export async function markReviewCompleted(
  context: vscode.ExtensionContext,
  threadId: string,
  input: {
    requestId: string;
    candidate: MaterializedSurfaceCandidate;
  },
): Promise<ReviewDesignState> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const currentReview = existing?.currentReview;

  if (!currentReview) {
    throw new Error('Review Design cannot be completed before Analyzer Workspace has been launched.');
  }

  if (currentReview.requestId !== input.requestId || currentReview.candidateId !== input.candidate.id) {
    throw new Error('Review completion must match the active launched review candidate.');
  }

  if (currentReview.status === 'completed') {
    return existing;
  }

  const completedReview: ReviewDesignExecutionRecord = {
    ...currentReview,
    status: 'completed',
    completedAt: new Date().toISOString(),
  };
  const nextState: ReviewDesignState = {
    threadId,
    currentReview: completedReview,
    history: existing?.history ?? [],
  };
  writePersistedState(filePath, nextState);
  return nextState;
}

export async function recordAnalyzerResultsAvailable(
  context: vscode.ExtensionContext,
  threadId: string,
  input: {
    requestId: string;
    candidate: MaterializedSurfaceCandidate;
    results: DesignStudioAnalyzerResultReference[];
  },
): Promise<ReviewDesignState> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const currentReview = existing?.currentReview;

  if (!currentReview || currentReview.status !== 'completed') {
    throw new Error('Analyzer results can only be recorded after Review Design has been completed.');
  }

  if (currentReview.requestId !== input.requestId || currentReview.candidateId !== input.candidate.id) {
    throw new Error('Analyzer results must match the active completed review candidate.');
  }

  validateResultLineage(input.candidate, input.results);
  const nextReview: ReviewDesignExecutionRecord = {
    ...currentReview,
    availableResults: input.results.map(cloneResult),
    attachedResults: [],
  };
  const nextState: ReviewDesignState = {
    threadId,
    currentReview: nextReview,
    history: existing?.history ?? [],
  };
  writePersistedState(filePath, nextState);
  return nextState;
}

export async function markAnalyzerResultsAttached(
  context: vscode.ExtensionContext,
  threadId: string,
  input: {
    requestId: string;
    candidate: MaterializedSurfaceCandidate;
  },
): Promise<ReviewDesignState> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const currentReview = existing?.currentReview;

  if (!currentReview || currentReview.status !== 'completed') {
    throw new Error('Analyzer results cannot be attached before Review Design has been completed.');
  }

  if (currentReview.requestId !== input.requestId || currentReview.candidateId !== input.candidate.id) {
    throw new Error('Analyzer result attachment must match the active completed review candidate.');
  }

  if (currentReview.availableResults.length === 0) {
    throw new Error('No analyzer results are available to attach.');
  }

  const nextReview: ReviewDesignExecutionRecord = {
    ...currentReview,
    attachedResults: currentReview.availableResults.map(cloneResult),
  };
  const nextState: ReviewDesignState = {
    threadId,
    currentReview: nextReview,
    history: existing?.history ?? [],
  };
  writePersistedState(filePath, nextState);
  return nextState;
}

export async function syncDiscoveredAnalyzerResults(
  context: vscode.ExtensionContext,
  threadId: string,
  input: {
    requestId: string;
    candidate: MaterializedSurfaceCandidate;
  },
): Promise<ReviewDesignState | undefined> {
  const existing = await loadReviewDesignState(context, threadId);
  const currentReview = existing?.currentReview;

  if (!currentReview || currentReview.status !== 'completed' || currentReview.availableResults.length > 0) {
    return existing;
  }

  const discovered = await loadAnalyzerWorkspaceReturn(context, input.candidate.id);
  if (!discovered) {
    return existing;
  }

  if (discovered.threadId !== threadId || discovered.requestId !== input.requestId) {
    throw new Error('Discovered analyzer return does not match the active Design Studio review request.');
  }

  validateResultLineage(input.candidate, discovered.results);
  return recordAnalyzerResultsAvailable(context, threadId, {
    requestId: input.requestId,
    candidate: input.candidate,
    results: discovered.results,
  });
}
