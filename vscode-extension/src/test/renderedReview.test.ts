import {
  buildRenderedReviewChecklist,
  classifyRenderedReviewFinding,
  updateRenderedReviewItem,
} from '../analyzer/renderedReview/reviewModel';
import type { NormalizedFinding } from '../analyzer/contracts/scorePanel';

function finding(overrides: Partial<NormalizedFinding> = {}): NormalizedFinding {
  return {
    id: 'finding-1',
    title: 'KPI prominence',
    summary: 'The KPI cards may not visually dominate supporting charts.',
    severity: 'medium',
    confidence: 80,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Overview'],
    impactArea: 'kpiEffectiveness',
    frameworkImpact: ['Graphical Perception'],
    recommendation: 'Review the visual hierarchy after opening the rendered page.',
    sourceKind: 'frameworkFeedback',
    sourceSection: 'issues',
    evidence: [{ kind: 'framework', label: 'Graphical Perception', pageName: 'Overview' }],
    ...overrides,
  };
}

describe('rendered review model', () => {
  it('classifies visual judgment findings as rendered review recommendations', () => {
    expect(classifyRenderedReviewFinding(finding())).toEqual({
      classification: 'renderedReviewRecommended',
      category: 'kpiProminence',
    });
  });

  it('keeps deterministic and semantic findings out of the rendered checklist', () => {
    expect(classifyRenderedReviewFinding(finding({
      id: 'deterministic',
      title: 'Invalid visual configuration',
      summary: 'The visual contains an unsupported property.',
    }))).toEqual({ classification: 'deterministic' });

    expect(classifyRenderedReviewFinding(finding({
      id: 'semantic',
      title: 'Semantic model usage',
      summary: 'The page uses a deprecated semantic model field.',
      evidence: [{ kind: 'semanticModel', label: 'Semantic model usage' }],
    })).classification).toBe('semantic');
  });

  it('builds one guided checklist item per rendered category', () => {
    const checklist = buildRenderedReviewChecklist([
      finding(),
      finding({ id: 'kpi-2', title: 'KPI emphasis', summary: 'KPI cards need more visual emphasis.' }),
      finding({ id: 'title', title: 'Title wrapping', summary: 'The title may wrap at normal width.' }),
    ]);

    expect(checklist.map((item) => item.category)).toEqual(['kpiProminence', 'titleWrapping']);
    expect(checklist[0]).toMatchObject({
      status: 'Not Reviewed',
      findingIds: ['finding-1', 'kpi-2'],
      guidance: {
        why: expect.any(String),
        lookFor: expect.any(String),
        expectedOutcome: expect.any(String),
      },
    });
  });

  it('records rendered review state, notes, and screenshot evidence immutably', () => {
    const item = buildRenderedReviewChecklist([finding()])[0];
    const updated = updateRenderedReviewItem(item, {
      status: 'Confirmed',
      reviewerNote: 'KPI is visually dominant after mutation.',
      screenshot: {
        report: 'sales.pbip',
        page: 'Overview',
        timestamp: '2026-08-15T12:00:00.000Z',
        provider: 'PBI Lens',
        fileReference: '/tmp/overview.png',
        notes: 'Post-mutation review',
      },
    });

    expect(item.status).toBe('Not Reviewed');
    expect(updated).toMatchObject({
      status: 'Confirmed',
      reviewerNote: 'KPI is visually dominant after mutation.',
      screenshotEvidence: [{ provider: 'PBI Lens', fileReference: '/tmp/overview.png' }],
    });
  });
});
