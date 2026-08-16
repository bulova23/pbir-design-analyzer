import type {
  AdvisoryAlternative,
  AdvisoryPriority,
  EnrichedExplanation,
  EnrichedImpactSummary,
  EnrichedTitleSuggestion,
  ExpectedOutcomeNarrative,
  ProposalEnricherId,
} from '../contracts/scorePanel';

export interface ProposalEnrichmentFindingSummary {
  id: string;
  title: string;
  summary: string;
  severity: string;
  impactArea: string;
  recommendation: string;
}

export interface ProposalEnrichmentPageSummary {
  pageName: string;
  visiblePageTitle?: string;
  inferredPurpose?: string;
  whyThisMatters?: string;
}

export interface ProposalEnrichmentContext {
  remediationItemId: string;
  remediationTitle: string;
  remediationDetail: string;
  remediationWhy: string;
  recommendedAction: string;
  resolvedOutcomes: string[];
  affectedPages: string[];
  findings: ProposalEnrichmentFindingSummary[];
  pageSummaries: ProposalEnrichmentPageSummary[];
  supportedOpportunityCategories: string[];
  hasDeterministicOpportunities: boolean;
  enricherIds: ProposalEnricherId[];
}

export interface ProposalEnrichmentCandidate {
  titleSuggestions?: EnrichedTitleSuggestion[];
  explanation?: EnrichedExplanation;
  whyThisMatters?: EnrichedImpactSummary;
  advisoryPriority?: AdvisoryPriority;
  expectedOutcome?: ExpectedOutcomeNarrative;
  advisoryAlternatives?: AdvisoryAlternative[];
}

export interface ProposalEnrichmentProvider {
  providerName: string;
  isConfigured(): Promise<boolean>;
  enrich(input: {
    context: ProposalEnrichmentContext;
    enricherIds: ProposalEnricherId[];
  }): Promise<ProposalEnrichmentCandidate>;
}
