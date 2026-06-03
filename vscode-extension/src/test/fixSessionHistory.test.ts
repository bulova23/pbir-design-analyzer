import type { FixApplySessionRecord } from '../analyzer/contracts/scorePanel';
import {
  createFixApplySessionRecord,
  markFixSessionRegenerated,
  recordFixSessionRollback,
} from '../analyzer/fixes/fixSessionHistory';

const baseSession: FixApplySessionRecord = {
  id: 'session-1',
  appliedAt: '2026-06-01T22:30:00.000Z',
  opportunityIds: ['fix-1', 'fix-2'],
  opportunityTitles: ['Fix 1', 'Fix 2'],
  rollbackAvailable: true,
  rollbackHistory: [],
};

describe('fixSessionHistory', () => {
  it('records applied opportunities and rollback availability', () => {
    const session = createFixApplySessionRecord({
      appliedAt: '2026-06-01T22:30:00.000Z',
      opportunities: [
        { id: 'fix-1', title: 'Fix 1', state: 'Applied' },
        { id: 'fix-2', title: 'Fix 2', state: 'AppliedWithUnexpectedOutcome' },
      ],
      rollbackAvailable: true,
      groupedOutcomeSummary: {
        totalEntries: 2,
        statuses: [
          { status: 'Resolved', count: 1, opportunityIds: ['fix-1'] },
          { status: 'Unexpected', count: 1, opportunityIds: ['fix-2'] },
        ],
        appliedWithUnexpectedOutcomeOpportunityIds: ['fix-2'],
      },
    });

    expect(session).toMatchObject({
      opportunityIds: ['fix-1', 'fix-2'],
      rollbackAvailable: true,
      groupedOutcomeSummary: expect.objectContaining({
        appliedWithUnexpectedOutcomeOpportunityIds: ['fix-2'],
      }),
    });
  });

  it('records rollback success and failure history deterministically', () => {
    const rolledBack = recordFixSessionRollback(baseSession, {
      rolledBackAt: '2026-06-01T22:31:00.000Z',
      state: 'RolledBack',
    });
    const rollbackFailed = recordFixSessionRollback(rolledBack, {
      rolledBackAt: '2026-06-01T22:32:00.000Z',
      state: 'RollbackFailed',
    });

    expect(rollbackFailed.rollbackHistory).toEqual([
      { rolledBackAt: '2026-06-01T22:31:00.000Z', state: 'RolledBack' },
      { rolledBackAt: '2026-06-01T22:32:00.000Z', state: 'RollbackFailed' },
    ]);
  });

  it('records superseded and regenerated opportunities without losing the original session', () => {
    const session = markFixSessionRegenerated(baseSession, {
      staleOpportunityIds: ['fix-1'],
      regeneratedOpportunityIds: ['fix-1b'],
    });

    expect(session.staleOpportunityIds).toEqual(['fix-1']);
    expect(session.regeneratedOpportunityIds).toEqual(['fix-1b']);
  });
});
