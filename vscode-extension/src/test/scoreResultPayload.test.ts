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
      VisualMetadata: {
        PageName: 'Overview',
        VisiblePageTitle: 'Sales Overview',
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
        SemanticColors: [
          {
            SemanticKey: 'revenue',
            DisplayName: 'Revenue',
            ColorHex: '#00AA55',
            SemanticRole: 'positive',
            VisualIds: ['v1'],
          },
        ],
        ChartIntents: [
          {
            VisualId: 'v1',
            Intent: 'comparison',
            Confidence: 'high',
            FitQuality: 'good',
            Reasons: ['Bar chart compares categories.'],
            SuggestedVisualTypes: ['barChart', 'columnChart'],
          },
        ],
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
          },
        ],
      },
      ReportConsistency: {
        Score: 88,
        Findings: [
          {
            Rule: 'semanticColorConsistency',
            Severity: 'warning',
            Message: 'Revenue uses multiple semantic colors across pages.',
            AffectedPages: ['Overview', 'Details'],
            AffectedVisuals: [
              {
                PageName: 'Overview',
                VisualId: 'v1',
                VisualType: 'barChart',
              },
            ],
          },
        ],
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
          FrameworkWeights: {
            gestalt: 60,
            cognitiveLoad: 40,
          },
          ReportConsistency: {
            Score: 88,
            Findings: [
              {
                Rule: 'semanticColorConsistency',
                Severity: 'warning',
                Message: 'Revenue uses multiple semantic colors across pages.',
                AffectedPages: ['Overview', 'Details'],
              },
            ],
          },
          VisualMetadata: {
            PageName: 'Overview',
            VisiblePageTitle: 'Sales Overview',
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
    expect(normalized.pageScores?.[0].frameworkWeights).toEqual({
      gestalt: 60,
      cognitiveLoad: 40,
    });
    expect(normalized.visualMetadata?.visiblePageTitle).toBe('Sales Overview');
    expect(normalized.visualMetadata?.visuals[0]).toMatchObject({
      visualId: 'v1',
      visualType: 'barChart',
      hasLegend: true,
      categoryHints: ['Region'],
      hasBorder: true,
    });
    expect(normalized.visualMetadata?.semanticColors).toEqual([
      {
        semanticKey: 'revenue',
        displayName: 'Revenue',
        colorHex: '#00AA55',
        semanticRole: 'positive',
        visualIds: ['v1'],
      },
    ]);
    expect(normalized.visualMetadata?.chartIntents).toEqual([
      {
        visualId: 'v1',
        intent: 'comparison',
        confidence: 'high',
        fitQuality: 'good',
        reasons: ['Bar chart compares categories.'],
        suggestedVisualTypes: ['barChart', 'columnChart'],
      },
    ]);
    expect(normalized.pageScores?.[0].visualMetadata?.visiblePageTitle).toBe('Sales Overview');
    expect(normalized.pageScores?.[0].reportConsistency).toEqual({
      score: 88,
      findings: [
        {
          rule: 'semanticColorConsistency',
          severity: 'warning',
          message: 'Revenue uses multiple semantic colors across pages.',
          affectedPages: ['Overview', 'Details'],
          affectedVisuals: [],
        },
      ],
    });
    expect(normalized.reportConsistency).toEqual({
      score: 88,
      findings: [
        {
          rule: 'semanticColorConsistency',
          severity: 'warning',
          message: 'Revenue uses multiple semantic colors across pages.',
          affectedPages: ['Overview', 'Details'],
          affectedVisuals: [
            {
              pageName: 'Overview',
              visualId: 'v1',
              visualType: 'barChart',
            },
          ],
        },
      ],
    });
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

  it('defaults missing semantic and consistency collections to empty arrays', () => {
    const normalized = normalizeScoreResultPayload({
      VisualMetadata: {
        PageName: 'Overview',
      },
      ReportConsistency: {
        Score: 91,
      },
      PageScores: [
        {
          PageName: 'Overview',
          ReportConsistency: {},
          VisualMetadata: {
            PageName: 'Overview',
          },
        },
      ],
    });

    expect(normalized.visualMetadata?.semanticColors).toEqual([]);
    expect(normalized.visualMetadata?.chartIntents).toEqual([]);
    expect(normalized.reportConsistency).toEqual({
      score: 91,
      findings: [],
    });
    expect(normalized.pageScores?.[0].reportConsistency).toEqual({
      score: undefined,
      findings: [],
    });
    expect(normalized.pageScores?.[0].visualMetadata?.semanticColors).toEqual([]);
    expect(normalized.pageScores?.[0].visualMetadata?.chartIntents).toEqual([]);
  });
});
