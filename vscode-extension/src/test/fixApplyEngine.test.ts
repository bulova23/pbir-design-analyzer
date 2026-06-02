import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { FixOpportunity } from '../analyzer/contracts/scorePanel';
import { applyFixOpportunity, rollbackFixOpportunity } from '../analyzer/fixes/fixApplyEngine';
import { buildFixOpportunities } from '../analyzer/fixes/fixOpportunityBuilder';
import type { NormalizedFinding, PageScore, ScoreResult, VisualMetadataItem } from '../analyzer/contracts/scorePanel';

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

function pageScore(): PageScore {
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
  };
}

function finding(): NormalizedFinding {
  return {
    id: 'story-finding',
    title: 'storytelling issue',
    summary: 'story issue',
    severity: 'high',
    confidence: 88,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Overview'],
    impactArea: 'storytelling',
    frameworkImpact: ['Narrative Design'],
    recommendation: 'clarify title',
    sourceKind: 'framework',
    sourceSection: 'issues',
    evidence: [],
  };
}

function createReportRoot(): string {
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-fix-apply-'));
  const reportRoot = path.join(tempDir, 'Sales.Report');
  const definitionRoot = path.join(reportRoot, 'definition');
  const overviewPageRoot = path.join(definitionRoot, 'pages', 'OverviewPage');
  fs.mkdirSync(path.join(overviewPageRoot, 'visuals', 'title-1'), { recursive: true });
  fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
  fs.writeFileSync(path.join(definitionRoot, 'report.json'), JSON.stringify({ name: 'Sales' }));
  fs.writeFileSync(path.join(definitionRoot, 'pages', 'pages.json'), JSON.stringify({ pageOrder: ['OverviewPage'] }));
  fs.writeFileSync(path.join(overviewPageRoot, 'page.json'), JSON.stringify({ name: 'OverviewPage', displayName: 'Overview' }));
  fs.writeFileSync(path.join(overviewPageRoot, 'visuals', 'title-1', 'visual.json'), JSON.stringify({
    name: 'title-1',
    position: { x: 100, y: 180, width: 400, height: 48 },
    title: { text: 'Overview Title' },
    visual: { visualType: 'textbox' },
  }));
  return reportRoot;
}

function opportunity(reportPath: string): FixOpportunity {
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
    normalizedFindings: [finding()],
    fixPlan: [{
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
    }],
    pageScores: [pageScore()],
  };

  return buildFixOpportunities(result)[0];
}

describe('fixApplyEngine', () => {
  afterEach(() => {
    for (const entry of fs.readdirSync(os.tmpdir()).filter((name) => name.startsWith('pbir-fix-apply-'))) {
      fs.rmSync(path.join(os.tmpdir(), entry), { recursive: true, force: true });
    }
  });

  it('applies mutations only after validation and rewrites the target file', () => {
    const reportPath = createReportRoot();
    const fix = opportunity(reportPath);

    const result = applyFixOpportunity(fix);

    expect(result).toMatchObject({
      opportunityId: fix.id,
      state: 'Applied',
      appliedMutationCount: fix.mutations.length,
      validationErrors: [],
    });
    const updated = JSON.parse(fs.readFileSync(fix.mutations[0].targetFile, 'utf8')) as { position: { y: number } };
    expect(updated.position.y).toBe(24);
  });

  it('marks the opportunity stale instead of partially applying when before-values drift', () => {
    const reportPath = createReportRoot();
    const fix = opportunity(reportPath);
    const visualPath = fix.mutations[0].targetFile;
    const visualJson = JSON.parse(fs.readFileSync(visualPath, 'utf8')) as { position: { y: number } };
    visualJson.position.y = 999;
    fs.writeFileSync(visualPath, JSON.stringify(visualJson), 'utf8');

    const result = applyFixOpportunity(fix);

    expect(result.state).toBe('Stale');
    expect(result.appliedMutationCount).toBe(0);
  });

  it('restores original file contents through deterministic rollback', () => {
    const reportPath = createReportRoot();
    const fix = opportunity(reportPath);
    const original = fs.readFileSync(fix.mutations[0].targetFile, 'utf8');

    const applyResult = applyFixOpportunity(fix);
    expect(applyResult.state).toBe('Applied');

    const rollbackResult = rollbackFixOpportunity(fix);
    expect(rollbackResult.state).toBe('RolledBack');
    expect(fs.readFileSync(fix.mutations[0].targetFile, 'utf8')).toBe(original);
  });
});
