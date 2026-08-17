import {
  buildRenderedReviewChecklist,
  classifyRenderedReviewFinding,
  updateRenderedReviewItem,
} from '../analyzer/renderedReview/reviewModel';
import { buildNormalizedFindings } from '../analyzer/score/normalizedFindings';
import type { NormalizedFinding, PageVisualMetadataSummary } from '../analyzer/contracts/scorePanel';

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
        provider: 'Manual attachment',
        fileReference: '/tmp/overview.png',
        notes: 'Post-mutation review',
      },
    });

    expect(item.status).toBe('Not Reviewed');
    expect(updated).toMatchObject({
      status: 'Confirmed',
      reviewerNote: 'KPI is visually dominant after mutation.',
      screenshotEvidence: [{ provider: 'Manual attachment', fileReference: '/tmp/overview.png' }],
    });
  });

  it('classifies a customVisual-evidence finding as unsupportedVisualType, independent of wording', () => {
    const finding: NormalizedFinding = {
      id: 'custom-visual-overview-v1',
      title: 'Some unrelated title that mentions nothing about categories below',
      summary: 'Some unrelated summary text.',
      severity: 'medium',
      confidence: 90,
      scope: 'page',
      detectionType: 'deterministic',
      affectedPages: ['Overview'],
      impactArea: 'metadata',
      frameworkImpact: [],
      recommendation: 'Attach a screenshot.',
      sourceKind: 'customVisual',
      sourceSection: 'issues',
      evidence: [{ kind: 'customVisual', label: 'Deneb visual', pageName: 'Overview', visualId: 'v1' }],
    };

    const result = classifyRenderedReviewFinding(finding);

    expect(result.classification).toBe('renderedReviewRecommended');
    expect(result.category).toBe('unsupportedVisualType');
  });

  it('routes a real custom-visual finding built by buildNormalizedFindings into the unsupportedVisualType category', () => {
    const visualMetadata: PageVisualMetadataSummary = {
      pageName: 'Overview',
      semanticColorMap: [],
      visualCount: 1,
      visibleTitleVisualCount: 0,
      textVisualCount: 0,
      slicerCount: 0,
      legendVisualCount: 0,
      axisLabelVisualCount: 0,
      dataLabelVisualCount: 0,
      formattedVisualCount: 0,
      visuals: [
        {
          visualId: 'v1',
          visualType: 'deneb7E15AEF80B9E4D4F8E12924291ECE89A',
          x: 0,
          y: 0,
          width: 100,
          height: 100,
          isHidden: false,
          isNavigationElement: false,
          isDecorative: false,
          isSlicer: false,
          hasVisibleTitleIntent: false,
          categoryHints: [],
          valueHints: [],
          seriesHints: [],
          measureHints: [],
          semanticColors: [],
          customVisualEvidence: {
            kind: 'deneb',
            visualType: 'deneb7E15AEF80B9E4D4F8E12924291ECE89A',
            denebHasTooltip: false,
          },
        },
      ],
    };

    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata,
    } as never);

    const customVisualFinding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(customVisualFinding).toBeDefined();
    expect(customVisualFinding!.reviewClassification).toBe('renderedReviewRecommended');
    expect(customVisualFinding!.renderedReviewCategory).toBe('unsupportedVisualType');
    expect(classifyRenderedReviewFinding(customVisualFinding!)).toEqual({
      classification: 'renderedReviewRecommended',
      category: 'unsupportedVisualType',
    });
  });
});
