import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  buildFixWorkflowPayload,
  normalizeScoreResultPayload,
  SCORE_RESULT_OPTIONAL_FIELDS,
  SCORE_RESULT_REQUIRED_FIELDS,
} from '../views/scoreResultPayload';

function createMinimalScorePayload(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    GestaltScore: 84,
    CognitiveLoadScore: 72,
    DataInkScore: 80,
    AccessibilityScore: 70,
    VisualBestPracticesScore: 78,
    StephenFewScore: 66,
    EnterpriseGovernanceScore: 74,
    TufteScore: 68,
    GraphicalPerceptionScore: 70,
    DensityScore: 64,
    NarrativeScore: 69,
    CompositeScore: 77,
    Feedback: {},
    PageCount: 1,
    Recommendations: [],
    ReportPath: '/tmp/Sales.Report',
    ScoredAt: '2026-06-11T00:00:00.000Z',
    ...overrides,
  };
}

function readWorkspaceFile(relativePath: string): string {
  return fs.readFileSync(path.join(__dirname, '../../..', relativePath), 'utf8');
}

function extractCSharpAutoPropertyNames(source: string, className: string): string[] {
  const classIndex = source.indexOf(`public sealed class ${className}`);
  expect(classIndex).toBeGreaterThanOrEqual(0);

  const classText = source.slice(classIndex);
  const propertyPattern = /public\s+[A-Za-z0-9_<>,?.\[\]\s]+\s+([A-Za-z0-9_]+)\s*\{/g;
  const propertyNames = new Set<string>();
  let match: RegExpExecArray | null;

  while ((match = propertyPattern.exec(classText)) !== null) {
    propertyNames.add(match[1]);
  }

  return [...propertyNames];
}

describe('normalizeScoreResultPayload', () => {
  it('declares required and optional top-level score result fields explicitly', () => {
    expect(SCORE_RESULT_REQUIRED_FIELDS).toEqual([
      'gestaltScore',
      'cognitiveLoadScore',
      'dataInkScore',
      'accessibilityScore',
      'visualBestPracticesScore',
      'stephenFewScore',
      'enterpriseGovernanceScore',
      'tufteScore',
      'graphicalPerceptionScore',
      'densityScore',
      'narrativeScore',
      'compositeScore',
      'feedback',
      'pageCount',
      'recommendations',
      'reportPath',
      'scoredAt',
    ]);
    expect(SCORE_RESULT_OPTIONAL_FIELDS).toEqual([
      'actionabilityBreakdown',
      'benchmarkComparison',
      'dataVisualCount',
      'frameworkWeights',
      'guidedStoryImprovements',
      'governanceScore',
      'hiddenVisualCount',
      'inferredStorySummary',
      'layoutScore',
      'navigationVisualCount',
      'pageIntentProfile',
      'pageScores',
      'reportConsistencySummary',
      'scoredPageId',
      'scoredPageName',
      'scoringErrors',
      'themeScore',
      'visualMetadata',
    ]);
  });

  it('keeps the required top-level score fields aligned with the backend ScoreResult contract', () => {
    const csharpSource = readWorkspaceFile('service-dotnet/Services/Pbir/Models/ScoreResult.cs');
    const propertyNames = extractCSharpAutoPropertyNames(csharpSource, 'ScoreResult');

    for (const requiredField of SCORE_RESULT_REQUIRED_FIELDS) {
      const backendProperty = `${requiredField[0].toUpperCase()}${requiredField.slice(1)}`;
      expect(propertyNames).toContain(backendProperty);
    }
  });

  it('rejects missing required top-level score fields explicitly', () => {
    expect(() => normalizeScoreResultPayload(createMinimalScorePayload({
      CompositeScore: undefined,
    }))).toThrow("Missing required numeric field 'compositeScore'");
  });

  it('keeps optional top-level score fields backward compatible when they are absent', () => {
    const normalized = normalizeScoreResultPayload(createMinimalScorePayload());

    expect(normalized.pageScores).toBeUndefined();
    expect(normalized.actionabilityBreakdown).toBeUndefined();
    expect(normalized.benchmarkComparison).toBeUndefined();
    expect(normalized.visualMetadata).toBeUndefined();
    expect(normalized.reportConsistencySummary).toBeUndefined();
  });

  it('maps safe Guided Story Improvements fields and omits unsafe research-stage fields', () => {
    const normalized = normalizeScoreResultPayload({
      GestaltScore: 84,
      CognitiveLoadScore: 72,
      DataInkScore: 80,
      AccessibilityScore: 70,
      VisualBestPracticesScore: 78,
      StephenFewScore: 66,
      EnterpriseGovernanceScore: 74,
      TufteScore: 68,
      GraphicalPerceptionScore: 70,
      DensityScore: 64,
      NarrativeScore: 69,
      CompositeScore: 77,
      Feedback: {},
      PageCount: 1,
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-06-11T00:00:00.000Z',
      GuidedStoryImprovements: {
        HighPriorityImprovements: [
          {
            Id: 'story-missing-title',
            Title: 'Add a clearer page question or title',
            Summary: 'The page does not establish its main question early enough.',
            Rationale: 'Readers need a clear story anchor before interpreting the visuals.',
            ExpectedImpact: 'Stronger narrative scan path.',
            Priority: 'high',
            RelatedImpactArea: 'storytelling',
            ConfidenceBreakdown: {
              Accuracy: 'low',
            },
          },
        ],
        MediumPriorityImprovements: [
          {
            Id: 'story-scattered-filters',
            Title: 'Consolidate scattered filters',
            Summary: 'Filter controls are split across multiple zones.',
            Rationale: 'A single control zone creates a cleaner exploration entry point.',
            ExpectedImpact: 'Cleaner reading flow.',
            Priority: 'medium',
            RelatedImpactArea: 'storytelling',
            RawEvidenceIds: ['gap-1'],
          },
        ],
        StoryImprovementRationale: 'The page needs a clearer narrative frame before the visuals can do their job.',
        SignalRegistry: ['internal-only'],
      },
    });

    expect(normalized.guidedStoryImprovements).toEqual({
      highPriorityImprovements: [
        {
          id: 'story-missing-title',
          title: 'Add a clearer page question or title',
          summary: 'The page does not establish its main question early enough.',
          rationale: 'Readers need a clear story anchor before interpreting the visuals.',
          expectedImpact: 'Stronger narrative scan path.',
          priority: 'high',
          relatedImpactArea: 'storytelling',
        },
      ],
      mediumPriorityImprovements: [
        {
          id: 'story-scattered-filters',
          title: 'Consolidate scattered filters',
          summary: 'Filter controls are split across multiple zones.',
          rationale: 'A single control zone creates a cleaner exploration entry point.',
          expectedImpact: 'Cleaner reading flow.',
          priority: 'medium',
          relatedImpactArea: 'storytelling',
        },
      ],
      storyImprovementRationale: 'The page needs a clearer narrative frame before the visuals can do their job.',
    });
    expect(JSON.stringify(normalized.guidedStoryImprovements)).not.toContain('ConfidenceBreakdown');
    expect(JSON.stringify(normalized.guidedStoryImprovements)).not.toContain('RawEvidenceIds');
    expect(JSON.stringify(normalized.guidedStoryImprovements)).not.toContain('SignalRegistry');
  });

  it('keeps older payloads without Guided Story Improvements backward compatible', () => {
    const normalized = normalizeScoreResultPayload({
      GestaltScore: 84,
      CognitiveLoadScore: 72,
      DataInkScore: 80,
      AccessibilityScore: 70,
      VisualBestPracticesScore: 78,
      StephenFewScore: 66,
      EnterpriseGovernanceScore: 74,
      TufteScore: 68,
      GraphicalPerceptionScore: 70,
      DensityScore: 64,
      NarrativeScore: 69,
      CompositeScore: 77,
      Feedback: {},
      PageCount: 1,
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-06-11T00:00:00.000Z',
    });

    expect(normalized.guidedStoryImprovements).toBeUndefined();
    expect(normalized.compositeScore).toBe(77);
  });

  it('uses Guided Story Improvements as the story-finding source of truth without duplicate inflation', () => {
    const normalized = normalizeScoreResultPayload({
      GestaltScore: 84,
      CognitiveLoadScore: 72,
      DataInkScore: 80,
      AccessibilityScore: 70,
      VisualBestPracticesScore: 78,
      StephenFewScore: 66,
      EnterpriseGovernanceScore: 74,
      TufteScore: 68,
      GraphicalPerceptionScore: 70,
      DensityScore: 64,
      NarrativeScore: 69,
      CompositeScore: 77,
      Feedback: {},
      PageCount: 1,
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredPageName: 'Overview',
      ScoredAt: '2026-06-11T00:00:00.000Z',
      ActionabilityBreakdown: {
        Score: 52,
        TargetBenchmarkPresent: false,
        ExceptionVisibility: false,
        UrgencySignaling: false,
        PriorPeriodContext: false,
        DrillPathPresent: true,
        ExpectationLevel: 'high',
        Strengths: [],
        Gaps: ['Add a target or benchmark.'],
        Summary: 'The page still lacks decision context.',
      },
      BenchmarkComparison: {
        Archetype: 'executive scorecard',
        BenchmarkLabel: 'Executive-ready benchmark',
        ComparativePosition: 'below',
        BeautifulButUseless: false,
        Insight: 'The page is below the benchmark because it lacks comparison context.',
        Strengths: [],
        Gaps: ['Add a target or benchmark.'],
      },
      GuidedStoryImprovements: {
        HighPriorityImprovements: [
          {
            Id: 'missing-benchmark-target',
            Title: 'Add a benchmark or target',
            Summary: 'The current result appears without a visible target, budget, or benchmark for comparison.',
            Rationale: 'Readers need an explicit reference point to judge the result.',
            ExpectedImpact: 'Clearer decision context around the headline numbers.',
            Priority: 'high',
            RelatedImpactArea: 'benchmark',
          },
        ],
        MediumPriorityImprovements: [],
        StoryImprovementRationale: 'The page has a story, but its decision frame is still weak.',
      },
    });

    const storyFindings = normalized.normalizedFindings?.filter((finding) => finding.sourceKind === 'guidedStoryImprovement') ?? [];
    expect(storyFindings).toHaveLength(1);
    expect(storyFindings[0]).toMatchObject({
      title: 'Add a benchmark or target',
      impactArea: 'benchmark',
      severity: 'high',
    });
    expect(normalized.normalizedFindings?.some((finding) => finding.sourceKind === 'actionability')).toBe(false);
    expect(normalized.normalizedFindings?.some((finding) => finding.sourceKind === 'benchmark')).toBe(false);
  });

  it('maps PascalCase score payloads to the webview contract', () => {
    const normalized = normalizeScoreResultPayload({
      GestaltScore: 84,
      CognitiveLoadScore: 72,
      DataInkScore: 80,
      AccessibilityScore: 70,
      VisualBestPracticesScore: 78,
      StephenFewScore: 66,
      EnterpriseGovernanceScore: 74,
      TufteScore: 68,
      GraphicalPerceptionScore: 70,
      DensityScore: 64,
      NarrativeScore: 69,
      CompositeScore: 77,
      Feedback: {
        gestalt: [{ Ok: true, Text: 'Grid alignment: Strong alignment.', FindingType: 'objective', EarnedPoints: 35, PossiblePoints: 35 }],
      },
      PageCount: 2,
      Recommendations: ['[High] Layout: Snap visuals to grid'],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-05-02T20:00:00.000Z',
      DataVisualCount: 12,
      NavigationVisualCount: 4,
      HiddenVisualCount: 1,
      InferredStorySummary: {
        IntentProfile: 'executiveOverview',
        StoryArchetype: 'executive overview + trend + comparison',
        InferredStory: 'This page appears to summarize Revenue performance over time, with region comparison as supporting evidence.',
        Confidence: 'high',
        Evidence: ['Visible title: Sales Overview', '2 KPI cards in the top scan path'],
      },
      PageIntentProfile: {
        InferredProfile: 'executive',
        ActionabilityExpectation: 'high',
        ReviewGuidance: ['Executive pages should expose the target quickly.'],
        Evidence: ['2 KPI cards detected'],
      },
      ActionabilityBreakdown: {
        Score: 60,
        TargetBenchmarkPresent: true,
        ExceptionVisibility: false,
        UrgencySignaling: false,
        PriorPeriodContext: true,
        DrillPathPresent: true,
        ExpectationLevel: 'high',
        Strengths: ['Prior-period context is visible.'],
        Gaps: ['Exception visibility is weak.'],
        Summary: 'The page includes some decision context but still hides the main exception.',
      },
      BenchmarkComparison: {
        Archetype: 'executive scorecard',
        BenchmarkLabel: 'Executive-ready benchmark',
        ComparativePosition: 'mixed',
        BeautifulButUseless: false,
        Insight: 'The page is readable, but exception visibility is still weaker than the benchmark.',
        Strengths: ['Clear KPI band'],
        Gaps: ['Weak exception callout'],
      },
      VisualMetadata: {
        PageName: 'Overview',
        VisiblePageTitle: 'Sales Overview',
        SemanticColorMap: [
          {
            SemanticKey: 'region:north',
            DisplayLabel: 'North',
            Color: '#3366CC',
            SourceVisualId: 'v1',
            SourcePageName: 'Overview',
          },
        ],
        ChartIntentSummary: {
          Intent: 'comparison',
          Confidence: 'high',
          Evidence: ['bar chart', 'category axis'],
          FitStatus: 'good',
          RecommendedAlternatives: ['columnChart'],
        },
        CanvasWidth: 1280,
        CanvasHeight: 720,
        VisualCount: 2,
        VisibleTitleVisualCount: 1,
        TextVisualCount: 0,
        SlicerCount: 1,
        LegendVisualCount: 1,
        AxisLabelVisualCount: 1,
        DataLabelVisualCount: 0,
        FormattedVisualCount: 1,
        Visuals: [
          {
            VisualId: 'v1',
            VisualType: 'barChart',
            X: 0,
            Y: 0,
            Width: 320,
            Height: 180,
            IsHidden: false,
            IsNavigationElement: false,
            IsDecorative: false,
            IsSlicer: false,
            VisibleTitleText: 'Sales Overview',
            BestVisibleText: 'Sales Overview',
            HasVisibleTitleIntent: true,
            HasLegend: true,
            HasAxisLabels: true,
            HasDataLabels: false,
            CategoryHints: ['Region'],
            ValueHints: ['Revenue'],
            SeriesHints: [],
            MeasureHints: ['Revenue'],
            BackgroundFillColor: '#FFFFFF',
            FontColor: '#111111',
            HasBorder: true,
            CornerRadius: 8,
            HasShadow: false,
            SemanticColors: [
              {
                SemanticKey: 'region:north',
                DisplayLabel: 'North',
                Color: '#3366CC',
                SourceVisualId: 'v1',
                SourcePageName: 'Overview',
              },
            ],
            ChartIntent: {
              Intent: 'comparison',
              Confidence: 'high',
              Evidence: ['bar chart'],
              FitStatus: 'good',
              RecommendedAlternatives: [],
            },
          },
        ],
      },
      ReportConsistencySummary: {
        ConsistentTitleAnchors: true,
        ConsistentFilterBand: true,
        ConsistentMetricLabels: false,
        ConsistentSemanticColors: true,
        OverallFinding: '2 cross-page consistency issue(s) detected across layout, metricGovernance.',
        AffectedPages: ['Overview', 'Finance Detail'],
        IssueCount: 2,
        Issues: [
          {
            Category: 'layout',
            IssueCategory: 'layoutPattern',
            OverallFinding: 'Overview Detail breaks from the dominant layout pattern.',
            AffectedPages: ['Overview Detail'],
            Severity: 'medium',
            Confidence: 'high',
            RecommendedRemediation: 'Keep repeated pages on the dominant layout pattern.',
          },
          {
            Category: 'metricGovernance',
            IssueCategory: 'metricLabels',
            OverallFinding: 'KPI label naming drifts across pages.',
            AffectedPages: ['Overview', 'Finance Detail'],
            Severity: 'low',
            Confidence: 'medium',
            RecommendedRemediation: 'Use one canonical KPI label such as Current Year Sales.',
          },
        ],
        Findings: ['Metric labels drift across overview pages.'],
      },
      FrameworkWeights: {
        gestalt: 60,
        cognitiveLoad: 40,
      },
      PageScores: [
        {
          PageName: 'Overview',
          GestaltScore: 82,
          CognitiveLoadScore: 70,
          DataInkScore: 79,
          AccessibilityScore: 70,
          VisualBestPracticesScore: 77,
          StephenFewScore: 65,
          EnterpriseGovernanceScore: 73,
          TufteScore: 68,
          GraphicalPerceptionScore: 69,
          DensityScore: 63,
          NarrativeScore: 67,
          CompositeScore: 75,
          Feedback: {
            gestalt: [{ Ok: true, Text: 'Grid alignment: Overview grid is aligned.', FindingType: 'stylePreference', EarnedPoints: 35, PossiblePoints: 35 }],
          },
          Recommendations: ['[High] Layout: Snap visuals to grid'],
          ReportConsistencyNotes: ['Metric labels drift across overview pages.'],
          InferredStorySummary: {
            IntentProfile: 'executiveOverview',
            StoryArchetype: 'executive overview + trend + comparison',
            InferredStory: 'This page appears to summarize Sales performance over time, with region comparison as supporting evidence.',
            Confidence: 'high',
            Evidence: ['Visible title: Sales Overview', '2 KPI cards in the top scan path'],
          },
          PageIntentProfile: {
            InferredProfile: 'executive',
            ActionabilityExpectation: 'high',
            ReviewGuidance: ['Executive pages should expose the target quickly.'],
            Evidence: ['2 KPI cards detected'],
          },
          ActionabilityBreakdown: {
            Score: 60,
            TargetBenchmarkPresent: true,
            ExceptionVisibility: false,
            UrgencySignaling: false,
            PriorPeriodContext: true,
            DrillPathPresent: true,
            ExpectationLevel: 'high',
            Strengths: ['Prior-period context is visible.'],
            Gaps: ['Exception visibility is weak.'],
            Summary: 'The page includes some decision context but still hides the main exception.',
          },
          BenchmarkComparison: {
            Archetype: 'executive scorecard',
            BenchmarkLabel: 'Executive-ready benchmark',
            ComparativePosition: 'mixed',
            BeautifulButUseless: false,
            Insight: 'The page is readable, but exception visibility is still weaker than the benchmark.',
            Strengths: ['Clear KPI band'],
            Gaps: ['Weak exception callout'],
          },
          FrameworkWeights: {
            gestalt: 60,
            cognitiveLoad: 40,
          },
          VisualMetadata: {
            PageName: 'Overview',
            VisiblePageTitle: 'Sales Overview',
            SemanticColorMap: [],
            ChartIntentSummary: {
              Intent: 'comparison',
              Confidence: 'medium',
              Evidence: ['bar chart'],
              FitStatus: 'good',
              RecommendedAlternatives: [],
            },
            VisualCount: 1,
            VisibleTitleVisualCount: 1,
            TextVisualCount: 0,
            SlicerCount: 0,
            LegendVisualCount: 1,
            AxisLabelVisualCount: 1,
            DataLabelVisualCount: 0,
            FormattedVisualCount: 1,
            Visuals: [],
          },
        },
      ],
      ScoringErrors: {
        Intro: 'Hidden visual parse failed.',
      },
    });

    expect(normalized.recommendations).toEqual(['[High] Layout: Snap visuals to grid']);
    expect(normalized.frameworkWeights).toEqual({
      gestalt: 60,
      cognitiveLoad: 40,
    });
    expect(normalized.feedback.gestalt).toEqual([{
      ok: true,
      text: 'Grid alignment: Strong alignment.',
      findingType: 'objective',
      earnedPoints: 35,
      possiblePoints: 35,
    }]);
    expect(normalized.pageScores?.[0].pageName).toBe('Overview');
    expect(normalized.pageScores?.[0].feedback.gestalt).toEqual([{
      ok: true,
      text: 'Grid alignment: Overview grid is aligned.',
      findingType: 'stylePreference',
      earnedPoints: 35,
      possiblePoints: 35,
    }]);
    expect(normalized.pageScores?.[0].recommendations).toEqual(['[High] Layout: Snap visuals to grid']);
    expect(normalized.pageScores?.[0].reportConsistencyNotes).toEqual(['Metric labels drift across overview pages.']);
    expect(normalized.pageScores?.[0].inferredStorySummary).toEqual({
      intentProfile: 'executiveOverview',
      storyArchetype: 'executive overview + trend + comparison',
      inferredStory: 'This page appears to summarize Sales performance over time, with region comparison as supporting evidence.',
      confidence: 'high',
      evidence: ['Visible title: Sales Overview', '2 KPI cards in the top scan path'],
    });
    expect(normalized.pageScores?.[0].pageIntentProfile?.inferredProfile).toBe('executive');
    expect(normalized.pageScores?.[0].actionabilityBreakdown?.score).toBe(60);
    expect(normalized.pageScores?.[0].benchmarkComparison?.archetype).toBe('executive scorecard');
    expect(normalized.normalizedFindings).toBeDefined();
    expect(normalized.normalizedFindings?.length).toBeGreaterThan(0);
    expect(normalized.normalizedFindings?.[0]).toEqual(
      expect.objectContaining({
        severity: expect.any(String),
        confidence: expect.any(Number),
        scope: expect.any(String),
        detectionType: expect.any(String),
        affectedPages: expect.any(Array),
        impactArea: expect.any(String),
        frameworkImpact: expect.any(Array),
        recommendation: expect.any(String),
      }),
    );
    expect(normalized.overviewSummary).toMatchObject({
      maturityBand: expect.any(String),
      riskBand: expect.any(String),
      severityDistribution: {
        high: expect.any(Number),
        medium: expect.any(Number),
        low: expect.any(Number),
        info: expect.any(Number),
      },
      topActions: expect.arrayContaining([
        expect.objectContaining({
          sourceFindingIds: expect.any(Array),
        }),
      ]),
    });
    expect((normalized as unknown as Record<string, unknown>).pagePurposeAnalysis).toMatchObject({
      inferredPurpose: 'Executive',
      confidence: 'high',
      actionabilityScore: 60,
      benchmarkStatus: expect.any(String),
      whyThisMatters: expect.any(String),
    });
    expect((normalized.pageScores?.[0] as unknown as Record<string, unknown>)?.pagePurposeAnalysis).toMatchObject({
      inferredPurpose: 'Executive',
      confidence: 'high',
      actionabilityScore: 60,
      whyThisMatters: expect.any(String),
    });
    expect(normalized.fixPlan).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          severity: expect.any(String),
          effort: expect.any(String),
          impact: expect.any(String),
          why: expect.any(String),
          resolvedOutcomes: expect.any(Array),
          sourceFindingIds: expect.any(Array),
        }),
      ]),
    );
    expect(normalized.personaPresentation).toMatchObject({
      activePersona: 'default',
      availablePersonas: expect.arrayContaining([
        expect.objectContaining({
          id: 'executive',
          emphasizedImpactAreas: expect.any(Array),
          overviewEmphasis: expect.any(Array),
        }),
        expect.objectContaining({
          id: 'accessibility',
          defaultSeverityFilter: expect.any(Array),
        }),
      ]),
    });
    expect(normalized.crossPageMatrix).toBeUndefined();
    expect(normalized.pageScores?.[0].frameworkWeights).toEqual({
      gestalt: 60,
      cognitiveLoad: 40,
    });
    expect(normalized.reportConsistencySummary).toMatchObject({
      overallFinding: '2 cross-page consistency issue(s) detected across layout, metricGovernance.',
      affectedPages: ['Overview', 'Finance Detail'],
      issueCount: 2,
      issues: [
        expect.objectContaining({
          category: 'layout',
          issueCategory: 'layoutPattern',
          severity: 'medium',
          confidence: 'high',
        }),
        expect.objectContaining({
          category: 'metricGovernance',
          issueCategory: 'metricLabels',
          severity: 'low',
          confidence: 'medium',
        }),
      ],
    });
    expect(normalized.visualMetadata?.visiblePageTitle).toBe('Sales Overview');
    expect(normalized.visualMetadata?.visuals[0]).toMatchObject({
      visualId: 'v1',
      visualType: 'barChart',
        hasLegend: true,
        categoryHints: ['Region'],
        hasBorder: true,
        semanticColors: [
          {
            semanticKey: 'region:north',
            displayLabel: 'North',
            color: '#3366CC',
            sourceVisualId: 'v1',
            sourcePageName: 'Overview',
          },
        ],
        chartIntent: {
          intent: 'comparison',
          confidence: 'high',
          evidence: ['bar chart'],
          fitStatus: 'good',
          recommendedAlternatives: [],
        },
      });
    expect(normalized.visualMetadata?.semanticColorMap).toEqual([
      {
        semanticKey: 'region:north',
        displayLabel: 'North',
        color: '#3366CC',
        sourceVisualId: 'v1',
        sourcePageName: 'Overview',
      },
    ]);
    expect(normalized.visualMetadata?.chartIntentSummary).toEqual({
      intent: 'comparison',
      confidence: 'high',
      evidence: ['bar chart', 'category axis'],
      fitStatus: 'good',
      recommendedAlternatives: ['columnChart'],
    });
    expect(normalized.pageScores?.[0].visualMetadata?.visiblePageTitle).toBe('Sales Overview');
    expect(normalized.pageScores?.[0].visualMetadata?.chartIntentSummary).toEqual({
      intent: 'comparison',
      confidence: 'medium',
      evidence: ['bar chart'],
      fitStatus: 'good',
      recommendedAlternatives: [],
    });
    expect(normalized.reportConsistencySummary).toMatchObject({
      consistentTitleAnchors: true,
      consistentFilterBand: true,
      consistentMetricLabels: false,
      consistentSemanticColors: true,
      findings: ['Metric labels drift across overview pages.'],
    });
    expect(normalized.inferredStorySummary).toEqual({
      intentProfile: 'executiveOverview',
      storyArchetype: 'executive overview + trend + comparison',
      inferredStory: 'This page appears to summarize Revenue performance over time, with region comparison as supporting evidence.',
      confidence: 'high',
      evidence: ['Visible title: Sales Overview', '2 KPI cards in the top scan path'],
    });
    expect(normalized.pageIntentProfile?.inferredProfile).toBe('executive');
    expect(normalized.actionabilityBreakdown?.summary).toContain('decision context');
    expect(normalized.benchmarkComparison?.comparativePosition).toBe('mixed');
    expect(normalized.scoringErrors).toEqual({
      Intro: 'Hidden visual parse failed.',
    });
  });

  it('adds analysis context and Fabric App readiness assessment for PBIR surfaces', () => {
    const normalized = normalizeScoreResultPayload({
      CompositeScore: 77,
      GestaltScore: 84,
      CognitiveLoadScore: 72,
      DataInkScore: 80,
      AccessibilityScore: 70,
      VisualBestPracticesScore: 78,
      StephenFewScore: 66,
      EnterpriseGovernanceScore: 74,
      TufteScore: 68,
      GraphicalPerceptionScore: 70,
      DensityScore: 64,
      NarrativeScore: 69,
      Feedback: {},
      PageCount: 1,
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-06-03T21:00:00.000Z',
      PageScores: [
        {
          PageName: 'Executive Overview',
          GestaltScore: 84,
          CognitiveLoadScore: 78,
          DataInkScore: 79,
          AccessibilityScore: 82,
          VisualBestPracticesScore: 83,
          StephenFewScore: 76,
          EnterpriseGovernanceScore: 78,
          TufteScore: 72,
          GraphicalPerceptionScore: 75,
          DensityScore: 74,
          NarrativeScore: 81,
          CompositeScore: 80,
          Feedback: {},
          Recommendations: [],
          ActionabilityBreakdown: {
            Score: 76,
            TargetBenchmarkPresent: true,
            ExceptionVisibility: true,
            UrgencySignaling: true,
            PriorPeriodContext: true,
            DrillPathPresent: true,
            ExpectationLevel: 'high',
            Strengths: ['Benchmarks are visible.'],
            Gaps: [],
            Summary: 'The page exposes a clear decision path with target and prior-period context.',
          },
          PageIntentProfile: {
            InferredProfile: 'executive',
            ActionabilityExpectation: 'high',
            ReviewGuidance: ['Keep the decision path prominent.'],
            Evidence: ['Executive KPI band'],
          },
          VisualMetadata: {
            PageName: 'Executive Overview',
            VisiblePageTitle: 'Executive Overview',
            SemanticColorMap: [],
            VisualCount: 5,
            VisibleTitleVisualCount: 1,
            TextVisualCount: 1,
            SlicerCount: 1,
            LegendVisualCount: 1,
            AxisLabelVisualCount: 1,
            DataLabelVisualCount: 1,
            FormattedVisualCount: 5,
            Visuals: [],
          },
        },
      ],
    });

    expect(normalized.analysisContext).toMatchObject({
      surfaceType: 'pbirReport',
      analyzerType: 'fabricAppReadiness',
      analyzerProfile: 'migrationReadiness',
    });
    expect(normalized.readinessAssessment).toMatchObject({
      readinessBand: 'strongCandidate',
      candidatePages: ['Executive Overview'],
    });
    expect(normalized.overviewSummary?.readinessSummary).toMatchObject({
      candidatePageCount: 1,
    });
    expect(normalized.normalizedFindings?.some((finding) => finding.sourceKind === 'fabricAppReadiness')).toBe(true);
  });

  it('defaults missing or invalid finding types to strongHeuristic', () => {
    const normalized = normalizeScoreResultPayload({
      GestaltScore: 72,
      CognitiveLoadScore: 72,
      DataInkScore: 72,
      AccessibilityScore: 72,
      VisualBestPracticesScore: 72,
      StephenFewScore: 72,
      EnterpriseGovernanceScore: 72,
      TufteScore: 72,
      GraphicalPerceptionScore: 72,
      DensityScore: 72,
      NarrativeScore: 72,
      CompositeScore: 72,
      Feedback: {
        gestalt: [
          { Ok: false, Text: 'Grid alignment: Off-grid visual detected.' },
          { Ok: false, Text: 'Similarity: Visual mix is noisy.', FindingType: 'not-real' },
        ],
      },
      PageCount: 1,
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-06-02T20:00:00.000Z',
    });

    expect(normalized.feedback.gestalt).toEqual([
      {
        ok: false,
        text: 'Grid alignment: Off-grid visual detected.',
        findingType: 'strongHeuristic',
      },
      {
        ok: false,
        text: 'Similarity: Visual mix is noisy.',
        findingType: 'strongHeuristic',
      },
    ]);
  });

  it('initializes proposal enrichment state without changing deterministic score semantics', () => {
    const normalized = normalizeScoreResultPayload({
      GestaltScore: 84,
      CognitiveLoadScore: 72,
      DataInkScore: 72,
      AccessibilityScore: 72,
      VisualBestPracticesScore: 72,
      StephenFewScore: 72,
      EnterpriseGovernanceScore: 72,
      TufteScore: 72,
      GraphicalPerceptionScore: 72,
      DensityScore: 72,
      NarrativeScore: 72,
      CompositeScore: 77,
      PageCount: 1,
      Feedback: {},
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-06-02T20:00:00.000Z',
      PageScores: [],
    });

    expect(normalized.proposalEnrichments).toEqual([]);
    expect(normalized.compositeScore).toBe(77);
    expect(normalized.fixPlan).toEqual(expect.any(Array));
  });

  it('drops partial report consistency summaries and malformed nested semantic metadata', () => {
    const normalized = normalizeScoreResultPayload({
      GestaltScore: 72,
      CognitiveLoadScore: 72,
      DataInkScore: 72,
      AccessibilityScore: 72,
      VisualBestPracticesScore: 72,
      StephenFewScore: 72,
      EnterpriseGovernanceScore: 72,
      TufteScore: 72,
      GraphicalPerceptionScore: 72,
      DensityScore: 72,
      NarrativeScore: 72,
      CompositeScore: 72,
      Feedback: {},
      PageCount: 1,
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-06-02T20:00:00.000Z',
      VisualMetadata: {
        PageName: 'Overview',
        VisualCount: 1,
        VisibleTitleVisualCount: 0,
        TextVisualCount: 0,
        SlicerCount: 0,
        LegendVisualCount: 0,
        AxisLabelVisualCount: 0,
        DataLabelVisualCount: 0,
        FormattedVisualCount: 0,
        SemanticColorMap: [
          {
            SemanticKey: 'region:north',
            SourceVisualId: 'v1',
            SourcePageName: 'Overview',
          },
        ],
        ChartIntentSummary: {
          Intent: 'comparison',
          Confidence: 'very-high',
          Evidence: ['bar chart'],
          FitStatus: 'good',
          RecommendedAlternatives: ['columnChart'],
        },
        Visuals: [
          {
            VisualId: 'v1',
            VisualType: 'barChart',
            X: 0,
            Y: 0,
            Width: 320,
            Height: 180,
            IsHidden: false,
            IsNavigationElement: false,
            IsDecorative: false,
            IsSlicer: false,
            HasVisibleTitleIntent: false,
            CategoryHints: [],
            ValueHints: [],
            SeriesHints: [],
            MeasureHints: [],
            SemanticColors: [
              {
                SemanticKey: 'region:north',
                Color: '#3366CC',
                SourcePageName: 'Overview',
              },
            ],
            ChartIntent: {
              Intent: 'comparison',
              Confidence: 'invalid',
              Evidence: ['bar chart'],
              FitStatus: 'good',
              RecommendedAlternatives: [],
            },
          },
        ],
      },
      ReportConsistencySummary: {
        ConsistentTitleAnchors: true,
        Findings: ['Incomplete payload should be ignored.'],
      },
    });

    expect(normalized.reportConsistencySummary).toBeUndefined();
    expect(normalized.visualMetadata?.semanticColorMap).toEqual([]);
    expect(normalized.visualMetadata?.chartIntentSummary).toEqual({
      intent: 'comparison',
      confidence: undefined,
      evidence: ['bar chart'],
      fitStatus: 'good',
      recommendedAlternatives: ['columnChart'],
    });
    expect(normalized.visualMetadata?.visuals[0].semanticColors).toEqual([]);
    expect(normalized.visualMetadata?.visuals[0].chartIntent).toEqual({
      intent: 'comparison',
      confidence: undefined,
      evidence: ['bar chart'],
      fitStatus: 'good',
      recommendedAlternatives: [],
    });
  });
  it('builds grouped fix workflow payload state for selected opportunities and sessions', () => {
    const payload = buildFixWorkflowPayload({
      opportunities: [
        {
          id: 'fix-a',
          remediationItemId: 'rem-a',
          title: 'Fix A',
          category: 'alignment',
          summary: 'Summary',
          confidence: 95,
          safetyClass: 'safe',
          affectedPages: ['Overview'],
          targetObjectIds: ['visual-1'],
          sourceFindingIds: ['finding-a'],
          expectedResolutions: ['Layout consistency'],
          mutations: [{
            id: 'mutation-a',
            pageName: 'Overview',
            targetObjectId: 'visual-1',
            targetFile: '/tmp/a.json',
            propertyPath: 'position.x',
            mutationType: 'setPosition',
            before: 12,
            after: 24,
          }],
          previewRows: [{
            pageName: 'Overview',
            objectId: 'visual-1',
            property: 'position.x',
            before: 12,
            after: 24,
          }],
          rollbackPlan: {
            id: 'rollback-a',
            fixOpportunityId: 'fix-a',
            fileBackups: [{ targetFile: '/tmp/a.json', beforeContent: '{}' }],
            reverseMutations: [],
          },
          state: 'Previewed',
        },
      ],
      selectedOpportunityIds: ['fix-a'],
      approvalState: 'Previewed',
      message: 'Preview ready',
      fixApplySessions: [{
        id: 'session-1',
        appliedAt: '2026-06-01T22:40:00.000Z',
        opportunityIds: ['fix-a'],
        opportunityTitles: ['Fix A'],
        rollbackAvailable: true,
        rollbackHistory: [],
      }],
    });

    expect(payload.fixSelection).toMatchObject({
      selectedOpportunityIds: ['fix-a'],
      approvalState: 'Previewed',
      message: 'Preview ready',
      groupedPreview: {
        opportunityIds: ['fix-a'],
      },
    });
    expect(payload.fixSelection.compatibility.blockingReasons).toEqual([]);
    expect(payload.fixApplySessions).toHaveLength(1);
  });

  it('preserves local Fabric App review findings and evidence when a Fabric App surface is detected', () => {
    const repoRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'fabric-score-payload-'));
    fs.mkdirSync(path.join(repoRoot, 'src', 'routes'), { recursive: true });
    fs.writeFileSync(path.join(repoRoot, 'package.json'), JSON.stringify({ name: 'executive-fabric-app' }));
    fs.writeFileSync(path.join(repoRoot, 'src', 'ExecutiveDashboard.tsx'), `
      export function ExecutiveDashboard() {
        return <DashboardLayout><KpiCard /></DashboardLayout>;
      }
    `);
    fs.writeFileSync(path.join(repoRoot, 'src', 'routes', 'index.tsx'), `
      export const routes = [{ path: '/overview', label: 'Executive Overview' }];
    `);

    try {
      const normalized = normalizeScoreResultPayload({
        GestaltScore: 72,
        CognitiveLoadScore: 72,
        DataInkScore: 72,
        AccessibilityScore: 72,
        VisualBestPracticesScore: 72,
        StephenFewScore: 72,
        EnterpriseGovernanceScore: 72,
        TufteScore: 72,
        GraphicalPerceptionScore: 72,
        DensityScore: 72,
        NarrativeScore: 72,
        CompositeScore: 72,
        Feedback: {},
        PageCount: 2,
        Recommendations: [],
        ReportPath: repoRoot,
        ScoredAt: '2026-06-04T21:00:00.000Z',
        NormalizedFindings: [
          {
            Id: 'fabric-route-clarity',
            Title: 'Route labeling is too generic for analytical navigation',
            Summary: 'Generic labels weaken evidence flow.',
            Severity: 'medium',
            Confidence: 82,
            Scope: 'report',
            DetectionType: 'deterministic',
            AffectedPages: [],
            ImpactArea: 'navigation',
            FrameworkImpact: ['Fabric App Review'],
            Recommendation: 'Rename generic routes.',
            SourceKind: 'fabricAppReview',
            SourceSection: 'issues',
            Evidence: [
              {
                Kind: 'navigation',
                Label: 'Navigation evidence',
                Detail: 'src/routes/index.tsx — Detail -> /detail',
                FilePath: 'src/routes/index.tsx',
              },
            ],
          },
        ],
        FabricAppReview: {
          QualityScore: 72,
          Summary: 'Fabric App review produced bounded findings.',
          RemediationGuidance: ['Rename generic routes.'],
          Evidence: [
            {
              Kind: 'navigation',
              Label: 'Navigation evidence',
              Summary: 'Executive Overview -> /overview',
              FilePath: 'src/routes/index.tsx',
            },
          ],
        },
      });

      expect(normalized.analysisContext).toMatchObject({
        surfaceType: 'fabricApp',
        analyzerType: 'fabricAppReview',
        analyzerProfile: 'fabricAppQuality',
      });
      expect(normalized.fabricAppReview).toMatchObject({
        qualityScore: 72,
        summary: 'Fabric App review produced bounded findings.',
      });
      expect(normalized.normalizedFindings?.some((finding) => finding.sourceKind === 'fabricAppReview')).toBe(true);
      expect(normalized.fixPlan?.[0]).toMatchObject({
        title: expect.stringMatching(/navigation|route/i),
      });
    } finally {
      fs.rmSync(repoRoot, { recursive: true, force: true });
    }
  });

  it('fails explicitly when a required numeric field is missing', () => {
    expect(() => normalizeScoreResultPayload({
      CognitiveLoadScore: 72,
      DataInkScore: 80,
      AccessibilityScore: 70,
      VisualBestPracticesScore: 78,
      StephenFewScore: 66,
      EnterpriseGovernanceScore: 74,
      TufteScore: 68,
      GraphicalPerceptionScore: 70,
      DensityScore: 64,
      NarrativeScore: 69,
      CompositeScore: 77,
      Feedback: {},
      PageCount: 1,
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-06-11T00:00:00.000Z',
    })).toThrow("Missing required numeric field 'gestaltScore'");
  });

  it('fails explicitly when a required boolean field is missing inside a provided nested structure', () => {
    expect(() => normalizeScoreResultPayload({
      GestaltScore: 84,
      CognitiveLoadScore: 72,
      DataInkScore: 80,
      AccessibilityScore: 70,
      VisualBestPracticesScore: 78,
      StephenFewScore: 66,
      EnterpriseGovernanceScore: 74,
      TufteScore: 68,
      GraphicalPerceptionScore: 70,
      DensityScore: 64,
      NarrativeScore: 69,
      CompositeScore: 77,
      Feedback: {},
      PageCount: 1,
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-06-11T00:00:00.000Z',
      ActionabilityBreakdown: {
        Score: 52,
        ExceptionVisibility: false,
        UrgencySignaling: false,
        PriorPeriodContext: false,
        DrillPathPresent: true,
        ExpectationLevel: 'high',
        Strengths: [],
        Gaps: [],
        Summary: 'Missing target benchmark field should be rejected.',
      },
    })).toThrow("Missing required boolean field 'actionabilityBreakdown.targetBenchmarkPresent'");
  });

  it('fails explicitly when a required field is renamed instead of silently defaulting', () => {
    expect(() => normalizeScoreResultPayload({
      GestaltScore: 84,
      CognitiveLoadScore: 72,
      DataInkScore: 80,
      AccessibilityScore: 70,
      VisualBestPracticesScore: 78,
      StephenFewScore: 66,
      EnterpriseGovernanceScore: 74,
      TufteScore: 68,
      GraphicalPerceptionScore: 70,
      DensityScore: 64,
      NarrativeScore: 69,
      Composite_SCORE: 77,
      Feedback: {},
      PageCount: 1,
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-06-11T00:00:00.000Z',
    })).toThrow("Missing required numeric field 'compositeScore'");
  });

  it('fails explicitly when a provided nested structure is malformed', () => {
    expect(() => normalizeScoreResultPayload({
      GestaltScore: 84,
      CognitiveLoadScore: 72,
      DataInkScore: 80,
      AccessibilityScore: 70,
      VisualBestPracticesScore: 78,
      StephenFewScore: 66,
      EnterpriseGovernanceScore: 74,
      TufteScore: 68,
      GraphicalPerceptionScore: 70,
      DensityScore: 64,
      NarrativeScore: 69,
      CompositeScore: 77,
      Feedback: {},
      PageCount: 1,
      Recommendations: [],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-06-11T00:00:00.000Z',
      PageScores: [
        {
          PageName: 'Overview',
          GestaltScore: 82,
          CognitiveLoadScore: 70,
          DataInkScore: 79,
          AccessibilityScore: 70,
          VisualBestPracticesScore: 77,
          StephenFewScore: 65,
          EnterpriseGovernanceScore: 73,
          TufteScore: 68,
          GraphicalPerceptionScore: 69,
          DensityScore: 63,
          NarrativeScore: 67,
          CompositeScore: 75,
          Feedback: {},
          Recommendations: [],
          VisualMetadata: {
            PageName: 'Overview',
            VisualCount: 1,
            VisibleTitleVisualCount: 1,
            TextVisualCount: 0,
            SlicerCount: 0,
            LegendVisualCount: 1,
            AxisLabelVisualCount: 1,
            DataLabelVisualCount: 0,
            FormattedVisualCount: 1,
            Visuals: [
              {
                VisualId: 'v1',
                VisualType: 'barChart',
                X: 0,
                Y: 0,
                Width: 320,
                Height: 180,
              },
            ],
          },
        },
      ],
    })).toThrow("Missing required boolean field 'pageScores[0].visualMetadata.visuals[0].isHidden'");
  });
});
