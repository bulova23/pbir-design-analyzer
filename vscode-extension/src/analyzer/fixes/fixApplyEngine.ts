import * as fs from 'fs';
import type { FixApplyResult, FixMutation, FixOpportunity } from '../contracts/scorePanel';

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
