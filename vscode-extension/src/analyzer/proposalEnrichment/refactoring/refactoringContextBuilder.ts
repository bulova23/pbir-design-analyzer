import type {
  CrossPageMatrixCell,
  CrossPageMatrixDimension,
  FixApplySessionRecord,
  FixPlanItem,
  NormalizedFinding,
  NormalizedFindingSeverity,
  PageScore,
  RefactoringDomain,
  ScoreResult,
} from '../../contracts/scorePanel';

export interface RefactoringContextFindingSummary {
  id: string;
  title: string;
  summary: string;
  severity: NormalizedFindingSeverity;
  scope: NormalizedFinding['scope'];
  impactArea: NormalizedFinding['impactArea'];
  recommendation: string;
  evidenceLabels: string[];
}

export interface RefactoringContextVisualSummary {
  visualCount: number;
  dataVisualCount?: number;
  navigationVisualCount?: number;
  hiddenVisualCount?: number;
  slicerCount: number;
  visibleTitleVisualCount: number;
  textVisualCount: number;
}

export interface RefactoringContextPageSummary {
  pageName: string;
  visiblePageTitle?: string;
  inferredPurpose?: string;
  whyThisMatters?: string;
  storyArchetype?: string;
  inferredStory?: string;
  visualSummary?: RefactoringContextVisualSummary;
}

export interface RefactoringContextCrossPageCue {
  pageName: string;
  dimension: CrossPageMatrixDimension;
  status: CrossPageMatrixCell['status'];
  summary: string;
  relatedFindingIds: string[];
}

export interface RefactoringDeterministicSupport {
  supportedOpportunityCategories: string[];
  hasDeterministicOpportunities: boolean;
}

export interface RefactoringContext {
  remediationItemId: string;
  remediationTitle: string;
  remediationDetail: string;
  remediationWhy: string;
  recommendedAction: string;
  resolvedOutcomes: string[];
  affectedPages: string[];
  requestedDomains: RefactoringDomain[];
  findings: RefactoringContextFindingSummary[];
  pageSummaries: RefactoringContextPageSummary[];
  crossPageCues: RefactoringContextCrossPageCue[];
  deterministicSupport: RefactoringDeterministicSupport;
}

function toFindingSummary(finding: NormalizedFinding): RefactoringContextFindingSummary {
  return {
    id: finding.id,
    title: finding.title,
    summary: finding.summary,
    severity: finding.severity,
    scope: finding.scope,
    impactArea: finding.impactArea,
    recommendation: finding.recommendation,
    evidenceLabels: finding.evidence.map((item) => item.label),
  };
}

function toPageSummary(page: PageScore): RefactoringContextPageSummary {
  return {
    pageName: page.pageName,
    visiblePageTitle: page.visualMetadata?.visiblePageTitle ?? page.visualMetadata?.strictVisiblePageTitle,
    inferredPurpose: page.pagePurposeAnalysis?.inferredPurpose,
    whyThisMatters: page.pagePurposeAnalysis?.whyThisMatters,
    storyArchetype: page.inferredStorySummary?.storyArchetype,
    inferredStory: page.inferredStorySummary?.inferredStory,
    visualSummary: page.visualMetadata
      ? {
          visualCount: page.visualMetadata.visualCount,
          dataVisualCount: page.dataVisualCount,
          navigationVisualCount: page.navigationVisualCount,
          hiddenVisualCount: page.hiddenVisualCount,
          slicerCount: page.visualMetadata.slicerCount,
          visibleTitleVisualCount: page.visualMetadata.visibleTitleVisualCount,
          textVisualCount: page.visualMetadata.textVisualCount,
        }
      : undefined,
  };
}

function collectCrossPageCues(result: ScoreResult, affectedPages: string[], findingIds: Set<string>): RefactoringContextCrossPageCue[] {
  const rows = result.crossPageMatrix?.rows ?? [];

  return rows
    .filter((row) => affectedPages.includes(row.pageName))
    .flatMap((row) => row.cells
      .filter((cell) => cell.status !== 'strong' && cell.status !== 'unknown')
      .filter((cell) => cell.relatedFindingIds.some((id) => findingIds.has(id)))
      .map((cell) => ({
        pageName: row.pageName,
        dimension: cell.dimension,
        status: cell.status,
        summary: cell.summary,
        relatedFindingIds: [...cell.relatedFindingIds],
      })));
}

function collectDeterministicSupport(result: ScoreResult, remediationItemId: string): RefactoringDeterministicSupport {
  const supportedOpportunityCategories = [...new Set((result.fixOpportunities ?? [])
    .filter((opportunity) => opportunity.remediationItemId === remediationItemId)
    .map((opportunity) => opportunity.category))];

  return {
    supportedOpportunityCategories,
    hasDeterministicOpportunities: supportedOpportunityCategories.length > 0,
  };
}

export function buildRefactoringContext(input: {
  result: ScoreResult;
  remediationItem: FixPlanItem;
  requestedDomains: RefactoringDomain[];
  fixApplySessions?: FixApplySessionRecord[];
}): RefactoringContext {
  const { result, remediationItem, requestedDomains } = input;
  const findings = (result.normalizedFindings ?? [])
    .filter((finding) => remediationItem.sourceFindingIds.includes(finding.id))
    .map(toFindingSummary);
  const pageSummaries = (result.pageScores ?? [])
    .filter((page) => remediationItem.affectedPages.includes(page.pageName))
    .map(toPageSummary);
  const findingIds = new Set(findings.map((finding) => finding.id));

  return {
    remediationItemId: remediationItem.id,
    remediationTitle: remediationItem.title,
    remediationDetail: remediationItem.detail,
    remediationWhy: remediationItem.why,
    recommendedAction: remediationItem.recommendedAction,
    resolvedOutcomes: [...remediationItem.resolvedOutcomes],
    affectedPages: [...remediationItem.affectedPages],
    requestedDomains: [...requestedDomains],
    findings,
    pageSummaries,
    crossPageCues: collectCrossPageCues(result, remediationItem.affectedPages, findingIds),
    deterministicSupport: collectDeterministicSupport(result, remediationItem.id),
  };
}
