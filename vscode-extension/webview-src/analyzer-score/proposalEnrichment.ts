import type { AdvisoryPriority, ProposalEnrichment } from '../../src/analyzer/contracts/scorePanel';

export function hasProposalEnrichmentContent(enrichment: ProposalEnrichment | undefined): boolean {
  if (!enrichment) {
    return false;
  }

  return Boolean(
    (enrichment.titleSuggestions?.length ?? 0) > 0
    || enrichment.explanation
    || enrichment.whyThisMatters
    || enrichment.expectedOutcome
    || (enrichment.advisoryAlternatives?.length ?? 0) > 0,
  );
}

export function getAdvisoryPriorityLabel(tier: AdvisoryPriority['tier']): string {
  switch (tier) {
    case 'highLeverage':
      return 'High leverage';
    case 'quickWin':
      return 'Quick win';
    case 'consistencyCleanup':
      return 'Consistency cleanup';
    case 'advisoryOnly':
      return 'Advisory only';
  }

  return 'Advisory';
}

export function getProposalEnrichmentSummary(enrichment: ProposalEnrichment): string {
  return enrichment.source === 'provider'
    ? 'AI-enriched'
    : 'Fallback guidance';
}
