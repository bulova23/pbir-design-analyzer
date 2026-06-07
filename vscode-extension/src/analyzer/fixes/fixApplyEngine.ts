import * as fs from 'fs';
import * as path from 'path';
import type {
  FixApplyResult,
  FixApplySessionRecord,
  FixBatchApplyResult,
  FixMutation,
  FixOpportunity,
} from '../contracts/scorePanel';
import { evaluateFixOpportunityCompatibility } from './fixCompatibility';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function getStoragePath(mutation: FixMutation): Array<string | number> {
  return mutation.storagePath ?? mutation.propertyPath.split('.');
}

function decodeStoredValue(mutation: FixMutation, storedValue: unknown): unknown {
  if (mutation.storageValueFormat === 'pbirStringLiteral') {
    if (typeof storedValue !== 'string') {
      return undefined;
    }

    if (storedValue.length >= 2 && storedValue.startsWith('\'') && storedValue.endsWith('\'')) {
      return storedValue.slice(1, -1).replace(/''/g, '\'');
    }
  }

  return storedValue;
}

function encodeStoredValue(mutation: FixMutation, value: unknown): unknown {
  if (mutation.storageValueFormat === 'pbirStringLiteral') {
    if (typeof value !== 'string') {
      throw new Error(`invalid-pbir-string-literal:${mutation.id}`);
    }

    return `'${value.replace(/'/g, '\'\'')}'`;
  }

  return value;
}

function getPropertyValue(source: unknown, storagePath: Array<string | number>): unknown {
  return storagePath.reduce<unknown>((current, segment) => {
    if (typeof segment === 'number') {
      return Array.isArray(current) ? current[segment] : undefined;
    }

    return isRecord(current) ? current[segment] : undefined;
  }, source);
}

function setPropertyValue(source: Record<string, unknown>, storagePath: Array<string | number>, value: unknown): void {
  let current: unknown = source;

  for (let index = 0; index < storagePath.length - 1; index += 1) {
    const segment = storagePath[index];
    const nextSegment = storagePath[index + 1];

    if (typeof segment === 'number') {
      if (!Array.isArray(current) || current[segment] === undefined) {
        throw new Error(`missing-array-path:${storagePath.join('.')}`);
      }
      current = current[segment];
      continue;
    }

    if (!isRecord(current)) {
      throw new Error(`missing-object-path:${storagePath.join('.')}`);
    }

    const next = current[segment];
    if (next === undefined) {
      current[segment] = typeof nextSegment === 'number' ? [] : {};
      current = current[segment];
      continue;
    }

    current = next;
  }

  const finalSegment = storagePath[storagePath.length - 1];
  if (typeof finalSegment === 'number') {
    if (!Array.isArray(current)) {
      throw new Error(`missing-final-array-path:${storagePath.join('.')}`);
    }
    current[finalSegment] = value;
    return;
  }

  if (!isRecord(current)) {
    throw new Error(`missing-final-object-path:${storagePath.join('.')}`);
  }

  current[finalSegment] = value;
}

function validateMutation(mutation: FixMutation): boolean {
  const content = JSON.parse(fs.readFileSync(mutation.targetFile, 'utf8')) as unknown;
  const currentValue = decodeStoredValue(mutation, getPropertyValue(content, getStoragePath(mutation)));
  return currentValue === mutation.before;
}

function validateMutations(mutations: FixMutation[]): string[] {
  return mutations.flatMap((mutation) => (validateMutation(mutation)
    ? []
    : [`${mutation.targetObjectId}:${mutation.propertyPath}`]));
}

function sortOpportunities(opportunities: FixOpportunity[]): FixOpportunity[] {
  return [...opportunities].sort((left, right) => {
    const leftPage = left.affectedPages[0] ?? '';
    const rightPage = right.affectedPages[0] ?? '';
    if (leftPage !== rightPage) {
      return leftPage.localeCompare(rightPage);
    }

    return left.id.localeCompare(right.id);
  });
}

function collectUniqueBackups(opportunities: FixOpportunity[]): Map<string, string> {
  const backups = new Map<string, string>();

  for (const opportunity of opportunities) {
    for (const backup of opportunity.rollbackPlan.fileBackups) {
      if (!backups.has(backup.targetFile)) {
        backups.set(backup.targetFile, backup.beforeContent);
      }
    }
  }

  return backups;
}

function restoreBackups(backups: Map<string, string>): void {
  for (const [targetFile, beforeContent] of backups.entries()) {
    if (fs.existsSync(targetFile) && fs.readFileSync(targetFile, 'utf8') === beforeContent) {
      continue;
    }
    fs.writeFileSync(targetFile, beforeContent, 'utf8');
  }
}

function buildUpdatedFileJson(mutations: FixMutation[]): Map<string, Record<string, unknown>> {
  const fileJson = new Map<string, Record<string, unknown>>();

  for (const mutation of mutations) {
    if (!fileJson.has(mutation.targetFile)) {
      fileJson.set(mutation.targetFile, JSON.parse(fs.readFileSync(mutation.targetFile, 'utf8')) as Record<string, unknown>);
    }

    setPropertyValue(
      fileJson.get(mutation.targetFile)!,
      getStoragePath(mutation),
      encodeStoredValue(mutation, mutation.after),
    );
  }

  return fileJson;
}

function persistFilesAtomically(fileJson: Map<string, Record<string, unknown>>): string[] {
  const tempFiles: string[] = [];
  const persistedTargets: string[] = [];

  try {
    for (const [targetFile, json] of fileJson.entries()) {
      // 0.5.1 safe fallback: when a surgical patcher is not available for the
      // supported mutation surface, rewrite the validated JSON atomically via
      // temp-file + rename rather than attempting best-effort in-place edits.
      const tempFile = path.join(path.dirname(targetFile), `${path.basename(targetFile)}.${Date.now()}.${Math.random().toString(16).slice(2)}.tmp`);
      tempFiles.push(tempFile);
      fs.writeFileSync(tempFile, JSON.stringify(json, null, 2), 'utf8');
      fs.renameSync(tempFile, targetFile);
      persistedTargets.push(targetFile);
    }

    return persistedTargets;
  } catch (error) {
    for (const tempFile of tempFiles) {
      if (fs.existsSync(tempFile)) {
        fs.rmSync(tempFile, { force: true });
      }
    }
    throw error;
  }
}

function validateWrittenMutations(mutations: FixMutation[]): string[] {
  return mutations.flatMap((mutation) => {
    const content = JSON.parse(fs.readFileSync(mutation.targetFile, 'utf8')) as unknown;
    const currentValue = decodeStoredValue(mutation, getPropertyValue(content, getStoragePath(mutation)));
    return currentValue === mutation.after
      ? []
      : [`post-write:${mutation.targetObjectId}:${mutation.propertyPath}`];
  });
}

export function applyFixOpportunity(opportunity: FixOpportunity): FixApplyResult {
  if (!opportunity.rollbackPlan || opportunity.rollbackPlan.fileBackups.length === 0) {
    return {
      opportunityId: opportunity.id,
      state: 'FailedValidation',
      appliedMutationCount: 0,
      validationErrors: ['rollback-plan-missing'],
    };
  }

  const validationErrors = validateMutations(opportunity.mutations);
  if (validationErrors.length > 0) {
    return {
      opportunityId: opportunity.id,
      state: 'Stale',
      appliedMutationCount: 0,
      validationErrors,
    };
  }

  const backups = collectUniqueBackups([opportunity]);

  try {
    const fileJson = buildUpdatedFileJson(opportunity.mutations);
    persistFilesAtomically(fileJson);
    const postWriteErrors = validateWrittenMutations(opportunity.mutations);
    if (postWriteErrors.length > 0) {
      restoreBackups(backups);
      return {
        opportunityId: opportunity.id,
        state: 'FailedValidation',
        appliedMutationCount: 0,
        validationErrors: postWriteErrors,
      };
    }

    return {
      opportunityId: opportunity.id,
      state: 'Applied',
      appliedMutationCount: opportunity.mutations.length,
      validationErrors: [],
    };
  } catch (error) {
    restoreBackups(backups);
    return {
      opportunityId: opportunity.id,
      state: 'FailedValidation',
      appliedMutationCount: 0,
      validationErrors: [error instanceof Error ? error.message : 'apply-failed'],
    };
  }
}

export function applyFixOpportunityBatch(
  opportunities: FixOpportunity[],
  appliedAt: string = new Date().toISOString(),
): FixBatchApplyResult {
  const ordered = sortOpportunities(opportunities);
  const compatibility = evaluateFixOpportunityCompatibility(ordered);
  if (!compatibility.isCompatible) {
    return {
      state: compatibility.blockingReasons.some((reason) => reason.code === 'staleOpportunity' || reason.code === 'targetDrifted')
        ? 'Stale'
        : 'FailedValidation',
      opportunityIds: ordered.map((opportunity) => opportunity.id),
      appliedMutationCount: 0,
      validationErrors: compatibility.blockingReasons.map((reason) => `${reason.code}:${reason.opportunityIds.join(',')}`),
      applyOrder: ordered.map((opportunity) => opportunity.id),
    };
  }

  const validationErrors = ordered.flatMap((opportunity) => validateMutations(opportunity.mutations)
    .map((error) => `${opportunity.id}:${error}`));
  if (validationErrors.length > 0) {
    return {
      state: 'Stale',
      opportunityIds: ordered.map((opportunity) => opportunity.id),
      appliedMutationCount: 0,
      validationErrors,
      applyOrder: ordered.map((opportunity) => opportunity.id),
    };
  }

  const allMutations = ordered.flatMap((opportunity) => opportunity.mutations);
  const backups = collectUniqueBackups(ordered);

  try {
    const fileJson = buildUpdatedFileJson(allMutations);
    persistFilesAtomically(fileJson);
    const postWriteErrors = validateWrittenMutations(allMutations);
    if (postWriteErrors.length > 0) {
      restoreBackups(backups);
      return {
        state: 'FailedValidation',
        opportunityIds: ordered.map((opportunity) => opportunity.id),
        appliedMutationCount: 0,
        validationErrors: postWriteErrors,
        applyOrder: ordered.map((opportunity) => opportunity.id),
      };
    }

    return {
      state: 'Applied',
      opportunityIds: ordered.map((opportunity) => opportunity.id),
      appliedMutationCount: ordered.reduce((sum, opportunity) => sum + opportunity.mutations.length, 0),
      validationErrors: [],
      applyOrder: ordered.map((opportunity) => opportunity.id),
      session: {
        id: `fix-session-${appliedAt}`,
        appliedAt,
        opportunityIds: ordered.map((opportunity) => opportunity.id),
        opportunityTitles: ordered.map((opportunity) => opportunity.title),
        rollbackAvailable: ordered.every((opportunity) => opportunity.rollbackPlan.fileBackups.length > 0),
        rollbackHistory: [],
      },
    };
  } catch (error) {
    restoreBackups(backups);
    return {
      state: 'FailedValidation',
      opportunityIds: ordered.map((opportunity) => opportunity.id),
      appliedMutationCount: 0,
      validationErrors: [error instanceof Error ? error.message : 'apply-failed'],
      applyOrder: ordered.map((opportunity) => opportunity.id),
    };
  }
}

export function rollbackFixOpportunity(opportunity: FixOpportunity): FixApplyResult {
  for (const backup of opportunity.rollbackPlan.fileBackups) {
    fs.writeFileSync(backup.targetFile, backup.beforeContent, 'utf8');
  }

  return {
    opportunityId: opportunity.id,
    state: 'RolledBack',
    appliedMutationCount: opportunity.rollbackPlan.reverseMutations.length,
    validationErrors: [],
  };
}

export function rollbackFixSession(
  session: FixApplySessionRecord,
  opportunities: FixOpportunity[],
  rolledBackAt: string = new Date().toISOString(),
): FixApplySessionRecord & { state: 'RolledBack' | 'RollbackFailed' } {
  try {
    for (const opportunityId of [...session.opportunityIds].reverse()) {
      const opportunity = opportunities.find((item) => item.id === opportunityId);
      if (!opportunity) {
        throw new Error(`missing-opportunity:${opportunityId}`);
      }

      rollbackFixOpportunity(opportunity);
    }

    return {
      ...session,
      rollbackHistory: [
        ...session.rollbackHistory,
        {
          rolledBackAt,
          state: 'RolledBack',
        },
      ],
      state: 'RolledBack',
    };
  } catch {
    return {
      ...session,
      rollbackHistory: [
        ...session.rollbackHistory,
        {
          rolledBackAt,
          state: 'RollbackFailed',
        },
      ],
      state: 'RollbackFailed',
    };
  }
}
