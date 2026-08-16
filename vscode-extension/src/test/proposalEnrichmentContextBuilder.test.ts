import type { FixPlanItem, NormalizedFinding, PageScore, ProposalEnricherId, ScoreResult } from '../analyzer/contracts/scorePanel';
import { buildProposalEnrichmentContext } from '../analyzer/proposalEnrichment/proposalEnrichmentContextBuilder';

function finding(overrides: Partial<NormalizedFinding> = {}): NormalizedFinding {
  return {
    id: 'finding-1',
    title: 'Missing benchmark',
    summary: 'The KPI band shows performance but no target benchmark.',
    severity: 'high',
    confidence: 91,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Overview'],
    impactArea: 'benchmark',
    frameworkImpact: ['Narrative Design'],
    recommendation: 'Add a clear target benchmark beside the KPI.',
    sourceKind: 'framework',
    sourceSection: 'issues',
    evidence: [],
    ...overrides,
  };
}

function pageScore(overrides: Partial<PageScore> = {}): PageScore {
  return {
    pageName: 'Overview',
    gestaltScore: 81,
    cognitiveLoadScore: 74,
    dataInkScore: 72,
    accessibilityScore: 68,
    visualBestPracticesScore: 75,
    stephenFewScore: 70,
    enterpriseGovernanceScore: 71,
    tufteScore: 69,
    graphicalPerceptionScore: 70,
    densityScore: 66,
    narrativeScore: 73,
    compositeScore: 74,
    feedback: {},
    recommendations: [],
    visualMetadata: {
      pageName: 'Overview',
      visiblePageTitle: 'Sales Overview',
      strictVisiblePageTitle: 'Sales Overview',
      canvasWidth: 1280,
      canvasHeight: 720,
      semanticColorMap: [],
      visualCount: 1,
      visibleTitleVisualCount: 1,
      textVisualCount: 1,
      slicerCount: 0,
      legendVisualCount: 0,
      axisLabelVisualCount: 0,
      dataLabelVisualCount: 0,
      formattedVisualCount: 1,
      visuals: [],
    },
    pagePurposeAnalysis: {
      inferredPurpose: 'Executive',
      confidence: 'high',
      actionabilityScore: 58,
      benchmarkStatus: 'Benchmark missing',
      topGaps: ['Target benchmark is missing'],
      whyThisMatters: 'Executive readers need a benchmark to interpret performance.',
    },
    ...overrides,
  };
}

function fixPlanItem(overrides: Partial<FixPlanItem> = {}): FixPlanItem {
  return {
    id: 'fix-decision-context:Overview',
    title: 'Add benchmarks and decision context',
    detail: 'Resolve the decision-context gap on this page.',
    severity: 'high',
    effort: 'low',
    impact: 'high',
    why: 'Reduces risk of KPI misinterpretation.',
    scope: 'page',
    affectedPages: ['Overview'],
    recommendedAction: 'Add a target benchmark beside the KPI.',
    resolvedOutcomes: ['Benchmark gap', 'Actionability gap'],
    sourceFindingIds: ['finding-1'],
    ...overrides,
  };
}

function scoreResult(overrides: Partial<ScoreResult> = {}): ScoreResult {
  return {
    gestaltScore: 81,
    cognitiveLoadScore: 74,
    dataInkScore: 72,
    accessibilityScore: 68,
    visualBestPracticesScore: 75,
    stephenFewScore: 70,
    enterpriseGovernanceScore: 71,
    tufteScore: 69,
    graphicalPerceptionScore: 70,
    densityScore: 66,
    narrativeScore: 73,
    compositeScore: 74,
    feedback: {},
    pageCount: 1,
    recommendations: [],
    reportPath: '/tmp/Sales.Report',
    scoredAt: '2026-06-02T20:00:00.000Z',
    normalizedFindings: [finding()],
    fixPlan: [fixPlanItem()],
    fixOpportunities: [],
    pageScores: [pageScore()],
    pagePurposeAnalysis: {
      inferredPurpose: 'Executive',
      confidence: 'high',
      actionabilityScore: 58,
      benchmarkStatus: 'Benchmark missing',
      topGaps: ['Target benchmark is missing'],
      whyThisMatters: 'Executive readers need a benchmark to interpret performance.',
    },
    ...overrides,
  };
}

describe('buildProposalEnrichmentContext', () => {
  it('builds bounded grounded context from findings remediation and page metadata', () => {
    const result = scoreResult();
    const remediationItem = result.fixPlan?.[0] as FixPlanItem;

    const context = buildProposalEnrichmentContext({
      result,
      remediationItem,
      enricherIds: ['storytelling', 'executiveReadability'] satisfies ProposalEnricherId[],
    });

    expect(context.remediationItemId).toBe(remediationItem.id);
    expect(context.enricherIds).toEqual(['storytelling', 'executiveReadability']);
    expect(context.affectedPages).toEqual(['Overview']);
    expect(context.findings).toEqual([
      expect.objectContaining({
        id: 'finding-1',
        title: 'Missing benchmark',
        severity: 'high',
        recommendation: 'Add a clear target benchmark beside the KPI.',
      }),
    ]);
    expect(context.pageSummaries).toEqual([
      expect.objectContaining({
        pageName: 'Overview',
        visiblePageTitle: 'Sales Overview',
        inferredPurpose: 'Executive',
        whyThisMatters: 'Executive readers need a benchmark to interpret performance.',
      }),
    ]);
  });

  it('excludes raw mutation internals and unrestricted file content from prompt context', () => {
    const result = scoreResult({
      fixOpportunities: [
        {
          id: 'fixopp-1',
          remediationItemId: 'fix-decision-context:Overview',
          title: 'Preview benchmark title mutation',
          category: 'title',
          summary: 'Would update title wording.',
          confidence: 88,
          safetyClass: 'safe',
          affectedPages: ['Overview'],
          targetObjectIds: ['title-1'],
          sourceFindingIds: ['finding-1'],
          expectedResolutions: ['Benchmark gap'],
          mutations: [
            {
              id: 'mut-1',
              pageName: 'Overview',
              targetObjectId: 'title-1',
              targetFile: '/tmp/unsafe/visual.json',
              propertyPath: 'title.text',
              mutationType: 'setTitleText',
              before: 'A',
              after: 'B',
            },
          ],
          previewRows: [],
          rollbackPlan: {
            id: 'rb-1',
            fixOpportunityId: 'fixopp-1',
            fileBackups: [{ targetFile: '/tmp/unsafe/visual.json', beforeContent: '{"secret":true}' }],
            reverseMutations: [],
          },
          state: 'Previewed',
        },
      ],
    });

    const context = buildProposalEnrichmentContext({
      result,
      remediationItem: result.fixPlan?.[0] as FixPlanItem,
      enricherIds: ['layout'],
    });

    expect(JSON.stringify(context)).not.toContain('beforeContent');
    expect(JSON.stringify(context)).not.toContain('/tmp/unsafe/visual.json');
    expect(context.supportedOpportunityCategories).toEqual(['title']);
    expect(context.hasDeterministicOpportunities).toBe(true);
  });
});
