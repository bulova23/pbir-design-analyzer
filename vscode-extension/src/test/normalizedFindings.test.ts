import type { NormalizedFinding, NormalizedFindingSeverity, ScoreResult } from '../analyzer/contracts/scorePanel';
import { buildNormalizedFindings } from '../analyzer/score/normalizedFindings';

describe('normalized finding contract', () => {
  it('supports the issue workspace attributes needed for triage', () => {
    const finding: NormalizedFinding = {
      id: 'consistency-title-anchor',
      title: 'Inconsistent title anchors across pages',
      summary: 'Page titles drift vertically across overview and detail pages.',
      severity: 'high',
      confidence: 92,
      scope: 'crossPage',
      detectionType: 'deterministic',
      affectedPages: ['Intro', 'Net Sales'],
      impactArea: 'governance',
      frameworkImpact: ['Enterprise Governance', 'Gestalt Principles'],
      recommendation: 'Normalize title anchor placement across report pages.',
      sourceKind: 'reportConsistency',
      sourceSection: 'issues',
      evidence: [],
    };

    const severity: NormalizedFindingSeverity = finding.severity;

    expect(severity).toBe('high');
    expect(finding.confidence).toBeGreaterThanOrEqual(0);
    expect(finding.confidence).toBeLessThanOrEqual(100);
  });
});

describe('buildNormalizedFindings', () => {
  it('maps report consistency issues into normalized findings', () => {
    const findings = buildNormalizedFindings({
      gestaltScore: 0,
      cognitiveLoadScore: 0,
      dataInkScore: 0,
      accessibilityScore: 0,
      visualBestPracticesScore: 0,
      stephenFewScore: 0,
      enterpriseGovernanceScore: 0,
      tufteScore: 0,
      graphicalPerceptionScore: 0,
      densityScore: 0,
      narrativeScore: 0,
      compositeScore: 0,
      feedback: {},
      pageCount: 2,
      recommendations: [],
      reportPath: '/tmp/report',
      scoredAt: '2026-05-30T00:00:00.000Z',
      reportConsistencySummary: {
        consistentTitleAnchors: false,
        consistentFilterBand: true,
        consistentMetricLabels: true,
        consistentSemanticColors: true,
        issueCount: 1,
        affectedPages: ['Intro', 'Forecast'],
        findings: [],
        issues: [
          {
            category: 'Layout Consistency',
            issueCategory: 'titleAnchors',
            overallFinding: 'Title positions drift across pages.',
            affectedPages: ['Intro', 'Forecast'],
            severity: 'high',
            confidence: 'high',
            recommendedRemediation: 'Align title anchors.',
          },
        ],
      },
    } as ScoreResult);

    expect(findings).toHaveLength(1);
    expect(findings[0]).toMatchObject({
      severity: 'high',
      scope: 'crossPage',
      detectionType: 'deterministic',
      affectedPages: ['Intro', 'Forecast'],
      impactArea: 'layout',
      recommendation: 'Align title anchors.',
    });
  });

  it('maps framework, actionability, and benchmark gaps into issue findings', () => {
    const findings = buildNormalizedFindings({
      gestaltScore: 0,
      cognitiveLoadScore: 0,
      dataInkScore: 0,
      accessibilityScore: 0,
      visualBestPracticesScore: 0,
      stephenFewScore: 0,
      enterpriseGovernanceScore: 0,
      tufteScore: 0,
      graphicalPerceptionScore: 0,
      densityScore: 0,
      narrativeScore: 0,
      compositeScore: 0,
      feedback: {},
      pageCount: 1,
      recommendations: [],
      reportPath: '/tmp/report',
      scoredAt: '2026-05-30T00:00:00.000Z',
      pageScores: [
        {
          pageName: 'Overview',
          gestaltScore: 0,
          cognitiveLoadScore: 0,
          dataInkScore: 0,
          accessibilityScore: 0,
          visualBestPracticesScore: 0,
          stephenFewScore: 0,
          enterpriseGovernanceScore: 0,
          tufteScore: 0,
          graphicalPerceptionScore: 0,
          densityScore: 0,
          narrativeScore: 0,
          compositeScore: 0,
          feedback: {
            cognitiveLoad: [
              {
                ok: false,
                text: 'Visual density: Several visuals compete for attention — simplify the page or split it into sub-pages.',
                findingType: 'strongHeuristic',
              },
            ],
          },
          recommendations: [],
          actionabilityBreakdown: {
            score: 42,
            targetBenchmarkPresent: false,
            exceptionVisibility: false,
            urgencySignaling: true,
            priorPeriodContext: false,
            drillPathPresent: false,
            expectationLevel: 'high',
            strengths: [],
            gaps: ['No clear benchmark target.', 'No visible drill path.'],
            summary: 'The page is visually clear but not actionable enough.',
          },
          benchmarkComparison: {
            archetype: 'Executive Scorecard',
            benchmarkLabel: 'Enterprise norms',
            comparativePosition: 'below',
            beautifulButUseless: true,
            insight: 'The page looks polished but does not support decisions quickly.',
            strengths: [],
            gaps: ['Decision context is weak.'],
          },
        },
      ],
    } as ScoreResult);

    expect(findings.map((finding) => finding.impactArea)).toEqual(
      expect.arrayContaining(['density', 'actionability', 'benchmark']),
    );
    expect(findings.find((finding) => finding.title === 'Actionability gap')).toMatchObject({
      severity: 'high',
      affectedPages: ['Overview'],
    });
    expect(findings.find((finding) => finding.title === 'Beautiful but weakly actionable')).toMatchObject({
      severity: 'high',
      affectedPages: ['Overview'],
    });
  });
});
