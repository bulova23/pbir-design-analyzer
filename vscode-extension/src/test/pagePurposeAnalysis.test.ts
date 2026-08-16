import type {
  ActionabilityBreakdown,
  BenchmarkComparisonSummary,
  PageIntentProfile,
  PageStorySummary,
} from '../analyzer/contracts/scorePanel';
import { buildPagePurposeAnalysis } from '../analyzer/score/pagePurposeAnalysis';

describe('buildPagePurposeAnalysis', () => {
  it('builds a summary-first page-purpose analysis with business-context narrative', () => {
    const storySummary: PageStorySummary = {
      intentProfile: 'executiveOverview',
      storyArchetype: 'executive overview + trend + comparison',
      inferredStory: 'This page appears to summarize KPI performance for executives.',
      confidence: 'high',
      evidence: ['Visible title: Executive Overview'],
    };
    const pageIntentProfile: PageIntentProfile = {
      inferredProfile: 'executive',
      actionabilityExpectation: 'high',
      reviewGuidance: ['Expose target, variance, and urgency quickly.'],
      evidence: ['Top-row KPI cards'],
    };
    const actionabilityBreakdown: ActionabilityBreakdown = {
      score: 40,
      targetBenchmarkPresent: false,
      exceptionVisibility: false,
      urgencySignaling: false,
      priorPeriodContext: false,
      drillPathPresent: true,
      expectationLevel: 'high',
      strengths: ['Drill path is present.'],
      gaps: ['Missing target', 'Missing prior-period context', 'Missing urgency cue'],
      summary: 'The page lacks the decision context expected for executive readers.',
    };
    const benchmarkComparison: BenchmarkComparisonSummary = {
      archetype: 'executive scorecard',
      benchmarkLabel: 'Executive-ready benchmark',
      comparativePosition: 'below',
      beautifulButUseless: false,
      insight: 'The page is below the expected benchmark for executive decision support.',
      strengths: [],
      gaps: ['Weak decision context'],
    };

    const analysis = buildPagePurposeAnalysis({
      storySummary,
      pageIntentProfile,
      actionabilityBreakdown,
      benchmarkComparison,
    });

    expect(analysis).toMatchObject({
      inferredPurpose: 'Executive',
      confidence: 'high',
      actionabilityScore: 40,
      benchmarkStatus: 'Below expected',
      topGaps: ['Missing target', 'Missing prior-period context', 'Missing urgency cue'],
    });
    expect(analysis?.whyThisMatters).toContain('executive review');
    expect(analysis?.whyThisMatters).toContain('Decision makers may misinterpret KPI values');
  });
});
