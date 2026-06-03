import type {
  FixCompatibilityResult,
  FixConflictReason,
  FixOpportunity,
} from '../contracts/scorePanel';

const incompatibleCategoryPairs = new Set([
  'crossPageConsistency:navigation',
  'navigation:crossPageConsistency',
]);

function buildReason(reason: Omit<FixConflictReason, 'message'>): FixConflictReason {
  switch (reason.code) {
    case 'overlappingMutation':
      return {
        ...reason,
        message: `Selected opportunities both change ${reason.targetObjectId ?? 'the same object'} at ${reason.propertyPath ?? 'the same property'}.`,
      };
    case 'incompatibleCategory':
      return {
        ...reason,
        message: 'Selected opportunities use incompatible deterministic mutation categories and cannot be applied together.',
      };
    case 'staleOpportunity':
      return {
        ...reason,
        message: 'One or more selected opportunities are stale and must be regenerated before preview or apply.',
      };
    case 'targetDrifted':
      return {
        ...reason,
        message: 'One or more selected opportunities no longer match the current target object state.',
      };
    case 'missingRollbackCoverage':
      return {
        ...reason,
        message: 'One or more selected opportunities do not have rollback coverage and cannot be applied safely.',
      };
  }
}

export function evaluateFixOpportunityCompatibility(opportunities: FixOpportunity[]): FixCompatibilityResult {
  const reasons: FixConflictReason[] = [];
  const blockingOpportunityIds = new Set<string>();
  const mutationOwners = new Map<string, string>();

  for (const opportunity of opportunities) {
    if (opportunity.state === 'Stale') {
      reasons.push(buildReason({
        code: 'staleOpportunity',
        opportunityIds: [opportunity.id],
      }));
      blockingOpportunityIds.add(opportunity.id);
    }

    if (opportunity.state === 'FailedValidation') {
      reasons.push(buildReason({
        code: 'targetDrifted',
        opportunityIds: [opportunity.id],
      }));
      blockingOpportunityIds.add(opportunity.id);
    }

    if (opportunity.rollbackPlan.fileBackups.length === 0) {
      reasons.push(buildReason({
        code: 'missingRollbackCoverage',
        opportunityIds: [opportunity.id],
      }));
      blockingOpportunityIds.add(opportunity.id);
    }

    for (const mutation of opportunity.mutations) {
      const key = `${mutation.targetObjectId}::${mutation.propertyPath}`;
      const owner = mutationOwners.get(key);
      if (owner && owner !== opportunity.id) {
        reasons.push(buildReason({
          code: 'overlappingMutation',
          opportunityIds: [owner, opportunity.id],
          targetObjectId: mutation.targetObjectId,
          propertyPath: mutation.propertyPath,
        }));
        blockingOpportunityIds.add(owner);
        blockingOpportunityIds.add(opportunity.id);
      } else {
        mutationOwners.set(key, opportunity.id);
      }
    }
  }

  for (let index = 0; index < opportunities.length; index += 1) {
    for (let compareIndex = index + 1; compareIndex < opportunities.length; compareIndex += 1) {
      const first = opportunities[index];
      const second = opportunities[compareIndex];
      if (incompatibleCategoryPairs.has(`${first.category}:${second.category}`)) {
        reasons.push(buildReason({
          code: 'incompatibleCategory',
          opportunityIds: [first.id, second.id],
        }));
        blockingOpportunityIds.add(first.id);
        blockingOpportunityIds.add(second.id);
      }
    }
  }

  return {
    isCompatible: reasons.length === 0,
    compatibleOpportunityIds: opportunities
      .map((opportunity) => opportunity.id)
      .filter((id) => !blockingOpportunityIds.has(id)),
    blockingOpportunityIds: opportunities
      .map((opportunity) => opportunity.id)
      .filter((id) => blockingOpportunityIds.has(id)),
    blockingReasons: reasons,
  };
}
