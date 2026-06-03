import * as fs from 'fs';
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

function getPropertyValue(source: unknown, propertyPath: string): unknown {
  return propertyPath.split('.').reduce<unknown>((current, segment) => {
    if (!isRecord(current)) {
      return undefined;
    }
    return current[segment];
  }, source);
}

function setPropertyValue(source: Record<string, unknown>, propertyPath: string, value: unknown): void {
  const segments = propertyPath.split('.');
  let current: Record<string, unknown> = source;
  for (const segment of segments.slice(0, -1)) {
    const next = current[segment];
    if (!isRecord(next)) {
      current[segment] = {};
    }
    current = current[segment] as Record<string, unknown>;
  }

  current[segments[segments.length - 1]] = value;
}

function validateMutations(mutations: FixMutation[]): string[] {
  return mutations.flatMap((mutation) => {
    const content = JSON.parse(fs.readFileSync(mutation.targetFile, 'utf8')) as unknown;
    const currentValue = getPropertyValue(content, mutation.propertyPath);
    return currentValue === mutation.before
      ? []
      : [`${mutation.targetObjectId}:${mutation.propertyPath}`];
  });
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

  const fileJson = new Map<string, Record<string, unknown>>();
  for (const mutation of opportunity.mutations) {
    if (!fileJson.has(mutation.targetFile)) {
      fileJson.set(mutation.targetFile, JSON.parse(fs.readFileSync(mutation.targetFile, 'utf8')) as Record<string, unknown>);
    }

    setPropertyValue(fileJson.get(mutation.targetFile)!, mutation.propertyPath, mutation.after);
  }

  for (const [targetFile, json] of fileJson.entries()) {
    fs.writeFileSync(targetFile, JSON.stringify(json, null, 2), 'utf8');
  }

  return {
    opportunityId: opportunity.id,
    state: 'Applied',
    appliedMutationCount: opportunity.mutations.length,
    validationErrors: [],
  };
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

  const fileJson = new Map<string, Record<string, unknown>>();
  for (const opportunity of ordered) {
    for (const mutation of opportunity.mutations) {
      if (!fileJson.has(mutation.targetFile)) {
        fileJson.set(mutation.targetFile, JSON.parse(fs.readFileSync(mutation.targetFile, 'utf8')) as Record<string, unknown>);
      }

      setPropertyValue(fileJson.get(mutation.targetFile)!, mutation.propertyPath, mutation.after);
    }
  }

  for (const [targetFile, json] of fileJson.entries()) {
    fs.writeFileSync(targetFile, JSON.stringify(json, null, 2), 'utf8');
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
