import type {
  FixGroupedOutcomeSummary,
  FixOpportunity,
  FixOutcomeStatus,
  ScoreResult,
} from '../contracts/scorePanel';

function severityRank(severity: 'high' | 'medium' | 'low' | 'info'): number {
  switch (severity) {
    case 'high':
      return 0;
    case 'medium':
      return 1;
    case 'low':
      return 2;
    default:
      return 3;
  }
}

export function evaluateFixOutcome(
  opportunity: FixOpportunity,
  previousResult: ScoreResult,
  nextResult: ScoreResult,
): { nextState: FixOpportunity['state']; outcome: NonNullable<FixOpportunity['outcome']> } {
  const previousFindings = new Map((previousResult.normalizedFindings ?? []).map((finding) => [finding.id, finding]));
  const nextFindings = new Map((nextResult.normalizedFindings ?? []).map((finding) => [finding.id, finding]));

  const entries = opportunity.sourceFindingIds.map((findingId) => {
    const previous = previousFindings.get(findingId);
    const next = nextFindings.get(findingId);

    let status: FixOutcomeStatus = 'Unexpected';
    if (!next) {
      status = 'Resolved';
    } else if (!previous) {
      status = 'Unexpected';
    } else if (severityRank(next.severity) > severityRank(previous.severity)) {
      status = 'Improved';
    } else if (severityRank(next.severity) === severityRank(previous.severity)) {
      status = 'Unchanged';
    } else {
      status = 'Unexpected';
    }

    return {
      findingId,
      title: previous?.title ?? next?.title ?? findingId,
      status,
    };
  });

  const hasUnexpected = entries.some((entry) => entry.status === 'Unexpected');

  return {
    nextState: hasUnexpected ? 'AppliedWithUnexpectedOutcome' : 'Applied',
    outcome: {
      entries,
    },
  };
}

export function summarizeBatchFixOutcomes(
  items: Array<{
    opportunityId: string;
    title: string;
    state: FixOpportunity['state'];
    outcome: NonNullable<FixOpportunity['outcome']>;
  }>,
): FixGroupedOutcomeSummary {
  const statusMap = new Map<FixOutcomeStatus, { count: number; opportunityIds: string[] }>();

  for (const item of items) {
    for (const entry of item.outcome.entries) {
      const existing = statusMap.get(entry.status) ?? { count: 0, opportunityIds: [] };
      existing.count += 1;
      if (!existing.opportunityIds.includes(item.opportunityId)) {
        existing.opportunityIds.push(item.opportunityId);
      }
      statusMap.set(entry.status, existing);
    }
  }

  return {
    totalEntries: items.reduce((sum, item) => sum + item.outcome.entries.length, 0),
    statuses: [...statusMap.entries()].map(([status, detail]) => ({
      status,
      count: detail.count,
      opportunityIds: detail.opportunityIds,
    })),
    appliedWithUnexpectedOutcomeOpportunityIds: items
      .filter((item) => item.state === 'AppliedWithUnexpectedOutcome')
      .map((item) => item.opportunityId),
  };
}
