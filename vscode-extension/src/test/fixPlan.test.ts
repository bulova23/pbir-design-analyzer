import type { NormalizedFinding } from '../analyzer/contracts/scorePanel';
import { buildFixPlan } from '../analyzer/score/fixPlan';

describe('buildFixPlan', () => {
  it('builds a prioritized remediation queue linked back to normalized findings', () => {
    const findings: NormalizedFinding[] = [
      {
        id: 'high-actionability',
        title: 'Actionability gap',
        summary: 'Exception visibility is weak.',
        severity: 'high',
        confidence: 88,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'actionability',
        frameworkImpact: ['Narrative Design'],
        recommendation: 'Add a stronger exception callout.',
        sourceKind: 'actionability',
        sourceSection: 'issues',
        evidence: [],
      },
      {
        id: 'medium-navigation',
        title: 'Navigation inconsistency',
        summary: 'Navigation controls drift across detail pages.',
        severity: 'medium',
        confidence: 74,
        scope: 'crossPage',
        detectionType: 'deterministic',
        affectedPages: ['Detail', 'Finance'],
        impactArea: 'navigation',
        frameworkImpact: ['Enterprise Governance'],
        recommendation: 'Keep navigation in one predictable zone.',
        sourceKind: 'reportConsistency',
        sourceSection: 'issues',
        evidence: [],
      },
    ];

    const queue = buildFixPlan(findings);

    expect(queue[0]).toMatchObject({
      title: 'Actionability gap',
      severity: 'high',
      effort: 'medium',
      sourceFindingIds: ['high-actionability'],
      recommendedAction: 'Add a stronger exception callout.',
    });
    expect(queue[1]).toMatchObject({
      severity: 'medium',
      effort: 'high',
      sourceFindingIds: ['medium-navigation'],
    });
  });
});
