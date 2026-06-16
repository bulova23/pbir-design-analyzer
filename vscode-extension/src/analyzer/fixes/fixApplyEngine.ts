import type {
  FixApplyResult,
  FixApplySessionRecord,
  FixBatchApplyResult,
  FixFileVersionSnapshot,
  FixMutation,
  FixOpportunity,
  RollbackFileBackup,
} from '../contracts/scorePanel';
import { evaluateFixOpportunityCompatibility } from './fixCompatibility';
import {
  FixPersistenceValidationError,
  type FixPersistenceService,
  NodeFixPersistenceService,
} from './fixPersistenceService';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function sameFileVersion(left: FixFileVersionSnapshot | undefined, right: FixFileVersionSnapshot | undefined): boolean {
  return Boolean(left)
    && Boolean(right)
    && left!.contentHash === right!.contentHash
    && left!.size === right!.size
    && left!.modifiedTimeMs === right!.modifiedTimeMs;
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

function collectUniqueBackups(opportunities: FixOpportunity[]): Map<string, RollbackFileBackup> {
  const backups = new Map<string, RollbackFileBackup>();

  for (const opportunity of opportunities) {
    for (const backup of opportunity.rollbackPlan.fileBackups) {
      if (!backups.has(backup.targetFile)) {
        backups.set(backup.targetFile, backup);
      }
    }
  }

  return backups;
}

function collectExpectedFileVersions(
  mutations: FixMutation[],
  backups: Map<string, RollbackFileBackup>,
): Map<string, FixFileVersionSnapshot> {
  const versions = new Map<string, FixFileVersionSnapshot>();

  for (const mutation of mutations) {
    const expected = mutation.targetFileVersion ?? backups.get(mutation.targetFile)?.beforeVersion;
    if (!expected) {
      continue;
    }

    const existing = versions.get(mutation.targetFile);
    if (!existing || sameFileVersion(existing, expected)) {
      versions.set(mutation.targetFile, expected);
    }
  }

  return versions;
}

function assignAppliedVersions(opportunities: FixOpportunity[], writtenVersions: Map<string, FixFileVersionSnapshot>): void {
  for (const opportunity of opportunities) {
    opportunity.rollbackPlan.fileBackups = opportunity.rollbackPlan.fileBackups.map((backup) => ({
      ...backup,
      appliedVersion: writtenVersions.get(backup.targetFile) ?? backup.appliedVersion,
    }));
  }
}

async function validateExpectedFileVersions(
  persistence: FixPersistenceService,
  expectedVersions: Map<string, FixFileVersionSnapshot>,
): Promise<string[]> {
  const errors: string[] = [];

  for (const [targetFile, expectedVersion] of expectedVersions.entries()) {
    const currentVersion = await persistence.captureFileVersion(targetFile);
    if (!sameFileVersion(currentVersion, expectedVersion)) {
      errors.push(`target-file-drift:${targetFile}`);
    }
  }

  return errors;
}

async function validateMutationState(
  persistence: FixPersistenceService,
  mutation: FixMutation,
  expectedValue: unknown,
  prefix?: string,
): Promise<string[]> {
  const content = await persistence.readJsonFile(mutation.targetFile);
  const currentValue = decodeStoredValue(mutation, getPropertyValue(content, getStoragePath(mutation)));
  return currentValue === expectedValue
    ? []
    : [`${prefix ? `${prefix}:` : ''}${mutation.targetObjectId}:${mutation.propertyPath}`];
}

async function validateMutations(
  persistence: FixPersistenceService,
  mutations: FixMutation[],
  expectedValueSelector: (mutation: FixMutation) => unknown,
  prefix?: string,
): Promise<string[]> {
  const results = await Promise.all(mutations.map(async (mutation) => validateMutationState(
    persistence,
    mutation,
    expectedValueSelector(mutation),
    prefix,
  )));
  return results.flat();
}

async function buildUpdatedFileJson(
  persistence: FixPersistenceService,
  mutations: FixMutation[],
): Promise<Map<string, Record<string, unknown>>> {
  const fileJson = new Map<string, Record<string, unknown>>();

  for (const mutation of mutations) {
    if (!fileJson.has(mutation.targetFile)) {
      fileJson.set(mutation.targetFile, await persistence.readJsonFile(mutation.targetFile));
    }

    setPropertyValue(
      fileJson.get(mutation.targetFile)!,
      getStoragePath(mutation),
      encodeStoredValue(mutation, mutation.after),
    );
  }

  return fileJson;
}

function buildFailedApplyResult(
  opportunityId: string,
  state: 'Stale' | 'FailedValidation',
  validationErrors: string[],
): FixApplyResult {
  return {
    opportunityId,
    state,
    appliedMutationCount: 0,
    validationErrors,
  };
}

function buildFailedBatchResult(
  opportunities: FixOpportunity[],
  state: 'Stale' | 'FailedValidation',
  validationErrors: string[],
): FixBatchApplyResult {
  return {
    state,
    opportunityIds: opportunities.map((opportunity) => opportunity.id),
    appliedMutationCount: 0,
    validationErrors,
    applyOrder: opportunities.map((opportunity) => opportunity.id),
  };
}

async function attemptBackupRestore(
  persistence: FixPersistenceService,
  backups: RollbackFileBackup[],
): Promise<string[]> {
  try {
    const restoreResult = await persistence.restoreBackups(backups);
    return restoreResult.conflictErrors;
  } catch (error) {
    return [error instanceof Error ? error.message : 'restore-failed'];
  }
}

export async function applyFixOpportunity(
  opportunity: FixOpportunity,
  persistence: FixPersistenceService = new NodeFixPersistenceService(),
): Promise<FixApplyResult> {
  if (!opportunity.rollbackPlan || opportunity.rollbackPlan.fileBackups.length === 0) {
    return buildFailedApplyResult(opportunity.id, 'FailedValidation', ['rollback-plan-missing']);
  }

  const backups = collectUniqueBackups([opportunity]);
  const expectedFileVersions = collectExpectedFileVersions(opportunity.mutations, backups);
  const versionErrors = await validateExpectedFileVersions(persistence, expectedFileVersions);
  if (versionErrors.length > 0) {
    return buildFailedApplyResult(opportunity.id, 'Stale', versionErrors);
  }

  const validationErrors = await validateMutations(persistence, opportunity.mutations, (mutation) => mutation.before);
  if (validationErrors.length > 0) {
    return buildFailedApplyResult(opportunity.id, 'Stale', validationErrors);
  }

  try {
    const fileJson = await buildUpdatedFileJson(persistence, opportunity.mutations);
    const writtenVersions = await persistence.writeJsonFilesAtomically(fileJson, {
      validate: [async () => validateMutations(persistence, opportunity.mutations, (mutation) => mutation.after, 'post-write')],
    });
    assignAppliedVersions([opportunity], writtenVersions);

    return {
      opportunityId: opportunity.id,
      state: 'Applied',
      appliedMutationCount: opportunity.mutations.length,
      validationErrors: [],
    };
  } catch (error) {
    const restoreErrors = await attemptBackupRestore(persistence, [...backups.values()]);
    const validationErrorsFromError = error instanceof FixPersistenceValidationError
      ? error.validationErrors
      : [error instanceof Error ? error.message : 'apply-failed'];
    return buildFailedApplyResult(opportunity.id, 'FailedValidation', [...validationErrorsFromError, ...restoreErrors]);
  }
}

export async function applyFixOpportunityBatch(
  opportunities: FixOpportunity[],
  appliedAt: string = new Date().toISOString(),
  persistence: FixPersistenceService = new NodeFixPersistenceService(),
): Promise<FixBatchApplyResult> {
  const ordered = sortOpportunities(opportunities);
  const compatibility = evaluateFixOpportunityCompatibility(ordered);
  if (!compatibility.isCompatible) {
    return buildFailedBatchResult(
      ordered,
      compatibility.blockingReasons.some((reason) => reason.code === 'staleOpportunity' || reason.code === 'targetDrifted')
        ? 'Stale'
        : 'FailedValidation',
      compatibility.blockingReasons.map((reason) => `${reason.code}:${reason.opportunityIds.join(',')}`),
    );
  }

  const allMutations = ordered.flatMap((opportunity) => opportunity.mutations);
  const backups = collectUniqueBackups(ordered);
  const expectedFileVersions = collectExpectedFileVersions(allMutations, backups);
  const versionErrors = await validateExpectedFileVersions(persistence, expectedFileVersions);
  if (versionErrors.length > 0) {
    return buildFailedBatchResult(ordered, 'Stale', versionErrors);
  }

  const validationErrors = (await Promise.all(ordered.map(async (opportunity) => (await validateMutations(
    persistence,
    opportunity.mutations,
    (mutation) => mutation.before,
  )).map((error) => `${opportunity.id}:${error}`)))).flat();
  if (validationErrors.length > 0) {
    return buildFailedBatchResult(ordered, 'Stale', validationErrors);
  }

  try {
    const fileJson = await buildUpdatedFileJson(persistence, allMutations);
    const writtenVersions = await persistence.writeJsonFilesAtomically(fileJson, {
      validate: [async () => validateMutations(persistence, allMutations, (mutation) => mutation.after, 'post-write')],
    });
    assignAppliedVersions(ordered, writtenVersions);

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
    const restoreErrors = await attemptBackupRestore(persistence, [...backups.values()]);
    const validationErrorsFromError = error instanceof FixPersistenceValidationError
      ? error.validationErrors
      : [error instanceof Error ? error.message : 'apply-failed'];
    return buildFailedBatchResult(ordered, 'FailedValidation', [...validationErrorsFromError, ...restoreErrors]);
  }
}

export async function rollbackFixOpportunity(
  opportunity: FixOpportunity,
  persistence: FixPersistenceService = new NodeFixPersistenceService(),
): Promise<FixApplyResult> {
  const expectedAppliedVersions = new Map<string, FixFileVersionSnapshot>();
  for (const backup of opportunity.rollbackPlan.fileBackups) {
    if (backup.appliedVersion) {
      expectedAppliedVersions.set(backup.targetFile, backup.appliedVersion);
    }
  }

  if (expectedAppliedVersions.size === 0) {
    const mutationErrors = await validateMutations(persistence, opportunity.mutations, (mutation) => mutation.after, 'rollback-conflict');
    if (mutationErrors.length > 0) {
      return buildFailedApplyResult(opportunity.id, 'FailedValidation', mutationErrors);
    }
  }

  const restoreResult = await persistence.restoreBackups(
    opportunity.rollbackPlan.fileBackups,
    expectedAppliedVersions.size > 0 ? expectedAppliedVersions : undefined,
  );
  if (restoreResult.conflictErrors.length > 0) {
    return buildFailedApplyResult(opportunity.id, 'FailedValidation', restoreResult.conflictErrors);
  }

  return {
    opportunityId: opportunity.id,
    state: 'RolledBack',
    appliedMutationCount: opportunity.rollbackPlan.reverseMutations.length,
    validationErrors: [],
  };
}

export async function rollbackFixSession(
  session: FixApplySessionRecord,
  opportunities: FixOpportunity[],
  rolledBackAt: string = new Date().toISOString(),
  persistence: FixPersistenceService = new NodeFixPersistenceService(),
): Promise<FixApplySessionRecord & { state: 'RolledBack' | 'RollbackFailed' }> {
  try {
    for (const opportunityId of [...session.opportunityIds].reverse()) {
      const opportunity = opportunities.find((item) => item.id === opportunityId);
      if (!opportunity) {
        throw new Error(`missing-opportunity:${opportunityId}`);
      }

      const rollbackResult = await rollbackFixOpportunity(opportunity, persistence);
      if (rollbackResult.state !== 'RolledBack') {
        return {
          ...session,
          rollbackHistory: [
            ...session.rollbackHistory,
            {
              rolledBackAt,
              state: 'RollbackFailed',
              validationErrors: rollbackResult.validationErrors,
            },
          ],
          state: 'RollbackFailed',
        };
      }
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
  } catch (error) {
    return {
      ...session,
      rollbackHistory: [
        ...session.rollbackHistory,
        {
          rolledBackAt,
          state: 'RollbackFailed',
          validationErrors: [error instanceof Error ? error.message : 'rollback-failed'],
        },
      ],
      state: 'RollbackFailed',
    };
  }
}
