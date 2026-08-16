import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { PageScore, ScoreResult, VisualMetadataItem } from '../analyzer/contracts/scorePanel';
import { planMutationsForCategory, resolveReportDefinitionPaths } from '../analyzer/fixes/fixMutationPlanner';

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
    pageId: 'OverviewPage',
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

function createTempReport(): string {
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-fix-mutation-planner-'));
  const reportRoot = path.join(tempDir, 'Sales.Report');
  const definitionRoot = path.join(reportRoot, 'definition');
  const overviewPageRoot = path.join(definitionRoot, 'pages', 'OverviewPage');
  const detailPageRoot = path.join(definitionRoot, 'pages', 'DetailPage');
  fs.mkdirSync(path.join(overviewPageRoot, 'visuals', 'title-1'), { recursive: true });
  fs.mkdirSync(path.join(detailPageRoot, 'visuals', 'title-2'), { recursive: true });
  fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
  fs.writeFileSync(path.join(definitionRoot, 'report.json'), JSON.stringify({ name: 'Sales' }));
  fs.writeFileSync(path.join(definitionRoot, 'pages', 'pages.json'), JSON.stringify({ pageOrder: ['OverviewPage', 'DetailPage'] }));
  fs.writeFileSync(path.join(overviewPageRoot, 'page.json'), JSON.stringify({ name: 'OverviewPage', displayName: 'Overview' }));
  fs.writeFileSync(path.join(detailPageRoot, 'page.json'), JSON.stringify({ name: 'DetailPage', displayName: 'Details' }));
  fs.writeFileSync(path.join(overviewPageRoot, 'visuals', 'title-1', 'visual.json'), JSON.stringify({
    name: 'title-1',
    position: { x: 100, y: 180, width: 400, height: 48 },
    visual: {
      visualType: 'textbox',
      visualContainerObjects: {
        title: [{
          properties: {
            text: {
              expr: {
                Literal: {
                  Value: '\'Overview Title\'',
                },
              },
            },
          },
        }],
      },
    },
  }));
  fs.writeFileSync(path.join(detailPageRoot, 'visuals', 'title-2', 'visual.json'), JSON.stringify({
    name: 'title-2',
    position: { x: 220, y: 140, width: 420, height: 48 },
    visual: {
      visualType: 'textbox',
      visualContainerObjects: {
        title: [{
          properties: {
            text: {
              expr: {
                Literal: {
                  Value: '\'Details Title\'',
                },
              },
            },
          },
        }],
      },
    },
    background: { color: '#00ff00' },
  }));
  return reportRoot;
}

function createTempReportWithoutPagesJson(): string {
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-fix-mutation-planner-'));
  const reportRoot = path.join(tempDir, 'Sales.Report');
  const definitionRoot = path.join(reportRoot, 'definition');
  const zPageRoot = path.join(definitionRoot, 'pages', 'ZetaPage');
  const aPageRoot = path.join(definitionRoot, 'pages', 'AlphaPage');
  fs.mkdirSync(path.join(zPageRoot, 'visuals', 'visual-z'), { recursive: true });
  fs.mkdirSync(path.join(aPageRoot, 'visuals', 'visual-a'), { recursive: true });
  fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
  fs.writeFileSync(path.join(definitionRoot, 'report.json'), JSON.stringify({ name: 'Sales' }));
  fs.writeFileSync(path.join(zPageRoot, 'page.json'), JSON.stringify({ name: 'ZetaPage', displayName: 'Zeta' }));
  fs.writeFileSync(path.join(aPageRoot, 'page.json'), JSON.stringify({ name: 'AlphaPage', displayName: 'Alpha' }));
  fs.writeFileSync(path.join(zPageRoot, 'visuals', 'visual-z', 'visual.json'), JSON.stringify({ name: 'visual-z', visual: { visualType: 'textbox' } }));
  fs.writeFileSync(path.join(aPageRoot, 'visuals', 'visual-a', 'visual.json'), JSON.stringify({ name: 'visual-a', visual: { visualType: 'textbox' } }));
  return reportRoot;
}

function createTempReportWithDuplicateDisplayNames(): string {
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-fix-mutation-planner-'));
  const reportRoot = path.join(tempDir, 'Sales.Report');
  const definitionRoot = path.join(reportRoot, 'definition');
  const alphaPageRoot = path.join(definitionRoot, 'pages', 'AlphaPage');
  const betaPageRoot = path.join(definitionRoot, 'pages', 'BetaPage');
  fs.mkdirSync(path.join(alphaPageRoot, 'visuals', 'title-a'), { recursive: true });
  fs.mkdirSync(path.join(betaPageRoot, 'visuals', 'title-b'), { recursive: true });
  fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
  fs.writeFileSync(path.join(definitionRoot, 'report.json'), JSON.stringify({ name: 'Sales' }));
  fs.writeFileSync(path.join(definitionRoot, 'pages', 'pages.json'), JSON.stringify({ pageOrder: ['AlphaPage', 'BetaPage'] }));
  fs.writeFileSync(path.join(alphaPageRoot, 'page.json'), JSON.stringify({ name: 'AlphaPage', displayName: 'Overview' }));
  fs.writeFileSync(path.join(betaPageRoot, 'page.json'), JSON.stringify({ name: 'BetaPage', displayName: 'Overview' }));
  fs.writeFileSync(path.join(alphaPageRoot, 'visuals', 'title-a', 'visual.json'), JSON.stringify({
    name: 'title-a',
    position: { x: 100, y: 180, width: 400, height: 48 },
    visual: {
      visualType: 'textbox',
      visualContainerObjects: {
        title: [{
          properties: {
            text: {
              expr: {
                Literal: {
                  Value: '\'Alpha Overview\'',
                },
              },
            },
          },
        }],
      },
    },
  }));
  fs.writeFileSync(path.join(betaPageRoot, 'visuals', 'title-b', 'visual.json'), JSON.stringify({
    name: 'title-b',
    position: { x: 220, y: 140, width: 420, height: 48 },
    visual: {
      visualType: 'textbox',
      visualContainerObjects: {
        title: [{
          properties: {
            text: {
              expr: {
                Literal: {
                  Value: '\'Beta Overview\'',
                },
              },
            },
          },
        }],
      },
    },
  }));
  return reportRoot;
}

function result(reportPath: string): ScoreResult {
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
    pageCount: 2,
    recommendations: [],
    reportPath,
    scoredAt: '2026-06-05T19:00:00.000Z',
    pageScores: [
      pageScore(),
      pageScore({
        pageId: 'DetailPage',
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
}

describe('fixMutationPlanner', () => {
  afterEach(() => {
    for (const entry of fs.readdirSync(os.tmpdir()).filter((name) => name.startsWith('pbir-fix-mutation-planner-'))) {
      fs.rmSync(path.join(os.tmpdir(), entry), { recursive: true, force: true });
    }
  });

  it('resolves pages by stable PBIR page name', () => {
    const reportPath = createTempReport();

    const paths = resolveReportDefinitionPaths(reportPath);

    expect(paths?.pages).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ pageId: 'OverviewPage', pageName: 'OverviewPage' }),
        expect.objectContaining({ pageId: 'DetailPage', pageName: 'DetailPage' }),
      ]),
    );
  });

  it('sorts fallback page and visual enumeration deterministically when pages.json is absent', () => {
    const reportPath = createTempReportWithoutPagesJson();

    const paths = resolveReportDefinitionPaths(reportPath);

    expect(paths?.pages.map((page) => page.pageId)).toEqual(['AlphaPage', 'ZetaPage']);
    expect([...paths!.pages[0].visualFiles.keys()]).toEqual(['visual-a']);
    expect([...paths!.pages[1].visualFiles.keys()]).toEqual(['visual-z']);
  });

  it('fails closed when display-name targeting is ambiguous across duplicate pages', () => {
    const reportPath = createTempReportWithDuplicateDisplayNames();
    const duplicateResult: ScoreResult = {
      ...result(reportPath),
      pageScores: [
        pageScore({
          pageId: 'AlphaPage',
          pageName: 'Overview',
          visualMetadata: {
            ...pageScore().visualMetadata!,
            pageName: 'Overview',
            visuals: [visual({ visualId: 'title-a', bestVisibleText: 'Alpha Overview', textBoxText: 'Alpha Overview', visibleTitleText: 'Alpha Overview' })],
          },
        }),
        pageScore({
          pageId: 'BetaPage',
          pageName: 'Overview',
          visualMetadata: {
            ...pageScore().visualMetadata!,
            pageName: 'Overview',
            visuals: [visual({ visualId: 'title-b', bestVisibleText: 'Beta Overview', textBoxText: 'Beta Overview', visibleTitleText: 'Beta Overview' })],
          },
        }),
      ],
    };

    const mutations = planMutationsForCategory({
      category: 'title',
      result: duplicateResult,
      pageName: 'Overview',
      affectedPages: ['Overview'],
    });

    expect(mutations).toEqual([]);
  });

  it('plans title mutations using stable page identity and schema-correct title paths', () => {
    const reportPath = createTempReport();

    const mutations = planMutationsForCategory({
      category: 'title',
      result: result(reportPath),
      pageName: 'Overview',
      affectedPages: ['Overview'],
    });

    expect(mutations).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          pageName: 'Overview',
          targetObjectId: 'title-1',
          propertyPath: 'position.y',
          before: 180,
          after: 24,
          storagePath: ['position', 'y'],
          targetFileVersion: expect.objectContaining({
            contentHash: expect.any(String),
            size: expect.any(Number),
            modifiedTimeMs: expect.any(Number),
          }),
        }),
        expect.objectContaining({
          pageName: 'Overview',
          targetObjectId: 'title-1',
          propertyPath: 'position.x',
          before: 100,
          after: 24,
          storagePath: ['position', 'x'],
          targetFileVersion: expect.objectContaining({
            contentHash: expect.any(String),
            size: expect.any(Number),
            modifiedTimeMs: expect.any(Number),
          }),
        }),
        expect.objectContaining({
          pageName: 'Overview',
          targetObjectId: 'title-1',
          propertyPath: 'title.text',
          before: 'Overview Title',
          after: 'Overview',
          storagePath: ['visual', 'visualContainerObjects', 'title', 0, 'properties', 'text', 'expr', 'Literal', 'Value'],
          targetFileVersion: expect.objectContaining({
            contentHash: expect.any(String),
            size: expect.any(Number),
            modifiedTimeMs: expect.any(Number),
          }),
        }),
      ]),
    );
    expect(mutations.every((mutation) => mutation.targetFileVersion?.contentHash)).toBe(true);
    expect(new Set(mutations.map((mutation) => mutation.targetFileVersion?.contentHash)).size).toBe(1);
  });

  it('keeps semantic color mutations disabled until schema-correct support exists', () => {
    const reportPath = createTempReport();

    const mutations = planMutationsForCategory({
      category: 'semanticColor',
      result: result(reportPath),
      affectedPages: ['Overview', 'Details'],
    });

    expect(mutations).toEqual([]);
  });
});
