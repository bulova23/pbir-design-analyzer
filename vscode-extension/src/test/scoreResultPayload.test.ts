import { normalizeScoreResultPayload } from '../views/scoreResultPayload';

describe('normalizeScoreResultPayload', () => {
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
    expect(normalized.fixPlan).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          severity: expect.any(String),
          effort: expect.any(String),
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

  it('defaults missing or invalid finding types to strongHeuristic', () => {
    const normalized = normalizeScoreResultPayload({
      Feedback: {
        gestalt: [
          { Ok: false, Text: 'Grid alignment: Off-grid visual detected.' },
          { Ok: false, Text: 'Similarity: Visual mix is noisy.', FindingType: 'not-real' },
        ],
      },
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

  it('drops partial report consistency summaries and malformed nested semantic metadata', () => {
    const normalized = normalizeScoreResultPayload({
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
});
