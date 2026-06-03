import type {
  AdvisoryAlternative,
  AdvisoryPriority,
  EnrichedExplanation,
  EnrichedImpactSummary,
  EnrichedTitleSuggestion,
  ExpectedOutcomeNarrative,
} from '../contracts/scorePanel';
import type { ProposalEnrichmentContext } from './proposalEnrichmentProvider';

export function buildFallbackTitleSuggestions(context: ProposalEnrichmentContext): EnrichedTitleSuggestion[] {
  const pageName = context.pageSummaries[0]?.visiblePageTitle ?? context.affectedPages[0] ?? 'Overview';
  return [{
    title: `${pageName} improvement proposal`,
    confidence: 0.55,
    rationale: 'Derived from the affected page and remediation intent.',
  }];
}

export function buildFallbackExplanation(context: ProposalEnrichmentContext): EnrichedExplanation {
  const recommendation = context.findings[0]?.recommendation ?? context.recommendedAction;
  return {
    shortText: `${recommendation} This guidance stays advisory until a deterministic fix path is available.`,
    expandedText: `${context.remediationWhy} Use the grounded findings and page context to decide whether this remediation should be applied through the existing deterministic workflow.`,
  };
}

export function buildFallbackWhyThisMatters(context: ProposalEnrichmentContext): EnrichedImpactSummary {
  return {
    text: context.pageSummaries[0]?.whyThisMatters
      ?? context.remediationWhy,
  };
}

export function buildFallbackPriority(context: ProposalEnrichmentContext): AdvisoryPriority {
  return {
    tier: context.hasDeterministicOpportunities ? 'highLeverage' : 'advisoryOnly',
    rationale: context.hasDeterministicOpportunities
      ? 'A deterministic opportunity exists, so this remediation is a strong candidate for review.'
      : 'No safe deterministic opportunity is currently available, so this remains advisory.',
  };
}

export function buildFallbackExpectedOutcome(context: ProposalEnrichmentContext): ExpectedOutcomeNarrative {
  return {
    text: `If applied, this change is expected to improve ${context.resolvedOutcomes.join(' and ').toLowerCase()}.`,
    areas: context.resolvedOutcomes.length > 0 ? [...context.resolvedOutcomes] : ['readability'],
  };
}

export function buildFallbackAlternatives(context: ProposalEnrichmentContext): AdvisoryAlternative[] {
  return [{
    title: 'Review the surrounding page context first',
    description: `Use the affected findings on ${context.affectedPages.join(' · ')} to confirm whether this remediation should stay advisory or advance to a deterministic fix path.`,
  }];
}
