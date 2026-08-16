import type {
  FixApplySessionRecord,
  FixGroupedOutcomeSummary,
  FixOpportunity,
  FixSessionRollbackRecord,
} from '../contracts/scorePanel';

export function createFixApplySessionRecord(input: {
  appliedAt: string;
  opportunities: Pick<FixOpportunity, 'id' | 'title' | 'state'>[];
  rollbackAvailable: boolean;
  groupedOutcomeSummary?: FixGroupedOutcomeSummary;
}): FixApplySessionRecord {
  return {
    id: `fix-session-${input.appliedAt}`,
    appliedAt: input.appliedAt,
    opportunityIds: input.opportunities.map((opportunity) => opportunity.id),
    opportunityTitles: input.opportunities.map((opportunity) => opportunity.title),
    rollbackAvailable: input.rollbackAvailable,
    rollbackHistory: [],
    groupedOutcomeSummary: input.groupedOutcomeSummary,
  };
}

export function recordFixSessionRollback(
  session: FixApplySessionRecord,
  rollback: FixSessionRollbackRecord,
): FixApplySessionRecord {
  return {
    ...session,
    rollbackHistory: [...session.rollbackHistory, rollback],
  };
}

export function markFixSessionRegenerated(
  session: FixApplySessionRecord,
  update: {
    staleOpportunityIds: string[];
    regeneratedOpportunityIds: string[];
  },
): FixApplySessionRecord {
  return {
    ...session,
    staleOpportunityIds: update.staleOpportunityIds,
    regeneratedOpportunityIds: update.regeneratedOpportunityIds,
  };
}
