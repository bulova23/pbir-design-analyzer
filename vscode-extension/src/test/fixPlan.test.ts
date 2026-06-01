import type { NormalizedFinding } from '../analyzer/contracts/scorePanel';
import { buildFixPlan } from '../analyzer/score/fixPlan';

describe('buildFixPlan', () => {
  it('builds a grouped remediation queue linked back to normalized findings', () => {
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
        id: 'high-benchmark',
        title: 'Benchmark gap',
        summary: 'Target benchmarks are missing from the executive KPI band.',
        severity: 'high',
        confidence: 85,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'benchmark',
        frameworkImpact: ['Narrative Design'],
        recommendation: 'Add target benchmarks to the KPI band.',
        sourceKind: 'benchmark',
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

    expect(queue).toHaveLength(2);
    expect(queue[0]).toMatchObject({
      title: 'Add benchmarks and decision context',
      severity: 'high',
      effort: 'low',
      sourceFindingIds: ['high-actionability', 'high-benchmark'],
      impact: 'high',
      why: 'Reduces risk of KPI misinterpretation.',
      resolvedOutcomes: ['Actionability gap', 'Benchmark gap'],
    });
    expect(queue[1]).toMatchObject({
      title: 'Standardize navigation cues',
      severity: 'medium',
      effort: 'high',
      impact: 'medium',
      why: 'Makes navigation more predictable across related pages.',
      sourceFindingIds: ['medium-navigation'],
    });
  });
});
