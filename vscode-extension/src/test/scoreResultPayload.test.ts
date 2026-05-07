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
        gestalt: [{ Ok: true, Text: 'Grid alignment: Strong alignment.', EarnedPoints: 35, PossiblePoints: 35 }],
      },
      PageCount: 2,
      Recommendations: ['[High] Layout: Snap visuals to grid'],
      ReportPath: '/tmp/Sales.Report',
      ScoredAt: '2026-05-02T20:00:00.000Z',
      DataVisualCount: 12,
      NavigationVisualCount: 4,
      HiddenVisualCount: 1,
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
            gestalt: [{ Ok: true, Text: 'Grid alignment: Overview grid is aligned.', EarnedPoints: 35, PossiblePoints: 35 }],
          },
          Recommendations: ['[High] Layout: Snap visuals to grid'],
          FrameworkWeights: {
            gestalt: 60,
            cognitiveLoad: 40,
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
      earnedPoints: 35,
      possiblePoints: 35,
    }]);
    expect(normalized.pageScores?.[0].pageName).toBe('Overview');
    expect(normalized.pageScores?.[0].feedback.gestalt).toEqual([{
      ok: true,
      text: 'Grid alignment: Overview grid is aligned.',
      earnedPoints: 35,
      possiblePoints: 35,
    }]);
    expect(normalized.pageScores?.[0].recommendations).toEqual(['[High] Layout: Snap visuals to grid']);
    expect(normalized.pageScores?.[0].frameworkWeights).toEqual({
      gestalt: 60,
      cognitiveLoad: 40,
    });
    expect(normalized.scoringErrors).toEqual({
      Intro: 'Hidden visual parse failed.',
    });
  });
});
