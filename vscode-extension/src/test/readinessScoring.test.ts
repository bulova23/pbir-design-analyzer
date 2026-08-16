import type { ScoreResult } from '../analyzer/contracts/scorePanel';
import { assessFabricAppReadiness } from '../analyzer/fabric/readiness/readinessAnalyzer';

function buildScoreResult(): ScoreResult {
  return {
    gestaltScore: 78,
    cognitiveLoadScore: 70,
    dataInkScore: 74,
    accessibilityScore: 76,
    visualBestPracticesScore: 79,
    stephenFewScore: 73,
    enterpriseGovernanceScore: 72,
    tufteScore: 68,
    graphicalPerceptionScore: 71,
    densityScore: 66,
    narrativeScore: 75,
    compositeScore: 77,
    feedback: {},
    pageCount: 2,
    recommendations: [],
    reportPath: '/tmp/Sales.Report',
    scoredAt: '2026-06-03T21:00:00.000Z',
    dataVisualCount: 9,
    navigationVisualCount: 3,
    hiddenVisualCount: 1,
    pageScores: [
      {
        pageName: 'Executive Overview',
        gestaltScore: 84,
        cognitiveLoadScore: 78,
        dataInkScore: 79,
        accessibilityScore: 82,
        visualBestPracticesScore: 83,
        stephenFewScore: 76,
        enterpriseGovernanceScore: 78,
        tufteScore: 72,
        graphicalPerceptionScore: 75,
        densityScore: 74,
        narrativeScore: 81,
        compositeScore: 80,
        feedback: {},
        recommendations: [],
        actionabilityBreakdown: {
          score: 76,
          targetBenchmarkPresent: true,
          exceptionVisibility: true,
          urgencySignaling: true,
          priorPeriodContext: true,
          drillPathPresent: true,
          expectationLevel: 'high',
          strengths: ['Benchmarks are visible.'],
          gaps: [],
          summary: 'The page exposes a clear decision path with target and prior-period context.',
        },
        pageIntentProfile: {
          inferredProfile: 'executive',
          actionabilityExpectation: 'high',
          reviewGuidance: ['Keep the decision path prominent.'],
          evidence: ['Executive KPI band'],
        },
        visualMetadata: {
          pageName: 'Executive Overview',
          visiblePageTitle: 'Executive Overview',
          semanticColorMap: [
            {
              semanticKey: 'status:on-track',
              color: '#00875A',
              sourceVisualId: 'kpi-1',
              sourcePageName: 'Executive Overview',
            },
          ],
          visualCount: 5,
          visibleTitleVisualCount: 1,
          textVisualCount: 1,
          slicerCount: 1,
          legendVisualCount: 1,
          axisLabelVisualCount: 1,
          dataLabelVisualCount: 1,
          formattedVisualCount: 5,
          visuals: [
            {
              visualId: 'kpi-1',
              visualType: 'card',
              x: 0,
              y: 0,
              width: 100,
              height: 60,
              isHidden: false,
              isNavigationElement: false,
              isDecorative: false,
              isSlicer: false,
              bestVisibleText: 'Revenue',
              hasVisibleTitleIntent: true,
              categoryHints: ['Region'],
              valueHints: ['Revenue'],
              seriesHints: [],
              measureHints: ['Revenue'],
              semanticColors: [],
            },
          ],
        },
      },
      {
        pageName: 'Dense Detail',
        gestaltScore: 54,
        cognitiveLoadScore: 42,
        dataInkScore: 48,
        accessibilityScore: 44,
        visualBestPracticesScore: 46,
        stephenFewScore: 40,
        enterpriseGovernanceScore: 50,
        tufteScore: 38,
        graphicalPerceptionScore: 45,
        densityScore: 34,
        narrativeScore: 43,
        compositeScore: 44,
        feedback: {},
        recommendations: [],
        actionabilityBreakdown: {
          score: 38,
          targetBenchmarkPresent: false,
          exceptionVisibility: false,
          urgencySignaling: false,
          priorPeriodContext: false,
          drillPathPresent: false,
          expectationLevel: 'medium',
          strengths: [],
          gaps: ['No clear benchmark framing.'],
          summary: 'The page is dense and requires substantial interpretation.',
        },
        pageIntentProfile: {
          inferredProfile: 'analytical',
          actionabilityExpectation: 'medium',
          reviewGuidance: ['Clarify the primary question.'],
          evidence: ['High visual count'],
        },
        visualMetadata: {
          pageName: 'Dense Detail',
          visiblePageTitle: 'Dense Detail',
          semanticColorMap: [],
          visualCount: 14,
          visibleTitleVisualCount: 1,
          textVisualCount: 3,
          slicerCount: 5,
          legendVisualCount: 4,
          axisLabelVisualCount: 6,
          dataLabelVisualCount: 0,
          formattedVisualCount: 14,
          visuals: [
            {
              visualId: 'detail-nav',
              visualType: 'button',
              x: 0,
              y: 0,
              width: 100,
              height: 60,
              isHidden: true,
              isNavigationElement: true,
              isDecorative: false,
              isSlicer: false,
              bestVisibleText: 'Go to detail',
              hasVisibleTitleIntent: true,
              categoryHints: [],
              valueHints: [],
              seriesHints: [],
              measureHints: [],
              semanticColors: [],
            },
          ],
        },
      },
    ],
    normalizedFindings: [
      {
        id: 'nav-drift',
        title: 'Navigation inconsistency',
        summary: 'Navigation patterns differ across detail pages.',
        severity: 'medium',
        confidence: 74,
        scope: 'crossPage',
        detectionType: 'deterministic',
        affectedPages: ['Dense Detail'],
        impactArea: 'navigation',
        frameworkImpact: ['Enterprise Governance'],
        recommendation: 'Keep navigation in one predictable zone.',
        sourceKind: 'reportConsistency',
        sourceSection: 'issues',
        evidence: [],
      },
    ],
  };
}

describe('assessFabricAppReadiness', () => {
  it('assigns report readiness bands, page candidate states, and redesign effort from PBIR signals', () => {
    const readiness = assessFabricAppReadiness(buildScoreResult(), 'migrationReadiness');

    expect(readiness.readinessBand).toBe('possibleCandidate');
    expect(readiness.estimatedRedesignEffort).toBe('medium');
    expect(readiness.candidatePages).toEqual(['Executive Overview']);
    expect(readiness.pageAssessments).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          pageName: 'Executive Overview',
          candidateState: 'strongCandidate',
        }),
        expect.objectContaining({
          pageName: 'Dense Detail',
          candidateState: 'keepAsReport',
        }),
      ]),
    );
  });

  it('detects blockers and unsupported patterns for dense Power BI-specific pages', () => {
    const readiness = assessFabricAppReadiness(buildScoreResult(), 'migrationReadiness');
    const denseDetail = readiness.pageAssessments.find((page) => page.pageName === 'Dense Detail');

    expect(denseDetail?.blockers).toEqual(
      expect.arrayContaining([
        expect.stringContaining('slicer'),
        expect.stringContaining('accessibility'),
      ]),
    );
    expect(denseDetail?.unsupportedPatterns).toEqual(
      expect.arrayContaining([
        expect.stringContaining('Hidden-visual'),
        expect.stringContaining('Slicer-heavy'),
      ]),
    );
  });
});
