import type {
  FixPlanItem,
  PageScore,
  ProposalEnricherId,
  ScoreResult,
} from '../contracts/scorePanel';
import type { ProposalEnrichmentContext, ProposalEnrichmentPageSummary } from './proposalEnrichmentProvider';

function collectPageSummary(page: PageScore): ProposalEnrichmentPageSummary {
  return {
    pageName: page.pageName,
    visiblePageTitle: page.visualMetadata?.visiblePageTitle ?? page.visualMetadata?.strictVisiblePageTitle,
    inferredPurpose: page.pagePurposeAnalysis?.inferredPurpose,
    whyThisMatters: page.pagePurposeAnalysis?.whyThisMatters,
  };
}

export function buildProposalEnrichmentContext(input: {
  result: ScoreResult;
  remediationItem: FixPlanItem;
  enricherIds: ProposalEnricherId[];
}): ProposalEnrichmentContext {
  const { result, remediationItem, enricherIds } = input;
  const relevantFindings = (result.normalizedFindings ?? [])
    .filter((finding) => remediationItem.sourceFindingIds.includes(finding.id))
    .map((finding) => ({
      id: finding.id,
      title: finding.title,
      summary: finding.summary,
      severity: finding.severity,
      impactArea: finding.impactArea,
      recommendation: finding.recommendation,
    }));

  const relevantPages = (result.pageScores ?? [])
    .filter((page) => remediationItem.affectedPages.includes(page.pageName))
    .map(collectPageSummary);

  const supportedOpportunityCategories = [...new Set((result.fixOpportunities ?? [])
    .filter((opportunity) => opportunity.remediationItemId === remediationItem.id)
    .map((opportunity) => opportunity.category))];

  return {
    remediationItemId: remediationItem.id,
    remediationTitle: remediationItem.title,
    remediationDetail: remediationItem.detail,
    remediationWhy: remediationItem.why,
    recommendedAction: remediationItem.recommendedAction,
    resolvedOutcomes: [...remediationItem.resolvedOutcomes],
    affectedPages: [...remediationItem.affectedPages],
    findings: relevantFindings,
    pageSummaries: relevantPages,
    supportedOpportunityCategories,
    hasDeterministicOpportunities: supportedOpportunityCategories.length > 0,
    enricherIds: [...enricherIds],
  };
}
