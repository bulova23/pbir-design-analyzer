import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';
import type {
  DesignBrief,
  DesignBriefDraftInput,
  DesignBriefValidationResult,
} from '../contracts/designStudioModels';
import { validateDesignBrief } from '../contracts/designStudioModels';

export interface DesignBriefHistoryEntry {
  version: number;
  savedAt: string;
  brief: DesignBrief;
}

export interface DesignBriefState {
  threadId: string;
  current: DesignBrief;
  history: DesignBriefHistoryEntry[];
  validation: DesignBriefValidationResult;
}

interface PersistedDesignBriefState {
  threadId: string;
  current: DesignBrief;
  history: DesignBriefHistoryEntry[];
}

function threadKey(threadId: string): string {
  return crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'threads', threadKey(threadId));
}

function manifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'design-brief.json');
}

function normalizeList(values: string[]): string[] {
  return values.map((value) => value.trim()).filter((value) => value.length > 0);
}

function normalizeOptionalString(value?: string): string | undefined {
  const normalized = value?.trim();
  return normalized ? normalized : undefined;
}

function normalizeOptionalList(values?: string[]): string[] | undefined {
  if (!values) {
    return undefined;
  }

  const normalized = normalizeList(values);
  return normalized.length > 0 ? normalized : undefined;
}

function buildBrief(
  threadId: string,
  input: DesignBriefDraftInput,
  existing?: DesignBrief,
): DesignBrief {
  const now = new Date().toISOString();
  return {
    id: existing?.id ?? `design-brief:${threadId}`,
    threadId,
    kind: 'designBrief',
    version: (existing?.version ?? 0) + 1,
    lifecycleState: 'draft',
    approvalState: 'notSubmitted',
    approvalKind: 'designApproval',
    createdAt: existing?.createdAt ?? now,
    updatedAt: now,
    authorSource: 'user',
    provenance: { source: 'user' },
    audience: input.audience.trim(),
    businessObjective: input.businessObjective.trim(),
    keyDecisions: normalizeList(input.keyDecisions),
    primaryKpis: normalizeList(input.primaryKpis),
    dimensions: normalizeList(input.dimensions),
    intendedStory: input.intendedStory.trim(),
    successCriteria: normalizeList(input.successCriteria),
    reportType: input.reportType,
    navigationExpectations: input.navigationExpectations.trim(),
    consumptionContext: normalizeOptionalString(input.consumptionContext),
    decisionCadence: normalizeOptionalString(input.decisionCadence),
    narrativeRisksOrConstraints: normalizeOptionalList(input.narrativeRisksOrConstraints),
    requiredEvidenceDomains: normalizeOptionalList(input.requiredEvidenceDomains),
    targetAnalyzableSurfaceFamily: normalizeOptionalString(input.targetAnalyzableSurfaceFamily),
  };
}

function toState(persisted: PersistedDesignBriefState): DesignBriefState {
  return {
    ...persisted,
    validation: validateDesignBrief(persisted.current),
  };
}

function readPersistedState(filePath: string): PersistedDesignBriefState | undefined {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8')) as PersistedDesignBriefState;
  } catch {
    return undefined;
  }
}

function writePersistedState(filePath: string, state: PersistedDesignBriefState): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(state, null, 2), 'utf8');
}

export async function loadDesignBriefState(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<DesignBriefState | undefined> {
  const filePath = manifestPath(context, threadId);
  if (!fs.existsSync(filePath)) {
    return undefined;
  }

  const persisted = readPersistedState(filePath);
  return persisted ? toState(persisted) : undefined;
}

export async function saveDesignBriefDraft(
  context: vscode.ExtensionContext,
  threadId: string,
  input: DesignBriefDraftInput,
): Promise<DesignBriefState> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const brief = buildBrief(threadId, input, existing?.current);
  const persisted: PersistedDesignBriefState = {
    threadId,
    current: brief,
    history: [
      ...(existing?.history ?? []),
      {
        version: brief.version,
        savedAt: brief.updatedAt,
        brief,
      },
    ],
  };

  writePersistedState(filePath, persisted);
  return toState(persisted);
}

export async function approveDesignBrief(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<DesignBriefState> {
  const filePath = manifestPath(context, threadId);
  const persisted = readPersistedState(filePath);
  if (!persisted) {
    throw new Error(`No Design Brief exists for thread ${threadId}.`);
  }

  if (persisted.current.approvalState !== 'pendingApproval') {
    throw new Error('Design Brief must be submitted for approval before approval can be recorded.');
  }

  const candidate = {
    ...persisted.current,
    version: persisted.current.version + 1,
    lifecycleState: 'approved' as const,
    approvalState: 'approved' as const,
    approvalKind: 'designApproval' as const,
    updatedAt: new Date().toISOString(),
  };
  const validation = validateDesignBrief(candidate);
  if (!validation.isValid) {
    throw new Error('Design Brief must be valid before approval.');
  }

  const updated: PersistedDesignBriefState = {
    threadId,
    current: candidate,
    history: [
      ...persisted.history,
      {
        version: candidate.version,
        savedAt: candidate.updatedAt,
        brief: candidate,
      },
    ],
  };

  writePersistedState(filePath, updated);
  return {
    ...updated,
    validation,
  };
}

export async function submitDesignBriefForApproval(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<DesignBriefState> {
  const filePath = manifestPath(context, threadId);
  const persisted = readPersistedState(filePath);
  if (!persisted) {
    throw new Error(`No Design Brief exists for thread ${threadId}.`);
  }

  if (persisted.current.approvalState === 'approved') {
    throw new Error('Approved Design Briefs cannot be resubmitted without creating a new draft revision.');
  }

  const candidate = {
    ...persisted.current,
    version: persisted.current.version + 1,
    lifecycleState: 'draft' as const,
    approvalState: 'pendingApproval' as const,
    approvalKind: 'designApproval' as const,
    updatedAt: new Date().toISOString(),
  };
  const validation = validateDesignBrief(candidate);
  if (!validation.isValid) {
    throw new Error('Design Brief must be valid before submission for approval.');
  }

  const updated: PersistedDesignBriefState = {
    threadId,
    current: candidate,
    history: [
      ...persisted.history,
      {
        version: candidate.version,
        savedAt: candidate.updatedAt,
        brief: candidate,
      },
    ],
  };

  writePersistedState(filePath, updated);
  return {
    ...updated,
    validation,
  };
}
