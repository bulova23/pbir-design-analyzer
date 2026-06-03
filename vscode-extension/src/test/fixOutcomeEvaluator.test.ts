import type { FixOpportunity, ScoreResult } from '../analyzer/contracts/scorePanel';
import { evaluateFixOutcome, summarizeBatchFixOutcomes } from '../analyzer/fixes/fixOutcomeEvaluator';

function scoreResult(sourceFinding: { id: string; severity: 'high' | 'medium' | 'low' } | undefined): ScoreResult {
  return {
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
    scoredAt: '2026-05-31T18:00:00.000Z',
    normalizedFindings: sourceFinding ? [{
      id: sourceFinding.id,
      title: 'Grid alignment',
      summary: 'Grid alignment issue',
      severity: sourceFinding.severity,
      confidence: 88,
      scope: 'page',
      detectionType: 'deterministic',
      affectedPages: ['Overview'],
      impactArea: 'layout',
      frameworkImpact: ['Gestalt Principles'],
      recommendation: 'Align the visuals',
      sourceKind: 'framework',
      sourceSection: 'issues',
      evidence: [],
    }] : [],
  };
}

const opportunity: FixOpportunity = {
  id: 'fix-1',
  remediationItemId: 'fix-layout',
  title: 'Reduce visual density and align layout (alignment)',
  category: 'alignment',
  summary: 'Improves scanability.',
  confidence: 95,
  safetyClass: 'safe',
  affectedPages: ['Overview'],
  targetObjectIds: ['chart-1'],
  sourceFindingIds: ['layout-finding'],
  expectedResolutions: ['Layout consistency'],
  mutations: [],
  previewRows: [],
  rollbackPlan: {
    id: 'rollback-fix-1',
    fixOpportunityId: 'fix-1',
    fileBackups: [],
    reverseMutations: [],
  },
  state: 'Applied',
};

describe('evaluateFixOutcome', () => {
  it('marks missing source findings as resolved', () => {
    const summary = evaluateFixOutcome(opportunity, scoreResult({ id: 'layout-finding', severity: 'high' }), scoreResult(undefined));
    expect(summary.nextState).toBe('Applied');
    expect(summary.outcome.entries[0]).toMatchObject({ findingId: 'layout-finding', status: 'Resolved' });
  });

  it('marks downgraded severity as improved', () => {
    const summary = evaluateFixOutcome(opportunity, scoreResult({ id: 'layout-finding', severity: 'high' }), scoreResult({ id: 'layout-finding', severity: 'medium' }));
    expect(summary.outcome.entries[0]).toMatchObject({ findingId: 'layout-finding', status: 'Improved' });
  });

  it('marks unchanged severity as unexpected outcome state', () => {
    const summary = evaluateFixOutcome(opportunity, scoreResult({ id: 'layout-finding', severity: 'high' }), scoreResult({ id: 'layout-finding', severity: 'high' }));
    expect(summary.nextState).toBe('AppliedWithUnexpectedOutcome');
    expect(summary.outcome.entries[0]).toMatchObject({ findingId: 'layout-finding', status: 'Unexpected' });
  });

  it('builds grouped batch outcome summaries without changing individual entry semantics', () => {
    const first = evaluateFixOutcome(opportunity, scoreResult({ id: 'layout-finding', severity: 'high' }), scoreResult(undefined));
    const second = evaluateFixOutcome(opportunity, scoreResult({ id: 'layout-finding', severity: 'high' }), scoreResult({ id: 'layout-finding', severity: 'high' }));

    const grouped = summarizeBatchFixOutcomes([
      { opportunityId: 'fix-1', title: opportunity.title, outcome: first.outcome, state: first.nextState },
      { opportunityId: 'fix-2', title: opportunity.title, outcome: second.outcome, state: second.nextState },
    ]);

    expect(grouped.totalEntries).toBe(2);
    expect(grouped.statuses).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ status: 'Resolved', count: 1 }),
        expect.objectContaining({ status: 'Unexpected', count: 1 }),
      ]),
    );
    expect(grouped.appliedWithUnexpectedOutcomeOpportunityIds).toEqual(['fix-2']);
  });
});
