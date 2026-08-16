import {
  buildScorePanelState,
  clampSelectedPageIndex,
  parseScorePanelHostMessage,
  parseScorePanelWebviewMessage,
  withScorePanelEnvelope,
} from '../views/scorePanelProtocol';

describe('scorePanelProtocol', () => {
  it('accepts Guided Story Improvements when only safe public fields are present', () => {
    const parsed = parseScorePanelHostMessage(withScorePanelEnvelope({
      type: 'scoreState',
      state: {
        protocolVersion: 1,
        schemaVersion: 1,
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
          pageCount: 1,
          recommendations: [],
          reportPath: '/tmp/Sales.Report',
          scoredAt: '2026-06-11T00:00:00.000Z',
          guidedStoryImprovements: {
            highPriorityImprovements: [
              {
                id: 'story-missing-title',
                title: 'Add a clearer page question or title',
                summary: 'The page does not establish its main question early enough.',
                rationale: 'Readers need a clear story anchor before interpreting the visuals.',
                expectedImpact: 'Stronger narrative scan path.',
                priority: 'high',
                relatedImpactArea: 'storytelling',
                navigationTarget: {
                  kind: 'page',
                  pageName: 'Overview',
                  label: 'Open Overview page',
                  reason: 'This recommendation affects page framing.',
                  supportState: 'direct',
                },
              },
            ],
            mediumPriorityImprovements: [],
            storyImprovementRationale: 'The current page story is understandable, but its entry point is weak.',
          },
        },
        selectedPageIndex: 0,
        intentFeedback: [],
      },
    }));

    expect(parsed.ok).toBe(true);
  });

  it('accepts navigateToTarget webview messages with safe additive navigation targets', () => {
    const parsed = parseScorePanelWebviewMessage(withScorePanelEnvelope({
      type: 'navigateToTarget',
      target: {
        kind: 'visual',
        pageName: 'Overview',
        visualId: 'hero-kpi',
        label: 'Open lead KPI visual',
        reason: 'This recommendation is tied to the main KPI.',
        supportState: 'direct',
      },
    }));

    expect(parsed).toEqual({
      ok: true,
      message: {
        type: 'navigateToTarget',
        target: {
          kind: 'visual',
          pageName: 'Overview',
          visualId: 'hero-kpi',
          label: 'Open lead KPI visual',
          reason: 'This recommendation is tied to the main KPI.',
          supportState: 'direct',
        },
      },
    });
  });

  it('rejects malformed navigateToTarget messages before host execution', () => {
    const parsed = parseScorePanelWebviewMessage(withScorePanelEnvelope({
      type: 'navigateToTarget',
      target: {
        kind: 'visual',
        label: 'Open lead KPI visual',
        reason: 'This recommendation is tied to the main KPI.',
        supportState: 'direct',
      },
    }));

    expect(parsed).toEqual({
      ok: false,
      error: expect.stringContaining('navigateToTarget'),
    });
  });

  it('rejects score state payloads that include research-stage Guided Story Improvements fields', () => {
    const parsed = parseScorePanelHostMessage(withScorePanelEnvelope({
      type: 'scoreState',
      state: {
        protocolVersion: 1,
        schemaVersion: 1,
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
          pageCount: 1,
          recommendations: [],
          reportPath: '/tmp/Sales.Report',
          scoredAt: '2026-06-11T00:00:00.000Z',
          guidedStoryImprovements: {
            highPriorityImprovements: [],
            mediumPriorityImprovements: [],
            storyImprovementRationale: 'The page needs a stronger narrative frame.',
            confidenceBreakdown: {
              accuracy: 'low',
            },
          },
        },
        selectedPageIndex: 0,
        intentFeedback: [],
      },
    }));

    expect(parsed).toEqual({
      ok: false,
      error: expect.stringContaining('Guided Story Improvements'),
    });
  });

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

  it('preserves itemId on an attachRenderedScreenshot message instead of treating it as payload-free', () => {
    // Regression: 'attachRenderedScreenshot' was previously listed alongside genuinely
    // payload-free types (webviewReady, refresh, ...), which returned `{ type }` before the
    // switch below ever ran its dedicated case — silently dropping itemId on every click.
    const parsed = parseScorePanelWebviewMessage({
      type: 'attachRenderedScreenshot',
      protocolVersion: 1,
      schemaVersion: 1,
      itemId: 'whitespace-balance:Customer Analysis',
    });

    expect(parsed).toEqual({
      ok: true,
      message: { type: 'attachRenderedScreenshot', itemId: 'whitespace-balance:Customer Analysis' },
    });
  });

  it('rejects an attachRenderedScreenshot message with no itemId', () => {
    const parsed = parseScorePanelWebviewMessage({
      type: 'attachRenderedScreenshot',
      protocolVersion: 1,
      schemaVersion: 1,
    });

    expect(parsed).toEqual({
      ok: false,
      error: 'Score panel rendered screenshot message is missing itemId.',
    });
  });
});
