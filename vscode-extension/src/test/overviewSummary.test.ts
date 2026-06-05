import type { ScoreResult } from '../analyzer/contracts/scorePanel';
import { buildOverviewSummary } from '../analyzer/score/overviewSummary';

describe('buildOverviewSummary', () => {
  it('derives deterministic maturity, risk, and executive rollups from existing signals', () => {
    const result = {
      compositeScore: 78,
      pageCount: 3,
      reportConsistencySummary: {
        consistentTitleAnchors: false,
        consistentFilterBand: true,
        consistentMetricLabels: true,
        consistentSemanticColors: false,
        overallFinding: '2 cross-page consistency issues detected.',
        affectedPages: ['Overview', 'Detail'],
        issueCount: 2,
        issues: [],
        findings: ['Navigation patterns drift on detail pages.'],
      },
      benchmarkComparison: {
        archetype: 'executive scorecard',
        benchmarkLabel: 'Executive-ready benchmark',
        comparativePosition: 'mixed',
        beautifulButUseless: false,
        insight: 'Decision support is weaker than peer scorecards.',
        strengths: ['Clear KPI band'],
        gaps: ['Weak exception callout'],
      },
      normalizedFindings: [
        {
          id: 'high-actionability',
          title: 'Actionability gap',
          summary: 'Exception visibility is weak.',
          severity: 'high',
          confidence: 88,
          scope: 'page',
          detectionType: 'deterministic',
          affectedPages: ['Overview'],
          impactArea: 'actionability',
          frameworkImpact: ['Narrative Design'],
          recommendation: 'Add a stronger exception callout.',
          sourceKind: 'actionability',
          sourceSection: 'issues',
          evidence: [],
        },
        {
          id: 'medium-navigation',
          title: 'Navigation inconsistency',
          summary: 'Navigation controls drift across detail pages.',
          severity: 'medium',
          confidence: 74,
          scope: 'crossPage',
          detectionType: 'deterministic',
          affectedPages: ['Detail'],
          impactArea: 'navigation',
          frameworkImpact: ['Enterprise Governance'],
          recommendation: 'Keep navigation in one predictable zone.',
          sourceKind: 'reportConsistency',
          sourceSection: 'issues',
          evidence: [],
        },
      ],
    } as unknown as ScoreResult;

    const summary = buildOverviewSummary(result);

    expect(summary.maturityBand).toBe('Mature');
    expect(summary.riskBand).toBe('Elevated');
    expect(summary.severityDistribution).toEqual({
      high: 1,
      medium: 1,
      low: 0,
      info: 0,
    });
    expect(summary.topIssues[0]).toMatchObject({
      title: 'Actionability gap',
      sourceFindingIds: ['high-actionability'],
    });
    expect(summary.topActions[0]).toMatchObject({
      detail: 'Add a stronger exception callout.',
      sourceFindingIds: ['high-actionability'],
    });
    expect(summary.benchmarkSummary).toContain('Executive-ready benchmark');
    expect(summary.crossPageSummary.headline).toContain('1 of 3');
  });

  it('adds Fabric App readiness rollups when a readiness assessment is available', () => {
    const result = {
      compositeScore: 78,
      pageCount: 3,
      normalizedFindings: [],
      readinessAssessment: {
        overallReadinessScore: 71,
        readinessBand: 'possibleCandidate',
        migrationSummary: 'Promising candidates exist, but navigation complexity should be reduced first.',
        candidatePages: ['Overview', 'Summary'],
        blockers: ['Navigation complexity is likely too Power BI-specific for direct migration.'],
        unsupportedPatterns: [],
        redesignRequiredAreas: ['navigation portability'],
        recommendedNextActions: ['Simplify navigation before treating the report as an app candidate.'],
        estimatedRedesignEffort: 'medium',
        dimensionScores: {
          layoutPortability: 75,
          interactionPortability: 61,
          narrativePortability: 72,
          semanticModelSuitability: 74,
          navigationPortability: 54,
          governancePortability: 70,
          accessibilityPortability: 76,
          visualizationAsCodeOpportunity: 68,
        },
        pageAssessments: [],
        evidence: [],
        governanceSignals: [],
      },
    } as unknown as ScoreResult;

    const summary = buildOverviewSummary(result);

    expect(summary.readinessSummary).toEqual({
      readinessScore: 71,
      readinessBand: 'possibleCandidate',
      candidatePageCount: 2,
      migrationBlockerCount: 1,
      estimatedRedesignEffort: 'medium',
    });
    expect(summary.executiveSummary).toContain('possible migration candidate');
  });
});
