import type { CrossPageMatrixCell, NormalizedFinding, PageScore } from '../analyzer/contracts/scorePanel';
import { buildCrossPageMatrix } from '../analyzer/score/crossPageMatrix';

describe('buildCrossPageMatrix', () => {
  it('maps findings into a navigation-aware page-by-dimension matrix', () => {
    const findings: NormalizedFinding[] = [
      {
        id: 'layout-overview',
        title: 'Layout drift',
        summary: 'Layout drift on overview page.',
        severity: 'medium',
        confidence: 70,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'layout',
        frameworkImpact: [],
        recommendation: 'Align the layout.',
        sourceKind: 'frameworkFeedback',
        sourceSection: 'issues',
        evidence: [],
      },
      {
        id: 'story-overview',
        title: 'Story issue',
        summary: 'Story issue on overview page.',
        severity: 'low',
        confidence: 80,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'storytelling',
        frameworkImpact: [],
        recommendation: 'Clarify the story.',
        sourceKind: 'frameworkFeedback',
        sourceSection: 'issues',
        evidence: [],
      },
      {
        id: 'navigation-detail',
        title: 'Navigation drift',
        summary: 'Navigation drift on detail page.',
        severity: 'high',
        confidence: 90,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Detail'],
        impactArea: 'navigation',
        frameworkImpact: [],
        recommendation: 'Normalize navigation.',
        sourceKind: 'reportConsistency',
        sourceSection: 'issues',
        evidence: [],
      },
      {
        id: 'actionability-cross',
        title: 'Cross-page actionability issue',
        summary: 'Actionability is inconsistent across the report.',
        severity: 'medium',
        confidence: 76,
        scope: 'crossPage',
        detectionType: 'deterministic',
        affectedPages: ['Overview', 'Detail'],
        impactArea: 'actionability',
        frameworkImpact: [],
        recommendation: 'Normalize actionability cues.',
        sourceKind: 'actionability',
        sourceSection: 'issues',
        evidence: [],
      },
    ];
    const pages = [
      { pageName: 'Overview' },
      { pageName: 'Detail' },
    ] as PageScore[];

    const matrix = buildCrossPageMatrix(findings, pages);

    expect(matrix?.dimensions).toEqual([
      'layout',
      'story',
      'accessibility',
      'consistency',
      'navigation',
      'actionability',
    ]);
    expect(matrix?.rows[0].pageName).toBe('Overview');
    expect(matrix?.rows[0].cells[0]).toMatchObject({
      pageName: 'Overview',
      dimension: 'layout',
      severity: 'medium',
      findingCount: 1,
      highSeverityCount: 0,
      status: 'watch',
      relatedFindingIds: ['layout-overview'],
    });
    expect(matrix?.rows[0].cells[1]).toMatchObject({
      pageName: 'Overview',
      dimension: 'story',
      severity: 'low',
      findingCount: 1,
      status: 'watch',
      relatedFindingIds: ['story-overview'],
    });
    expect(matrix?.rows[1].cells.find((cell: CrossPageMatrixCell) => cell.dimension === 'navigation')).toMatchObject({
      pageName: 'Detail',
      dimension: 'navigation',
      severity: 'high',
      findingCount: 1,
      highSeverityCount: 1,
      status: 'weak',
      relatedFindingIds: ['navigation-detail'],
    });
    expect(matrix?.rows[0].cells.find((cell: CrossPageMatrixCell) => cell.dimension === 'actionability')).toMatchObject({
      pageName: 'Overview',
      dimension: 'actionability',
      severity: 'medium',
      findingCount: 1,
      status: 'weak',
      relatedFindingIds: ['actionability-cross'],
    });
    expect(matrix?.rows[0].cells.find((cell: CrossPageMatrixCell) => cell.dimension === 'accessibility')).toMatchObject({
      pageName: 'Overview',
      dimension: 'accessibility',
      findingCount: 0,
      highSeverityCount: 0,
      status: 'unknown',
      relatedFindingIds: [],
    });
  });
});
