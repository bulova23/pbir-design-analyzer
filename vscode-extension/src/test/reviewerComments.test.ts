import { buildReviewerComments } from '../analyzer/score/reviewerComments';
import type { PageScore } from '../analyzer/contracts/scorePanel';

function makePage(overrides: Partial<PageScore> = {}): PageScore {
  return {
    pageName: 'Overview',
    gestaltScore: 82,
    cognitiveLoadScore: 74,
    dataInkScore: 78,
    accessibilityScore: 72,
    visualBestPracticesScore: 84,
    stephenFewScore: 70,
    enterpriseGovernanceScore: 76,
    tufteScore: 69,
    graphicalPerceptionScore: 71,
    densityScore: 66,
    narrativeScore: 68,
    compositeScore: 77,
    feedback: {
      narrative: [
        {
          ok: false,
          text: 'Visible page purpose: Overview uses a vague visible title. Replace generic labels such as Overview with a clearer decision-led title.',
          findingType: 'strongHeuristic',
        },
      ],
    },
    recommendations: ['[High] Layout: Snap visuals to grid'],
    inferredStorySummary: {
      intentProfile: 'executiveOverview',
      storyArchetype: 'executive overview + comparison',
      inferredStory: 'This page appears to summarize revenue performance.',
      confidence: 'high',
      evidence: ['Visible title: Overview'],
    },
    pageIntentProfile: {
      inferredProfile: 'executive',
      actionabilityExpectation: 'high',
      reviewGuidance: ['Executive pages should expose the decision, target, and exception path quickly.'],
      evidence: ['2 KPI cards in the top band'],
    },
    actionabilityBreakdown: {
      score: 32,
      targetBenchmarkPresent: false,
      exceptionVisibility: false,
      urgencySignaling: false,
      priorPeriodContext: false,
      drillPathPresent: true,
      expectationLevel: 'high',
      strengths: ['A supporting detail path exists.'],
      gaps: ['Add a target or benchmark next to the KPI.', 'Call out the exception that needs action now.'],
      summary: 'The page looks polished but still does not tell an executive what action to take.',
    },
    benchmarkComparison: {
      archetype: 'executive scorecard',
      benchmarkLabel: 'Executive-ready benchmark',
      comparativePosition: 'below',
      beautifulButUseless: true,
      insight: 'Beautiful but useless: the page looks polished, but the decision path is still weak.',
      strengths: ['Polished presentation'],
      gaps: ['Decision support is weak'],
    },
    ...overrides,
  };
}

describe('buildReviewerComments', () => {
  it('grounds coach comments in actionability and benchmark gaps', () => {
    const comments = buildReviewerComments(makePage(), {
      selectedProfile: 'executive',
      persona: 'coach',
    });

    expect(comments.headline).toMatch(/executive/i);
    expect(comments.comments.join(' ')).toMatch(/target|benchmark/i);
    expect(comments.comments.join(' ')).toMatch(/beautiful but useless/i);
  });

  it('tightens tone for strict design critic without becoming generic', () => {
    const comments = buildReviewerComments(makePage(), {
      selectedProfile: 'executive',
      persona: 'strictDesignCritic',
    });

    expect(comments.comments[0]).toMatch(/page/i);
    expect(comments.comments.join(' ')).toMatch(/decision/i);
    expect(comments.comments.join(' ')).not.toMatch(/looks good overall/i);
  });
});
