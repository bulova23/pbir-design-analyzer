import type {
  GuidedStoryImprovement,
  PageScore,
  ScoreResult,
  VisualMetadataItem,
} from '../analyzer/contracts/scorePanel';
import { attachNavigationTargets } from '../analyzer/score/navigationTargets';

function buildVisual(overrides: Partial<VisualMetadataItem> = {}): VisualMetadataItem {
  return {
    visualId: 'visual-1',
    visualType: 'barChart',
    x: 0,
    y: 0,
    width: 320,
    height: 180,
    isHidden: false,
    isNavigationElement: false,
    isDecorative: false,
    isSlicer: false,
    hasVisibleTitleIntent: false,
    categoryHints: [],
    valueHints: [],
    seriesHints: [],
    measureHints: [],
    semanticColors: [],
    ...overrides,
  };
}

function buildResult(improvement: GuidedStoryImprovement, visuals: VisualMetadataItem[]): ScoreResult {
  const pageScore: PageScore = {
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
    guidedStoryImprovements: {
      highPriorityImprovements: [improvement],
      mediumPriorityImprovements: [],
      storyImprovementRationale: 'Story rationale',
    },
    visualMetadata: {
      pageName: 'Overview',
      semanticColorMap: [],
      visualCount: visuals.length,
      visibleTitleVisualCount: 0,
      textVisualCount: 0,
      slicerCount: visuals.filter((item) => item.isSlicer).length,
      legendVisualCount: 0,
      axisLabelVisualCount: 0,
      dataLabelVisualCount: 0,
      formattedVisualCount: 0,
      visuals,
    },
  };

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
    normalizedFindings: [
      {
        id: `overview-guided-story-${improvement.id}`,
        title: improvement.title,
        summary: improvement.summary,
        severity: 'high',
        confidence: 90,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: improvement.relatedImpactArea,
        frameworkImpact: ['Story Assessment'],
        recommendation: improvement.rationale,
        sourceKind: 'guidedStoryImprovement',
        sourceSection: 'issues',
        evidence: [],
      },
    ],
    fixPlan: [
      {
        id: `fix-${improvement.id}`,
        title: improvement.title,
        detail: improvement.summary,
        severity: 'high',
        effort: 'low',
        impact: 'high',
        why: improvement.rationale,
        scope: 'page',
        affectedPages: ['Overview'],
        recommendedAction: improvement.rationale,
        resolvedOutcomes: [improvement.expectedImpact],
        sourceFindingIds: [`overview-guided-story-${improvement.id}`],
      },
    ],
  };
}

describe('attachNavigationTargets', () => {
  it('maps missing title improvements to a page target', () => {
    const result = attachNavigationTargets(buildResult({
      id: 'missing-title-question-anchor',
      title: 'Add a clearer page question or title',
      summary: 'The page does not establish its question early enough.',
      rationale: 'Readers need a story anchor before they interpret the visuals.',
      expectedImpact: 'Stronger narrative entry point.',
      priority: 'high',
      relatedImpactArea: 'storytelling',
    }, []));

    expect(result.pageScores?.[0].guidedStoryImprovements?.highPriorityImprovements[0].navigationTarget).toEqual({
      kind: 'page',
      pageName: 'Overview',
      label: 'Open Overview page',
      reason: 'This recommendation affects page framing.',
      supportState: 'direct',
    });
  });

  it('maps benchmark improvements to a stable lead KPI visual when one is identifiable', () => {
    const result = attachNavigationTargets(buildResult({
      id: 'missing-benchmark-target',
      title: 'Add a benchmark or target',
      summary: 'The result appears without a visible target.',
      rationale: 'Readers need an explicit reference point.',
      expectedImpact: 'Clearer decision context.',
      priority: 'high',
      relatedImpactArea: 'benchmark',
    }, [
      buildVisual({
        visualId: 'hero-kpi',
        visualType: 'card',
        measureHints: ['Revenue'],
        valueHints: ['Revenue'],
        visibleTitleText: 'Revenue',
        hasVisibleTitleIntent: true,
      }),
      buildVisual({
        visualId: 'supporting-chart',
        visualType: 'barChart',
        x: 400,
        categoryHints: ['Region'],
        valueHints: ['Revenue'],
      }),
    ]));

    expect(result.pageScores?.[0].guidedStoryImprovements?.highPriorityImprovements[0].navigationTarget).toMatchObject({
      kind: 'visual',
      pageName: 'Overview',
      visualId: 'hero-kpi',
      supportState: 'direct',
    });
  });

  it('falls back to page navigation when benchmark targeting is not stable', () => {
    const result = attachNavigationTargets(buildResult({
      id: 'missing-benchmark-target',
      title: 'Add a benchmark or target',
      summary: 'The result appears without a visible target.',
      rationale: 'Readers need an explicit reference point.',
      expectedImpact: 'Clearer decision context.',
      priority: 'high',
      relatedImpactArea: 'benchmark',
    }, [
      buildVisual({ visualId: 'kpi-a', visualType: 'card', measureHints: ['Revenue'], valueHints: ['Revenue'] }),
      buildVisual({ visualId: 'kpi-b', visualType: 'card', measureHints: ['Margin'], valueHints: ['Margin'] }),
    ]));

    expect(result.pageScores?.[0].guidedStoryImprovements?.highPriorityImprovements[0].navigationTarget).toMatchObject({
      kind: 'page',
      pageName: 'Overview',
      supportState: 'fallback',
    });
  });

  it('maps prior-period context improvements to a trend visual', () => {
    const result = attachNavigationTargets(buildResult({
      id: 'missing-prior-period-context',
      title: 'Add prior-period context',
      summary: 'The page lacks trend context.',
      rationale: 'Readers need to know movement over time.',
      expectedImpact: 'Clearer trend interpretation.',
      priority: 'high',
      relatedImpactArea: 'benchmark',
    }, [
      buildVisual({
        visualId: 'trend',
        visualType: 'lineChart',
        chartIntent: {
          intent: 'trend',
          confidence: 'high',
          evidence: ['line chart'],
          recommendedAlternatives: [],
        },
      }),
    ]));

    expect(result.pageScores?.[0].guidedStoryImprovements?.highPriorityImprovements[0].navigationTarget?.visualId).toBe('trend');
  });

  it('maps primary metric improvements to the lead metric visual', () => {
    const result = attachNavigationTargets(buildResult({
      id: 'missing-primary-metric',
      title: 'Make the primary metric more explicit',
      summary: 'No clear headline measure is established.',
      rationale: 'Readers need one obvious metric anchor.',
      expectedImpact: 'A clearer first takeaway.',
      priority: 'high',
      relatedImpactArea: 'kpiEffectiveness',
    }, [
      buildVisual({
        visualId: 'headline-metric',
        visualType: 'card',
        y: 0,
        valueHints: ['Revenue'],
        measureHints: ['Revenue'],
      }),
      buildVisual({
        visualId: 'supporting-table',
        visualType: 'tableEx',
        y: 220,
      }),
    ]));

    expect(result.pageScores?.[0].guidedStoryImprovements?.highPriorityImprovements[0].navigationTarget?.visualId).toBe('headline-metric');
  });

  it('maps primary dimension improvements to a comparison visual', () => {
    const result = attachNavigationTargets(buildResult({
      id: 'missing-primary-dimension',
      title: 'Clarify the primary comparison dimension',
      summary: 'The page does not establish what should be compared.',
      rationale: 'Readers need a clear grouping anchor.',
      expectedImpact: 'A more legible comparison story.',
      priority: 'high',
      relatedImpactArea: 'storytelling',
    }, [
      buildVisual({
        visualId: 'region-comparison',
        visualType: 'barChart',
        categoryHints: ['Region'],
        chartIntent: {
          intent: 'comparison',
          confidence: 'high',
          evidence: ['bar chart'],
          recommendedAlternatives: [],
        },
      }),
    ]));

    expect(result.pageScores?.[0].guidedStoryImprovements?.highPriorityImprovements[0].navigationTarget?.visualId).toBe('region-comparison');
  });

  it('maps scattered filter improvements to a slicer when one clear target exists and propagates it downstream', () => {
    const result = attachNavigationTargets(buildResult({
      id: 'scattered-filters',
      title: 'Consolidate scattered filters',
      summary: 'Filter controls are split across the page.',
      rationale: 'A single control zone creates a cleaner reading flow.',
      expectedImpact: 'Cleaner exploration entry point.',
      priority: 'medium',
      relatedImpactArea: 'navigation',
    }, [
      buildVisual({
        visualId: 'filter-cluster',
        visualType: 'slicer',
        isSlicer: true,
        visibleTitleText: 'Region filter',
      }),
    ]));

    const target = result.pageScores?.[0].guidedStoryImprovements?.highPriorityImprovements[0].navigationTarget
      ?? result.pageScores?.[0].guidedStoryImprovements?.mediumPriorityImprovements[0].navigationTarget;

    expect(target).toMatchObject({
      kind: 'visual',
      pageName: 'Overview',
      visualId: 'filter-cluster',
    });
    expect(result.normalizedFindings?.[0].navigationTarget).toMatchObject({
      visualId: 'filter-cluster',
    });
    expect(result.fixPlan?.[0].navigationTarget).toMatchObject({
      visualId: 'filter-cluster',
    });
  });
});
