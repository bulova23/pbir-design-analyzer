import { buildVisualAuditContextBlock } from '../analyzer/audit/providers/visualAuditPromptContext';
import type { PageScore } from '../analyzer/contracts/scorePanel';

describe('buildVisualAuditContextBlock', () => {
  it('includes page intent, story, actionability, benchmark, and chart grounding cues', () => {
    const context = buildVisualAuditContextBlock('Overview', {
      pageName: 'Overview',
      gestaltScore: 82,
      cognitiveLoadScore: 74,
      dataInkScore: 78,
      accessibilityScore: 72,
      visualBestPracticesScore: 84,
      stephenFewScore: 70,
      enterpriseGovernanceScore: 76,
      tufteScore: 69,
      graphicalPerceptionScore: 71,
      densityScore: 66,
      narrativeScore: 68,
      compositeScore: 77,
      feedback: {},
      recommendations: [],
      inferredStorySummary: {
        intentProfile: 'executiveOverview',
        storyArchetype: 'executive overview + comparison',
        inferredStory: 'This page appears to summarize revenue performance over time.',
        confidence: 'high',
        evidence: ['Visible title: Revenue vs Target'],
      },
      pageIntentProfile: {
        inferredProfile: 'executive',
        actionabilityExpectation: 'high',
        reviewGuidance: ['Executive pages should expose the target, exception, and supporting evidence quickly.'],
        evidence: ['2 KPI cards in the top band'],
      },
      actionabilityBreakdown: {
        score: 60,
        targetBenchmarkPresent: true,
        exceptionVisibility: false,
        urgencySignaling: false,
        priorPeriodContext: true,
        drillPathPresent: true,
        expectationLevel: 'high',
        strengths: ['Prior period context is visible.'],
        gaps: ['Exception visibility is weak.'],
        summary: 'The page includes some decision context but still hides the main exception.',
      },
      benchmarkComparison: {
        archetype: 'executive scorecard',
        benchmarkLabel: 'Executive-ready benchmark',
        comparativePosition: 'mixed',
        beautifulButUseless: false,
        insight: 'The visual polish is solid, but exception visibility is still weaker than the benchmark.',
        strengths: ['Clear KPI band'],
        gaps: ['Weak exception callout'],
      },
      visualMetadata: {
        pageName: 'Overview',
        visiblePageTitle: 'Revenue vs Target',
        semanticColorMap: [],
        chartIntentSummary: {
          intent: 'comparison',
          confidence: 'high',
          evidence: ['Revenue by Region'],
          fitStatus: 'good',
          recommendedAlternatives: [],
        },
        visualCount: 1,
        visibleTitleVisualCount: 1,
        textVisualCount: 0,
        slicerCount: 0,
        legendVisualCount: 1,
        axisLabelVisualCount: 1,
        dataLabelVisualCount: 1,
        formattedVisualCount: 1,
        visuals: [],
      },
    } as PageScore);

    expect(context).toContain('Page intent profile: executive');
    expect(context).toContain('Story archetype: executive overview + comparison');
    expect(context).toContain('Actionability score: 60.0');
    expect(context).toContain('Benchmark comparison: executive scorecard');
    expect(context).toContain('Distinguish rendered/layout issues from metadata/model issues');
  });
});
