import type {
  CrossPageMatrixSummary,
  FixApplySessionRecord,
  FixPlanItem,
  NormalizedFinding,
  PageScore,
  ScoreResult,
} from '../analyzer/contracts/scorePanel';
import { buildRefactoringContext } from '../analyzer/proposalEnrichment/refactoring/refactoringContextBuilder';

function finding(overrides: Partial<NormalizedFinding> = {}): NormalizedFinding {
  return {
    id: 'finding-layout-1',
    title: 'KPI row lacks alignment',
    summary: 'The KPI row uses inconsistent spacing and alignment on the overview page.',
    severity: 'high',
    confidence: 92,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Overview'],
    impactArea: 'layout',
    frameworkImpact: ['Narrative Design'],
    recommendation: 'Align KPI cards and normalize spacing.',
    sourceKind: 'framework',
    sourceSection: 'issues',
    evidence: [
      {
        kind: 'metadata',
        label: 'KPI spacing mismatch',
        pageName: 'Overview',
      },
    ],
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
    inferredStorySummary: {
      intentProfile: 'executive',
      storyArchetype: 'summary-to-detail',
      inferredStory: 'Lead with KPI status before moving into supporting trend context.',
      confidence: 'high',
      evidence: ['KPI row appears before trend detail'],
    },
    visualMetadata: {
      pageName: 'Overview',
      visiblePageTitle: 'Sales Overview',
      strictVisiblePageTitle: 'Sales Overview',
      canvasWidth: 1280,
      canvasHeight: 720,
      semanticColorMap: [],
      visualCount: 6,
      visibleTitleVisualCount: 1,
      textVisualCount: 2,
      slicerCount: 1,
      legendVisualCount: 0,
      axisLabelVisualCount: 2,
      dataLabelVisualCount: 1,
      formattedVisualCount: 6,
      visuals: [],
    },
    pagePurposeAnalysis: {
      inferredPurpose: 'Executive',
      confidence: 'high',
      actionabilityScore: 58,
      benchmarkStatus: 'Benchmark missing',
      topGaps: ['Target benchmark is missing'],
      whyThisMatters: 'Executive readers need a benchmark and a clear scan path.',
    },
    ...overrides,
  };
}

function fixPlanItem(overrides: Partial<FixPlanItem> = {}): FixPlanItem {
  return {
    id: 'layout-density:Overview',
    title: 'Reduce visual density and align layout',
    detail: 'Resolve the KPI strip spacing and alignment gap on the overview page.',
    severity: 'high',
    effort: 'low',
    impact: 'high',
    why: 'Improves scanability and reduces cognitive load.',
    scope: 'page',
    affectedPages: ['Overview'],
    recommendedAction: 'Align KPI cards and normalize the surrounding spacing.',
    resolvedOutcomes: ['Layout consistency', 'Readability'],
    sourceFindingIds: ['finding-layout-1'],
    ...overrides,
  };
}

function crossPageMatrix(): CrossPageMatrixSummary {
  return {
    dimensions: ['layout', 'story', 'accessibility', 'consistency', 'navigation', 'actionability'],
    rows: [
      {
        pageName: 'Overview',
        cells: [
          {
            pageName: 'Overview',
            dimension: 'layout',
            findingCount: 1,
            highSeverityCount: 1,
            confidenceAverage: 92,
            severity: 'high',
            status: 'weak',
            relatedFindingIds: ['finding-layout-1'],
            summary: 'Layout findings cluster on the overview page.',
          },
        ],
      },
    ],
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
    pageCount: 2,
    recommendations: [],
    reportPath: '/tmp/Sales.Report',
    scoredAt: '2026-06-05T12:00:00.000Z',
    normalizedFindings: [
      finding(),
      finding({
        id: 'finding-story-2',
        impactArea: 'storytelling',
        summary: 'The detail section interrupts the summary-to-detail narrative.',
        recommendation: 'Preserve the summary-to-detail sequencing.',
      }),
    ],
    fixPlan: [fixPlanItem()],
    fixOpportunities: [
      {
        id: 'fixopp-1',
        remediationItemId: 'layout-density:Overview',
        title: 'Align KPI row',
        category: 'alignment',
        summary: 'Normalize KPI positions.',
        confidence: 0.88,
        safetyClass: 'safe',
        affectedPages: ['Overview'],
        targetObjectIds: ['kpi-1'],
        sourceFindingIds: ['finding-layout-1'],
        expectedResolutions: ['Layout consistency'],
        mutations: [
          {
            id: 'mut-1',
            pageName: 'Overview',
            targetObjectId: 'kpi-1',
            targetFile: '/tmp/unsafe/visual.json',
            propertyPath: 'x',
            mutationType: 'setPosition',
            before: 100,
            after: 80,
          },
        ],
        previewRows: [],
        rollbackPlan: {
          id: 'rb-1',
          fixOpportunityId: 'fixopp-1',
          fileBackups: [
            {
              targetFile: '/tmp/unsafe/visual.json',
              beforeContent: '{"secret":true}',
            },
          ],
          reverseMutations: [],
        },
        state: 'Previewed',
      },
    ],
    pageScores: [
      pageScore(),
      pageScore({
        pageName: 'Details',
        visualMetadata: {
          ...pageScore().visualMetadata!,
          pageName: 'Details',
          visiblePageTitle: 'Sales Details',
          strictVisiblePageTitle: 'Sales Details',
        },
      }),
    ],
    crossPageMatrix: crossPageMatrix(),
    pagePurposeAnalysis: {
      inferredPurpose: 'Executive',
      confidence: 'high',
      actionabilityScore: 58,
      benchmarkStatus: 'Benchmark missing',
      topGaps: ['Target benchmark is missing'],
      whyThisMatters: 'Executive readers need a benchmark and a clear scan path.',
    },
    ...overrides,
  };
}

describe('buildRefactoringContext', () => {
  it('includes grounded findings remediation page-story visual and cross-page context', () => {
    const result = scoreResult();
    const remediationItem = result.fixPlan?.[0] as FixPlanItem;

    const context = buildRefactoringContext({
      result,
      remediationItem,
      requestedDomains: ['layout', 'storytelling', 'executiveExperience'],
    });

    expect(context.remediationItemId).toBe(remediationItem.id);
    expect(context.requestedDomains).toEqual(['layout', 'storytelling', 'executiveExperience']);
    expect(context.findings).toEqual([
      expect.objectContaining({
        id: 'finding-layout-1',
        severity: 'high',
        recommendation: 'Align KPI cards and normalize spacing.',
      }),
    ]);
    expect(context.pageSummaries).toEqual([
      expect.objectContaining({
        pageName: 'Overview',
        visiblePageTitle: 'Sales Overview',
        inferredPurpose: 'Executive',
        storyArchetype: 'summary-to-detail',
        inferredStory: expect.stringContaining('Lead with KPI status'),
        visualSummary: expect.objectContaining({
          visualCount: 6,
          slicerCount: 1,
        }),
      }),
    ]);
    expect(context.crossPageCues).toEqual([
      expect.objectContaining({
        pageName: 'Overview',
        dimension: 'layout',
        status: 'weak',
      }),
    ]);
    expect(context.deterministicSupport).toEqual(
      expect.objectContaining({
        hasDeterministicOpportunities: true,
        supportedOpportunityCategories: ['alignment'],
      }),
    );
  });

  it('excludes raw file contents mutation plans rollback plans apply-session history and score rewrites', () => {
    const result = scoreResult();
    const remediationItem = result.fixPlan?.[0] as FixPlanItem;
    const fixApplySessions: FixApplySessionRecord[] = [
      {
        id: 'session-1',
        appliedAt: '2026-06-05T12:10:00.000Z',
        opportunityIds: ['fixopp-1'],
        opportunityTitles: ['Align KPI row'],
        rollbackAvailable: true,
        rollbackHistory: [],
      },
    ];

    const context = buildRefactoringContext({
      result,
      remediationItem,
      requestedDomains: ['layout'],
      fixApplySessions,
    });

    const serialized = JSON.stringify(context);
    expect(serialized).not.toContain('beforeContent');
    expect(serialized).not.toContain('/tmp/unsafe/visual.json');
    expect(serialized).not.toContain('session-1');
    expect(serialized).not.toContain('rollbackHistory');
    expect(serialized).not.toContain('mutations');
    expect(serialized).not.toContain('enterpriseGovernanceScore');
  });
});
