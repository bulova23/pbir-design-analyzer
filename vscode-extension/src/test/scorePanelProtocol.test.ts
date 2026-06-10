import {
  buildScorePanelState,
  clampSelectedPageIndex,
  parseScorePanelHostMessage,
  parseScorePanelWebviewMessage,
  withScorePanelEnvelope,
} from '../views/scorePanelProtocol';

describe('scorePanelProtocol', () => {
  it('clamps stale selected page state to the latest page bounds', () => {
    const state = buildScorePanelState({
      config: {
        frameworks: [],
        navigationScoring: { enabled: true, weight: 25 },
        governance: [],
      },
      result: {
        gestaltScore: 80,
        cognitiveLoadScore: 80,
        dataInkScore: 80,
        accessibilityScore: 80,
        visualBestPracticesScore: 80,
        stephenFewScore: 80,
        enterpriseGovernanceScore: 80,
        tufteScore: 80,
        graphicalPerceptionScore: 80,
        densityScore: 80,
        narrativeScore: 80,
        compositeScore: 80,
        feedback: {},
        pageCount: 2,
        pageScores: [
          {
            pageName: 'Overview',
            gestaltScore: 80,
            cognitiveLoadScore: 80,
            dataInkScore: 80,
            accessibilityScore: 80,
            visualBestPracticesScore: 80,
            stephenFewScore: 80,
            enterpriseGovernanceScore: 80,
            tufteScore: 80,
            graphicalPerceptionScore: 80,
            densityScore: 80,
            narrativeScore: 80,
            compositeScore: 80,
            feedback: {},
            recommendations: [],
          },
        ],
        recommendations: [],
        reportPath: '/tmp/Sales.Report',
        scoredAt: '2026-06-10T00:00:00.000Z',
      },
      selectedPageIndex: 7,
      intentFeedback: [],
    });

    expect(state.selectedPageIndex).toBe(1);
    expect(clampSelectedPageIndex(-4, 3)).toBe(0);
  });

  it('rejects host messages when protocol versions diverge', () => {
    const parsed = parseScorePanelHostMessage({
      type: 'loading',
      protocolVersion: 999,
      schemaVersion: 1,
    });

    expect(parsed).toEqual({
      ok: false,
      error: expect.stringContaining('Score panel protocol mismatch'),
    });
  });

  it('rejects invalid score state payloads before the webview consumes them', () => {
    const parsed = parseScorePanelHostMessage(withScorePanelEnvelope({
      type: 'scoreState',
      state: {
        protocolVersion: 1,
        schemaVersion: 1,
        config: {},
        selectedPageIndex: 0,
        intentFeedback: [],
      },
    }));

    expect(parsed).toEqual({
      ok: false,
      error: 'Score panel state payload is missing config or result data.',
    });
  });

  it('rejects webview messages when protocol versions diverge', () => {
    const parsed = parseScorePanelWebviewMessage({
      type: 'refresh',
      protocolVersion: 1,
      schemaVersion: 999,
    });

    expect(parsed).toEqual({
      ok: false,
      error: expect.stringContaining('Score panel protocol mismatch'),
    });
  });
});
