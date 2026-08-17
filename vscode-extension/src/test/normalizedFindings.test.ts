import type {
  CustomVisualEvidence,
  NormalizedFinding,
  NormalizedFindingSeverity,
  PageVisualMetadataSummary,
  ScoreResult,
} from '../analyzer/contracts/scorePanel';
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
      affectedPages: ['Forecast', 'Intro'],
      impactArea: 'layout',
      recommendation: 'Align title anchors.',
      reviewClassification: 'deterministic',
      evidenceDomains: ['deterministic'],
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

  it('sorts findings deterministically when severities tie', () => {
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
            visualBestPractices: [
              {
                ok: false,
                text: 'Z issue: Later in the alphabet.',
                findingType: 'strongHeuristic',
              },
            ],
            accessibility: [
              {
                ok: false,
                text: 'A issue: Earlier in the alphabet.',
                findingType: 'strongHeuristic',
              },
            ],
          },
          recommendations: [],
        },
      ],
    } as ScoreResult);

    expect(findings.map((finding) => finding.title)).toEqual(['A issue', 'Z issue']);
  });
});

describe('buildCustomVisualFindings (via buildNormalizedFindings)', () => {
  function visualMetadata(evidence: CustomVisualEvidence): PageVisualMetadataSummary {
    return {
      pageName: 'Overview',
      semanticColorMap: [],
      visualCount: 1,
      visibleTitleVisualCount: 0,
      textVisualCount: 0,
      slicerCount: 0,
      legendVisualCount: 0,
      axisLabelVisualCount: 0,
      dataLabelVisualCount: 0,
      formattedVisualCount: 0,
      visuals: [
        {
          visualId: 'v1',
          visualType: evidence.visualType,
          x: 0,
          y: 0,
          width: 100,
          height: 100,
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
          customVisualEvidence: evidence,
        },
      ],
    };
  }

  it('emits a finding for a Deneb visual missing a tooltip encoding', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'deneb',
        visualType: 'deneb7E15AEF80B9E4D4F8E12924291ECE89A',
        denebMarkType: 'line',
        denebHasTooltip: false,
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.detectionType).toBe('deterministic');
    expect(finding!.summary.toLowerCase()).toContain('tooltip');
  });

  it('emits a finding for a Deneb visual with an unparseable specification', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'deneb',
        visualType: 'deneb7E15AEF80B9E4D4F8E12924291ECE89A',
        denebSpecUnparseable: true,
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.title.toLowerCase()).toContain('unreadable specification');
    expect(finding!.summary.toLowerCase()).toContain('could not be parsed');
  });

  it('emits a finding for a Deneb visual authored in raw Vega', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'deneb',
        visualType: 'deneb7E15AEF80B9E4D4F8E12924291ECE89A',
        denebIsRawVegaProvider: true,
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.title.toLowerCase()).toContain('raw vega');
  });

  it('emits a Deneb finding with the no-gaps fallback wording when all structural checks pass', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'deneb',
        visualType: 'deneb7E15AEF80B9E4D4F8E12924291ECE89A',
        denebHasTooltip: true,
        denebHasLegend: true,
        denebHasAxisTitles: true,
        denebHasTitle: true,
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.summary.toLowerCase()).toContain('though its specification includes');
    expect(finding!.summary.toLowerCase()).not.toContain('structural gaps found');
  });

  it('emits a finding for an HTML Content visual with a scripted static template', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'htmlContent',
        visualType: 'htmlContent443BE3AD55E043BF878BED274D3A6865',
        htmlStaticTemplateHasScriptTag: true,
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.summary.toLowerCase()).toContain('script');
  });

  it('emits a finding for a dynamically bound HTML Content visual with static-template flags', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'htmlContent',
        visualType: 'htmlContent443BE3AD55E043BF878BED274D3A6865',
        htmlContentIsDynamicallyBound: true,
        htmlStaticTemplateHasExternalResource: true,
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.summary.toLowerCase()).toContain('bound to a measure or field');
    expect(finding!.summary.toLowerCase()).toContain('external resource');
  });

  it('emits a finding for a dynamically bound HTML Content visual with no static-template flags', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'htmlContent',
        visualType: 'htmlContent443BE3AD55E043BF878BED274D3A6865',
        htmlContentIsDynamicallyBound: true,
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.summary.toLowerCase()).toContain('bound to a measure or field');
    expect(finding!.summary.toLowerCase()).not.toContain('its static template contains');
  });

  it('emits a finding for a static HTML Content visual with the no-flagged-content fallback wording', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'htmlContent',
        visualType: 'htmlContent443BE3AD55E043BF878BED274D3A6865',
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.summary.toLowerCase()).toContain('no flagged content');
  });

  it('emits a generic not-analyzed finding for an unrecognized custom visual', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'genericCustom',
        visualType: 'PBI_CV_1234567890ABCDEF',
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.summary.toLowerCase()).toContain('not analyzed');
  });

  it('does not emit a custom-visual finding when no visual carries customVisualEvidence', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: {
        pageName: 'Overview',
        semanticColorMap: [],
        visualCount: 0,
        visibleTitleVisualCount: 0,
        textVisualCount: 0,
        slicerCount: 0,
        legendVisualCount: 0,
        axisLabelVisualCount: 0,
        dataLabelVisualCount: 0,
        formattedVisualCount: 0,
        visuals: [],
      },
    } as never);

    expect(findings.some((f) => f.evidence.some((e) => e.kind === 'customVisual'))).toBe(false);
  });

  it('emits custom-visual findings in full-report mode via pageScores', () => {
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
          feedback: {},
          recommendations: [],
          visualMetadata: visualMetadata({
            kind: 'deneb',
            visualType: 'deneb7E15AEF80B9E4D4F8E12924291ECE89A',
            denebMarkType: 'line',
            denebHasTooltip: false,
          }),
        },
      ],
    } as ScoreResult);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.affectedPages).toEqual(['Overview']);
    expect(finding!.summary.toLowerCase()).toContain('tooltip');
  });
});
