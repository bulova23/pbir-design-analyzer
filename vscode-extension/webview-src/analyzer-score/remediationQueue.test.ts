import type { NormalizedFinding } from '../../src/analyzer/contracts/scorePanel';
import { buildContextAwareRemediationQueue, type RemediationFilterState } from './remediationQueue';

const findings: NormalizedFinding[] = [
  {
    id: 'overview-layout-high',
    title: 'Grid alignment',
    summary: 'Top-row cards overlap instead of holding a clean band.',
    severity: 'high',
    confidence: 84,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Overview'],
    impactArea: 'layout',
    frameworkImpact: ['Gestalt Principles'],
    recommendation: 'Tighten the layout so peer visuals read as a deliberate system.',
    sourceKind: 'framework',
    sourceSection: 'issues',
    evidence: [],
  },
  {
    id: 'overview-density-medium',
    title: 'Visual density',
    summary: 'The page is too crowded for fast executive scanning.',
    severity: 'medium',
    confidence: 79,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Overview'],
    impactArea: 'density',
    frameworkImpact: ['Cognitive Load'],
    recommendation: 'Split dense sections into a smaller number of focal visuals.',
    sourceKind: 'framework',
    sourceSection: 'issues',
    evidence: [],
  },
  {
    id: 'overview-story-high',
    title: 'Story clarity',
    summary: 'The page purpose is implied rather than stated.',
    severity: 'high',
    confidence: 82,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Overview'],
    impactArea: 'storytelling',
    frameworkImpact: ['Narrative Design'],
    recommendation: 'Add an explicit page-purpose anchor above the KPI band.',
    sourceKind: 'framework',
    sourceSection: 'issues',
    evidence: [],
  },
  {
    id: 'details-layout-medium',
    title: 'Layout spacing',
    summary: 'Detail visuals drift off a consistent grid.',
    severity: 'medium',
    confidence: 70,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Details'],
    impactArea: 'layout',
    frameworkImpact: ['Gestalt Principles'],
    recommendation: 'Use the dominant page grid on detail pages.',
    sourceKind: 'framework',
    sourceSection: 'issues',
    evidence: [],
  },
];

function filters(overrides: Partial<RemediationFilterState> = {}): RemediationFilterState {
  return {
    severity: 'all',
    pageName: 'all',
    dimension: 'all',
    impactArea: 'all',
    scope: 'all',
    detectionType: 'all',
    ...overrides,
  };
}

describe('buildContextAwareRemediationQueue', () => {
  it('derives remediation from page and dimension while keeping related medium findings in scope', () => {
    const queue = buildContextAwareRemediationQueue({
      findings,
      selectedPageName: undefined,
      filters: filters({
        pageName: 'Overview',
        dimension: 'layout',
        severity: 'high',
      }),
    });

    expect(queue.focus.label).toBe('Overview · Layout');
    expect(queue.items).toHaveLength(1);
    expect(queue.items[0]).toMatchObject({
      title: 'Reduce visual density and align layout',
      sourceFindingIds: ['overview-layout-high', 'overview-density-medium'],
      findingCoverageLabel: '1 High · 1 Medium',
    });
    expect(queue.items[0].coverageBySeverity).toEqual({
      high: 1,
      medium: 1,
      low: 0,
      info: 0,
    });
  });

  it('keeps the remediation domain stable across diagnostic-only filters', () => {
    const base = buildContextAwareRemediationQueue({
      findings,
      selectedPageName: undefined,
      filters: filters({
        pageName: 'Overview',
        dimension: 'layout',
      }),
    });
    const narrowed = buildContextAwareRemediationQueue({
      findings,
      selectedPageName: undefined,
      filters: filters({
        pageName: 'Overview',
        dimension: 'layout',
        severity: 'high',
        scope: 'visual',
        detectionType: 'aiAssisted',
      }),
    });

    expect(narrowed.focus.label).toBe(base.focus.label);
    expect(narrowed.items.map((item: { id: string }) => item.id)).toEqual(base.items.map((item: { id: string }) => item.id));
    expect(narrowed.items[0].findingCoverageLabel).toBe('1 High · 1 Medium');
  });

  it('changes the queue when the selected problem area changes', () => {
    const queue = buildContextAwareRemediationQueue({
      findings,
      selectedPageName: undefined,
      filters: filters({
        pageName: 'Overview',
        dimension: 'story',
      }),
    });

    expect(queue.focus.label).toBe('Overview · Story');
    expect(queue.items).toHaveLength(1);
    expect(queue.items[0]).toMatchObject({
      title: 'Clarify page purpose and narrative framing',
      sourceFindingIds: ['overview-story-high'],
      findingCoverageLabel: '1 High',
    });
  });

  it('preserves Guided Story Improvements ordering inside the story remediation queue', () => {
    const guidedFindings: NormalizedFinding[] = [
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
        recommendation: 'Add prior-period context.',
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
    ];

    const queue = buildContextAwareRemediationQueue({
      findings: guidedFindings,
      selectedPageName: 'Overview',
      filters: filters({
        pageName: 'Overview',
        dimension: 'story',
      }),
    });

    expect(queue.items.map((item) => item.title)).toEqual([
      'Add a clearer page question or title',
      'Add prior-period context',
    ]);
  });
});
