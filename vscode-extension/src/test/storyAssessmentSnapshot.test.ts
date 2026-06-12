import type {
  GuidedStoryImprovement,
  PageScore,
  ScoreResult,
  ScorePanelNavigationTarget,
} from '../analyzer/contracts/scorePanel';
import {
  buildStoryAssessmentReportSnapshot,
  compareStoryAssessmentSnapshots,
} from '../analyzer/score/storyAssessmentSnapshot';

function buildImprovement(
  overrides: Partial<GuidedStoryImprovement> = {},
  navigationTarget?: ScorePanelNavigationTarget,
): GuidedStoryImprovement {
  return {
    id: 'missing-benchmark-target',
    title: 'Add a benchmark or target',
    summary: 'The result appears without a visible target.',
    rationale: 'Readers need an explicit reference point.',
    expectedImpact: 'Clearer decision context.',
    priority: 'high',
    relatedImpactArea: 'benchmark',
    ...(navigationTarget ? { navigationTarget } : {}),
    ...overrides,
  };
}

function buildPageScore(improvements: GuidedStoryImprovement[]): PageScore {
  return {
    pageName: 'Overview',
    gestaltScore: 80,
    cognitiveLoadScore: 80,
    dataInkScore: 80,
    accessibilityScore: 80,
    visualBestPracticesScore: 80,
    stephenFewScore: 80,
    enterpriseGovernanceScore: 80,
    tufteScore: 80,
    graphicalPerceptionScore: 80,
    densityScore: 80,
    narrativeScore: 80,
    compositeScore: 80,
    feedback: {},
    recommendations: [],
    pageIntentProfile: {
      inferredProfile: 'executive',
      actionabilityExpectation: 'high',
      reviewGuidance: ['Executive pages should expose the target quickly.'],
      evidence: ['Top KPI band'],
    },
    actionabilityBreakdown: {
      score: 58,
      targetBenchmarkPresent: false,
      exceptionVisibility: false,
      urgencySignaling: false,
      priorPeriodContext: true,
      drillPathPresent: true,
      expectationLevel: 'high',
      strengths: ['Prior-period context is visible.'],
      gaps: ['Add a target or benchmark.'],
      summary: 'The page includes some decision context but still hides the main exception.',
    },
    benchmarkComparison: {
      archetype: 'executive scorecard',
      benchmarkLabel: 'Executive-ready benchmark',
      comparativePosition: 'mixed',
      beautifulButUseless: false,
      insight: 'The page is readable, but exception visibility is weaker than the benchmark.',
      strengths: ['Clear KPI band'],
      gaps: ['Weak exception callout'],
    },
    pagePurposeAnalysis: {
      inferredPurpose: 'Executive',
      confidence: 'high',
      actionabilityScore: 58,
      benchmarkStatus: 'Mixed against expected',
      topGaps: ['Exception visibility is weak.'],
      whyThisMatters: 'Decision makers may misinterpret KPI values without targets or prior-period comparison.',
    },
    guidedStoryImprovements: {
      highPriorityImprovements: improvements.filter((item) => item.priority === 'high'),
      mediumPriorityImprovements: improvements.filter((item) => item.priority !== 'high'),
      storyImprovementRationale: 'The page needs stronger story framing.',
    },
  };
}

function buildResult(pageScore: PageScore): ScoreResult {
  return {
    gestaltScore: 80,
    cognitiveLoadScore: 80,
    dataInkScore: 80,
    accessibilityScore: 80,
    visualBestPracticesScore: 80,
    stephenFewScore: 80,
    enterpriseGovernanceScore: 80,
    tufteScore: 80,
    graphicalPerceptionScore: 80,
    densityScore: 80,
    narrativeScore: 80,
    compositeScore: 80,
    feedback: {},
    pageCount: 1,
    recommendations: [],
    reportPath: '/tmp/Sales.Report',
    scoredAt: '2026-06-12T00:00:00.000Z',
    pageScores: [pageScore],
  };
}

describe('storyAssessmentSnapshot', () => {
  it('captures only the public story fields shown to users', () => {
    const snapshot = buildStoryAssessmentReportSnapshot(buildResult(buildPageScore([
      buildImprovement({}, {
        kind: 'visual',
        pageName: 'Overview',
        visualId: 'hero-kpi',
        label: 'Open lead KPI visual',
        reason: 'This recommendation is tied to the lead KPI.',
        supportState: 'direct',
      }),
    ])));

    expect(snapshot.pages).toEqual([
      {
        pageName: 'Overview',
        storyMaturity: 'Developing',
        strongSignals: ['Prior-period context is visible.', 'Clear KPI band', 'Top KPI band'],
        missingSignals: ['No clear exception callout', 'No visible benchmark or target'],
        recommendations: [
          {
            id: 'missing-benchmark-target',
            title: 'Add a benchmark or target',
            summary: 'The result appears without a visible target.',
            rationale: 'Readers need an explicit reference point.',
            expectedImpact: 'Clearer decision context.',
            priority: 'high',
            relatedImpactArea: 'benchmark',
            navigationTarget: {
              kind: 'visual',
              pageName: 'Overview',
              visualId: 'hero-kpi',
              label: 'Open lead KPI visual',
              reason: 'This recommendation is tied to the lead KPI.',
              supportState: 'direct',
            },
          },
        ],
        topImprovementIds: ['missing-benchmark-target'],
      },
    ]);
    expect(JSON.stringify(snapshot)).not.toContain('storyArchetype');
    expect(JSON.stringify(snapshot)).not.toContain('evidence');
    expect(JSON.stringify(snapshot)).not.toContain('benchmarkLabel');
  });

  it('compares prior and current public snapshots for maturity, recommendations, and signal changes', () => {
    const prior = buildStoryAssessmentReportSnapshot(buildResult({
      ...buildPageScore([
        buildImprovement({
          id: 'missing-benchmark-target',
          title: 'Add a benchmark or target',
          summary: 'The result appears without a visible target.',
        }),
        buildImprovement({
          id: 'missing-primary-metric',
          title: 'Make the primary metric more explicit',
          summary: 'No clear headline metric is established.',
          relatedImpactArea: 'kpiEffectiveness',
        }),
      ]),
      actionabilityBreakdown: {
        score: 30,
        targetBenchmarkPresent: false,
        exceptionVisibility: false,
        urgencySignaling: false,
        priorPeriodContext: true,
        drillPathPresent: true,
        expectationLevel: 'high',
        strengths: ['Prior-period context is visible.'],
        gaps: ['Add a target or benchmark.'],
        summary: 'The page still lacks basic decision context.',
      },
      pagePurposeAnalysis: {
        inferredPurpose: 'Executive',
        confidence: 'high',
        actionabilityScore: 30,
        benchmarkStatus: 'Below expected',
        topGaps: ['Exception visibility is weak.'],
        whyThisMatters: 'The page story is still weak.',
      },
    }));

    const current = buildStoryAssessmentReportSnapshot(buildResult({
      ...buildPageScore([
        buildImprovement({
          id: 'missing-primary-metric',
          title: 'Make the primary metric more explicit',
          summary: 'No clear headline metric is established.',
          relatedImpactArea: 'kpiEffectiveness',
        }),
        buildImprovement({
          id: 'scattered-filters',
          title: 'Consolidate scattered filters',
          summary: 'Filters are split across multiple zones.',
          priority: 'medium',
          relatedImpactArea: 'navigation',
        }),
      ]),
      actionabilityBreakdown: {
        score: 74,
        targetBenchmarkPresent: true,
        exceptionVisibility: true,
        urgencySignaling: false,
        priorPeriodContext: true,
        drillPathPresent: true,
        expectationLevel: 'high',
        strengths: ['Prior-period context is visible.', 'Clear exception callout.'],
        gaps: [],
        summary: 'The page is easier to interpret.',
      },
      pagePurposeAnalysis: {
        inferredPurpose: 'Executive',
        confidence: 'high',
        actionabilityScore: 74,
        benchmarkStatus: 'Near expected',
        topGaps: [],
        whyThisMatters: 'The page has a stronger story frame now.',
      },
    }));

    const diff = compareStoryAssessmentSnapshots(prior, current);
    const pageDiff = diff.byPage.Overview;

    expect(pageDiff.maturityChange).toBe('improved');
    expect(pageDiff.resolvedRecommendations.map((item) => item.id)).toEqual(['missing-benchmark-target']);
    expect(pageDiff.newRecommendations.map((item) => item.id)).toEqual(['scattered-filters']);
    expect(pageDiff.unchangedRecommendations.map((item) => item.id)).toEqual(['missing-primary-metric']);
    expect(pageDiff.addedStrongSignals).toEqual(['Clear exception callout.']);
    expect(pageDiff.removedMissingSignals).toContain('No visible benchmark or target');
    expect(pageDiff.summary).toEqual(expect.stringContaining('Story maturity improved'));
  });
});
