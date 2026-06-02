import type { FixOpportunity, FixOutcomeStatus, ScoreResult } from '../contracts/scorePanel';

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
    } else if (previous && severityRank(next.severity) > severityRank(previous.severity)) {
      status = 'Improved';
    } else if (previous && severityRank(next.severity) === severityRank(previous.severity)) {
      status = 'Unexpected';
    } else {
      status = 'Unchanged';
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
