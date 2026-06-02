import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type {
  FixPlanItem,
  NormalizedFinding,
  PageScore,
  ScoreResult,
  VisualMetadataItem,
} from '../analyzer/contracts/scorePanel';
import { buildFixOpportunities } from '../analyzer/fixes/fixOpportunityBuilder';

function visual(overrides: Partial<VisualMetadataItem> = {}): VisualMetadataItem {
  return {
    visualId: 'title-1',
    visualType: 'textbox',
    x: 100,
    y: 180,
    width: 400,
    height: 48,
    isHidden: false,
    isNavigationElement: false,
    isDecorative: false,
    isSlicer: false,
    visibleTitleText: 'Overview Title',
    visibleSubtitleText: undefined,
    textBoxText: 'Overview Title',
    bestVisibleText: 'Overview Title',
    hasVisibleTitleIntent: true,
    hasLegend: false,
    hasAxisLabels: false,
    hasDataLabels: false,
    categoryHints: [],
    valueHints: [],
    seriesHints: [],
    measureHints: [],
    backgroundFillColor: '#ff0000',
    fontColor: '#ffffff',
    hasBorder: true,
    cornerRadius: 8,
    hasShadow: false,
    semanticColors: [],
    chartIntent: undefined,
    ...overrides,
  };
}

function pageScore(overrides: Partial<PageScore> = {}): PageScore {
  return {
    pageName: 'Overview',
    gestaltScore: 80,
    cognitiveLoadScore: 75,
    dataInkScore: 70,
    accessibilityScore: 65,
    visualBestPracticesScore: 78,
    stephenFewScore: 71,
    enterpriseGovernanceScore: 72,
    tufteScore: 69,
    graphicalPerceptionScore: 70,
    densityScore: 68,
    narrativeScore: 73,
    compositeScore: 74,
    feedback: {},
    recommendations: [],
    visualMetadata: {
      pageName: 'Overview',
      visiblePageTitle: 'Overview Title',
      strictVisiblePageTitle: undefined,
      canvasWidth: 1280,
      canvasHeight: 720,
      semanticColorMap: [],
      chartIntentSummary: undefined,
      visualCount: 1,
      visibleTitleVisualCount: 1,
      textVisualCount: 1,
      slicerCount: 0,
      legendVisualCount: 0,
      axisLabelVisualCount: 0,
      dataLabelVisualCount: 0,
      formattedVisualCount: 1,
      visuals: [visual()],
    },
    ...overrides,
  };
}

function finding(id: string, impactArea: NormalizedFinding['impactArea'], pageName = 'Overview'): NormalizedFinding {
  return {
    id,
    title: `${impactArea} issue`,
    summary: `${impactArea} issue summary`,
    severity: 'high',
    confidence: 88,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: [pageName],
    impactArea,
    frameworkImpact: ['Narrative Design'],
    recommendation: `${impactArea} recommendation`,
    sourceKind: 'framework',
    sourceSection: 'issues',
    evidence: [],
  };
}

function fixPlanItem(overrides: Partial<FixPlanItem> = {}): FixPlanItem {
  return {
    id: 'fix-story',
    title: 'Clarify page purpose and narrative framing',
    detail: 'Resolve page purpose ambiguity.',
    severity: 'high',
    effort: 'low',
    impact: 'high',
    why: 'Improves page purpose clarity for executive readers.',
    scope: 'page',
    affectedPages: ['Overview'],
    recommendedAction: 'Standardize page title and anchor.',
    resolvedOutcomes: ['Story clarity'],
    sourceFindingIds: ['story-finding'],
    ...overrides,
  };
}

function createTempReport(): string {
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-fix-opportunity-'));
  const reportRoot = path.join(tempDir, 'Sales.Report');
  const definitionRoot = path.join(reportRoot, 'definition');
  const overviewPageRoot = path.join(definitionRoot, 'pages', 'OverviewPage');
  const detailPageRoot = path.join(definitionRoot, 'pages', 'DetailPage');
  fs.mkdirSync(path.join(overviewPageRoot, 'visuals', 'title-1'), { recursive: true });
  fs.mkdirSync(path.join(overviewPageRoot, 'visuals', 'nav-1'), { recursive: true });
  fs.mkdirSync(path.join(overviewPageRoot, 'visuals', 'chart-1'), { recursive: true });
  fs.mkdirSync(path.join(overviewPageRoot, 'visuals', 'chart-2'), { recursive: true });
  fs.mkdirSync(path.join(detailPageRoot, 'visuals', 'title-2'), { recursive: true });
  fs.mkdirSync(path.join(definitionRoot, 'themes'), { recursive: true });

  fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
  fs.writeFileSync(path.join(definitionRoot, 'report.json'), JSON.stringify({
    name: 'Sales',
    theme: {
      name: 'Corporate Theme',
      href: 'themes/corporate.json',
    },
  }));
  fs.writeFileSync(path.join(definitionRoot, 'themes', 'corporate.json'), JSON.stringify({
    dataColors: ['#ff0000', '#00ff00'],
  }));
  fs.writeFileSync(path.join(definitionRoot, 'pages', 'pages.json'), JSON.stringify({
    pageOrder: ['OverviewPage', 'DetailPage'],
  }));
  fs.writeFileSync(path.join(overviewPageRoot, 'page.json'), JSON.stringify({
    name: 'OverviewPage',
    displayName: 'Overview',
  }));
  fs.writeFileSync(path.join(detailPageRoot, 'page.json'), JSON.stringify({
    name: 'DetailPage',
    displayName: 'Details',
  }));
  fs.writeFileSync(path.join(overviewPageRoot, 'visuals', 'title-1', 'visual.json'), JSON.stringify({
    name: 'title-1',
    position: { x: 100, y: 180, width: 400, height: 48 },
    title: { text: 'Overview Title' },
    visual: { visualType: 'textbox' },
    background: { color: '#ff0000' },
  }));
  fs.writeFileSync(path.join(overviewPageRoot, 'visuals', 'nav-1', 'visual.json'), JSON.stringify({
    name: 'nav-1',
    position: { x: 900, y: 40, width: 120, height: 32 },
    title: { text: 'Back' },
    visual: { visualType: 'button' },
  }));
  fs.writeFileSync(path.join(overviewPageRoot, 'visuals', 'chart-1', 'visual.json'), JSON.stringify({
    name: 'chart-1',
    position: { x: 103, y: 220, width: 390, height: 200 },
    title: { text: 'Chart 1' },
    visual: { visualType: 'barChart' },
  }));
  fs.writeFileSync(path.join(overviewPageRoot, 'visuals', 'chart-2', 'visual.json'), JSON.stringify({
    name: 'chart-2',
    position: { x: 512, y: 218, width: 403, height: 200 },
    title: { text: 'Chart 2' },
    visual: { visualType: 'lineChart' },
  }));
  fs.writeFileSync(path.join(detailPageRoot, 'visuals', 'title-2', 'visual.json'), JSON.stringify({
    name: 'title-2',
    position: { x: 220, y: 140, width: 420, height: 48 },
    title: { text: 'Details Title' },
    visual: { visualType: 'textbox' },
    background: { color: '#00ff00' },
  }));

  return reportRoot;
}

describe('buildFixOpportunities', () => {
  afterEach(() => {
    // Temporary report fixtures are unique per test; clean recursively.
    const tempRoots = fs.readdirSync(os.tmpdir())
      .filter((entry) => entry.startsWith('pbir-fix-opportunity-'))
      .map((entry) => path.join(os.tmpdir(), entry));
    for (const root of tempRoots) {
      fs.rmSync(root, { recursive: true, force: true });
    }
  });

  it('builds a title normalization opportunity from remediation intent', () => {
    const reportPath = createTempReport();
    const result: ScoreResult = {
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
      reportPath,
      scoredAt: '2026-05-31T18:00:00.000Z',
      normalizedFindings: [finding('story-finding', 'storytelling')],
      fixPlan: [fixPlanItem()],
      pageScores: [pageScore()],
    };

    const opportunities = buildFixOpportunities(result);

    expect(opportunities).toHaveLength(1);
    expect(opportunities[0]).toMatchObject({
      remediationItemId: 'fix-story',
      category: 'title',
      state: 'Previewed',
      targetObjectIds: ['title-1'],
    });
    expect(opportunities[0].mutations[0]).toMatchObject({
      targetObjectId: 'title-1',
      mutationType: 'setPosition',
      propertyPath: 'position.y',
      before: 180,
      after: 24,
    });
  });

  it('builds opportunities from single-page scoredPageName and top-level visual metadata', () => {
    const reportPath = createTempReport();
    const singlePage = pageScore();
    const result: ScoreResult = {
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
      reportPath,
      scoredAt: '2026-05-31T18:00:00.000Z',
      scoredPageName: 'Overview',
      visualMetadata: singlePage.visualMetadata,
      normalizedFindings: [finding('story-finding', 'storytelling')],
      fixPlan: [fixPlanItem()],
    };

    const opportunities = buildFixOpportunities(result);

    expect(opportunities).toHaveLength(1);
    expect(opportunities[0]).toMatchObject({
      remediationItemId: 'fix-story',
      category: 'title',
      affectedPages: ['Overview'],
      targetObjectIds: ['title-1'],
    });
    expect(opportunities[0].mutations).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          targetObjectId: 'title-1',
          propertyPath: 'position.y',
          before: 180,
          after: 24,
        }),
      ]),
    );
  });

  it('builds a layout normalization opportunity for layout remediation', () => {
    const reportPath = createTempReport();
    const result: ScoreResult = {
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
      reportPath,
      scoredAt: '2026-05-31T18:00:00.000Z',
      normalizedFindings: [finding('layout-finding', 'layout')],
      fixPlan: [fixPlanItem({
        id: 'fix-layout',
        title: 'Reduce visual density and align layout',
        sourceFindingIds: ['layout-finding'],
        resolvedOutcomes: ['Layout consistency'],
      })],
      pageScores: [pageScore({
        visualMetadata: {
          ...pageScore().visualMetadata!,
          visuals: [
            visual({ visualId: 'chart-1', visualType: 'barChart', x: 103, y: 220, width: 390, height: 200, hasVisibleTitleIntent: false }),
            visual({ visualId: 'chart-2', visualType: 'lineChart', x: 512, y: 218, width: 403, height: 200, hasVisibleTitleIntent: false }),
          ],
          visualCount: 2,
        },
      })],
    };

    const opportunities = buildFixOpportunities(result);

    expect(opportunities).toHaveLength(1);
    expect(opportunities[0].category).toBe('alignment');
    expect(opportunities[0].mutations).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ targetObjectId: 'chart-1', propertyPath: 'position.x', after: 96 }),
        expect.objectContaining({ targetObjectId: 'chart-2', propertyPath: 'position.y', after: 192 }),
      ]),
    );
  });

  it('builds cross-page standards opportunities for title anchors and semantic colors', () => {
    const reportPath = createTempReport();
    const result: ScoreResult = {
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
      pageCount: 2,
      recommendations: [],
      reportPath,
      scoredAt: '2026-05-31T18:00:00.000Z',
      normalizedFindings: [finding('consistency-finding', 'governance', 'Overview')],
      fixPlan: [fixPlanItem({
        id: 'fix-standards',
        title: 'Normalize cross-page standards',
        sourceFindingIds: ['consistency-finding'],
        resolvedOutcomes: ['Governance consistency'],
        affectedPages: ['Overview', 'Details'],
      })],
      pageScores: [
        pageScore({
          pageName: 'Overview',
          visualMetadata: {
            ...pageScore().visualMetadata!,
            semanticColorMap: [{ semanticKey: 'status:on-track', color: '#ff0000', sourceVisualId: 'title-1', sourcePageName: 'Overview' }],
            visuals: [visual({ visualId: 'title-1', x: 100, y: 180 })],
          },
        }),
        pageScore({
          pageName: 'Details',
          visualMetadata: {
            ...pageScore().visualMetadata!,
            pageName: 'Details',
            semanticColorMap: [{ semanticKey: 'status:on-track', color: '#00ff00', sourceVisualId: 'title-2', sourcePageName: 'Details' }],
            visuals: [visual({ visualId: 'title-2', x: 220, y: 140 })],
          },
        }),
      ],
    };

    const opportunities = buildFixOpportunities(result);

    expect(opportunities).toHaveLength(2);
    expect(opportunities.map((item: { category: string }) => item.category)).toEqual(
      expect.arrayContaining(['crossPageConsistency', 'semanticColor']),
    );
  });

  it('keeps unsupported remediation items advisory by returning no opportunities', () => {
    const reportPath = createTempReport();
    const result: ScoreResult = {
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
      reportPath,
      scoredAt: '2026-05-31T18:00:00.000Z',
      normalizedFindings: [finding('benchmark-finding', 'benchmark')],
      fixPlan: [fixPlanItem({
        id: 'fix-benchmark',
        title: 'Add benchmarks and decision context',
        sourceFindingIds: ['benchmark-finding'],
        resolvedOutcomes: ['Benchmark gap'],
      })],
      pageScores: [pageScore()],
    };

    expect(buildFixOpportunities(result)).toEqual([]);
  });

  it('returns no opportunities when single-page payload has no visual metadata', () => {
    const reportPath = createTempReport();
    const result: ScoreResult = {
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
      reportPath,
      scoredAt: '2026-05-31T18:00:00.000Z',
      scoredPageName: 'Overview',
      normalizedFindings: [finding('story-finding', 'storytelling')],
      fixPlan: [fixPlanItem()],
    };

    expect(buildFixOpportunities(result)).toEqual([]);
  });
});
