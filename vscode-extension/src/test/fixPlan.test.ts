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

  it('builds migration-readiness remediation items for Fabric readiness findings', () => {
    const findings: NormalizedFinding[] = [
      {
        id: 'readiness-blocker-navigation',
        title: 'Migration Blocker',
        summary: 'Navigation complexity is likely too Power BI-specific for direct migration.',
        severity: 'high',
        confidence: 88,
        scope: 'report',
        detectionType: 'deterministic',
        affectedPages: ['Overview', 'Detail'],
        impactArea: 'navigation',
        frameworkImpact: ['Fabric App Readiness'],
        recommendation: 'Simplify navigation before treating the report as an app candidate.',
        sourceKind: 'fabricAppReadiness',
        sourceSection: 'issues',
        evidence: [],
      },
      {
        id: 'readiness-semantic',
        title: 'Migration Blocker',
        summary: 'Semantic labeling is too weak for clean app reuse.',
        severity: 'medium',
        confidence: 82,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Detail'],
        impactArea: 'metadata',
        frameworkImpact: ['Fabric App Readiness'],
        recommendation: 'Improve semantic labeling and measure framing for app reuse.',
        sourceKind: 'fabricAppReadiness',
        sourceSection: 'issues',
        evidence: [],
      },
    ];

    const queue = buildFixPlan(findings);

    expect(queue).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          title: 'Simplify navigation for app portability',
        }),
        expect.objectContaining({
          title: 'Improve semantic labeling for app reuse',
        }),
      ]),
    );
  });

  it('sequences Guided Story Improvements in the recommended story order while preserving non-story items', () => {
    const findings: NormalizedFinding[] = [
      {
        id: 'overview-guided-story-missing-prior-period-context',
        title: 'Add prior-period context',
        summary: 'The page shows the current result, but not enough context about movement over time.',
        severity: 'medium',
        confidence: 78,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'benchmark',
        frameworkImpact: ['Story Assessment'],
        recommendation: 'Add prior-period context to the headline result.',
        sourceKind: 'guidedStoryImprovement',
        sourceSection: 'issues',
        evidence: [],
      },
      {
        id: 'overview-guided-story-missing-title-question-anchor',
        title: 'Add a clearer page question or title',
        summary: 'The page does not establish its main question or decision early enough.',
        severity: 'high',
        confidence: 90,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'storytelling',
        frameworkImpact: ['Story Assessment'],
        recommendation: 'Add a clearer page question or title.',
        sourceKind: 'guidedStoryImprovement',
        sourceSection: 'issues',
        evidence: [],
      },
      {
        id: 'overview-guided-story-scattered-filters',
        title: 'Consolidate scattered filters',
        summary: 'Filter controls are spread across the page instead of creating one clear exploration entry point.',
        severity: 'medium',
        confidence: 78,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'storytelling',
        frameworkImpact: ['Story Assessment'],
        recommendation: 'Consolidate scattered filters.',
        sourceKind: 'guidedStoryImprovement',
        sourceSection: 'issues',
        evidence: [],
      },
      {
        id: 'cross-page-navigation',
        title: 'navigation',
        summary: 'Navigation patterns differ across the report.',
        severity: 'medium',
        confidence: 74,
        scope: 'crossPage',
        detectionType: 'deterministic',
        affectedPages: ['Details'],
        impactArea: 'navigation',
        frameworkImpact: ['Enterprise Governance'],
        recommendation: 'Keep navigation controls in one predictable zone.',
        sourceKind: 'reportConsistency',
        sourceSection: 'issues',
        evidence: [],
      },
    ];

    const queue = buildFixPlan(findings);

    expect(queue.slice(0, 3).map((item) => item.title)).toEqual([
      'Add a clearer page question or title',
      'Add prior-period context',
      'Consolidate scattered filters',
    ]);
    expect(queue[3]).toMatchObject({
      title: 'Standardize navigation cues',
    });
  });
});
