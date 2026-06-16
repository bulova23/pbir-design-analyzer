import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { FixOpportunity } from '../analyzer/contracts/scorePanel';
import { applyFixOpportunity, applyFixOpportunityBatch, rollbackFixOpportunity, rollbackFixSession } from '../analyzer/fixes/fixApplyEngine';
import { NodeFixPersistenceService } from '../analyzer/fixes/fixPersistenceService';
import { buildFixOpportunities } from '../analyzer/fixes/fixOpportunityBuilder';
import type { NormalizedFinding, PageScore, ScoreResult, VisualMetadataItem } from '../analyzer/contracts/scorePanel';

function visual(overrides: Partial<VisualMetadataItem> = {}): VisualMetadataItem {
  return {
    visualId: 'chart-1',
    visualType: 'barChart',
    x: 103,
    y: 220,
    width: 390,
    height: 200,
    isHidden: false,
    isNavigationElement: false,
    isDecorative: false,
    isSlicer: false,
    visibleTitleText: 'Chart 1',
    visibleSubtitleText: undefined,
    textBoxText: undefined,
    bestVisibleText: 'Chart 1',
    hasVisibleTitleIntent: false,
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
    id: 'layout-finding',
    title: 'layout issue',
    summary: 'layout issue',
    severity: 'high',
    confidence: 88,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Overview'],
    impactArea: 'layout',
    frameworkImpact: ['Gestalt Principles'],
    recommendation: 'align visuals',
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
  fs.mkdirSync(path.join(overviewPageRoot, 'visuals', 'chart-1'), { recursive: true });
  fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
  fs.writeFileSync(path.join(definitionRoot, 'report.json'), JSON.stringify({ name: 'Sales' }));
  fs.writeFileSync(path.join(definitionRoot, 'pages', 'pages.json'), JSON.stringify({ pageOrder: ['OverviewPage'] }));
  fs.writeFileSync(path.join(overviewPageRoot, 'page.json'), JSON.stringify({ name: 'OverviewPage', displayName: 'Overview' }));
  fs.writeFileSync(path.join(overviewPageRoot, 'visuals', 'chart-1', 'visual.json'), JSON.stringify({
    name: 'chart-1',
    position: { x: 103, y: 220, width: 390, height: 200 },
    title: { text: 'Chart 1' },
    visual: { visualType: 'barChart' },
  }));
  return reportRoot;
}

function createTitleReportRoot(): string {
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
      id: 'fix-layout',
      title: 'Reduce visual density and align layout',
      detail: 'Resolve layout drift.',
      severity: 'high',
      effort: 'low',
      impact: 'high',
      why: 'Improves scanability and alignment.',
      scope: 'page',
      affectedPages: ['Overview'],
      recommendedAction: 'Snap visuals to the shared layout grid.',
      resolvedOutcomes: ['Layout consistency'],
      sourceFindingIds: ['layout-finding'],
    }],
    pageScores: [pageScore()],
  };

  return buildFixOpportunities(result)[0];
}

function titleOpportunity(reportPath: string): FixOpportunity {
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
    scoredAt: '2026-06-06T13:00:00.000Z',
    normalizedFindings: [{
      id: 'story-finding',
      title: 'story issue',
      summary: 'story issue',
      severity: 'high',
      confidence: 90,
      scope: 'page',
      detectionType: 'deterministic',
      affectedPages: ['Overview'],
      impactArea: 'storytelling',
      frameworkImpact: ['Narrative Design'],
      recommendation: 'clarify page purpose',
      sourceKind: 'framework',
      sourceSection: 'issues',
      evidence: [],
    }],
    fixPlan: [{
      id: 'fix-story',
      title: 'Clarify page purpose and narrative framing',
      detail: 'Resolve page purpose ambiguity.',
      severity: 'high',
      effort: 'low',
      impact: 'high',
      why: 'Improves page purpose clarity.',
      scope: 'page',
      affectedPages: ['Overview'],
      recommendedAction: 'Standardize the page title.',
      resolvedOutcomes: ['Story clarity'],
      sourceFindingIds: ['story-finding'],
    }],
    pageScores: [{
      ...pageScore(),
      pageId: 'OverviewPage',
      visualMetadata: {
        ...pageScore().visualMetadata!,
        visuals: [visual({
          visualId: 'title-1',
          visualType: 'textbox',
          x: 100,
          y: 180,
          hasVisibleTitleIntent: true,
          bestVisibleText: 'Overview Title',
          textBoxText: 'Overview Title',
          visibleTitleText: 'Overview Title',
        })],
      },
    }],
  };

  return buildFixOpportunities(result)[0];
}

function listTempFiles(targetFile: string): string[] {
  return fs.readdirSync(path.dirname(targetFile))
    .filter((entry) => entry.startsWith(`${path.basename(targetFile)}.`) && entry.endsWith('.tmp'));
}

describe('fixApplyEngine', () => {
  afterEach(() => {
    for (const entry of fs.readdirSync(os.tmpdir()).filter((name) => name.startsWith('pbir-fix-apply-'))) {
      fs.rmSync(path.join(os.tmpdir(), entry), { recursive: true, force: true });
    }
  });

  it('runs persistence writes through the abstraction and post-write validation hooks', async () => {
    const reportPath = createReportRoot();
    const targetFile = path.join(reportPath, 'definition', 'report.json');
    const persistence = new NodeFixPersistenceService();
    let validationRan = false;

    const writtenVersions = await persistence.writeJsonFilesAtomically(new Map([
      [targetFile, { name: 'Sales', validated: true }],
    ]), {
      validate: [async () => {
        validationRan = true;
        return [];
      }],
    });

    expect(validationRan).toBe(true);
    expect(JSON.parse(fs.readFileSync(targetFile, 'utf8'))).toEqual({ name: 'Sales', validated: true });
    expect(writtenVersions.get(targetFile)?.contentHash).toEqual(expect.any(String));
    expect(listTempFiles(targetFile)).toEqual([]);
  });

  it('applies mutations only after validation and rewrites the target file', async () => {
    const reportPath = createReportRoot();
    const fix = opportunity(reportPath);

    const result = await applyFixOpportunity(fix);

    expect(result).toMatchObject({
      opportunityId: fix.id,
      state: 'Applied',
      appliedMutationCount: fix.mutations.length,
      validationErrors: [],
    });
    const updated = JSON.parse(fs.readFileSync(fix.mutations[0].targetFile, 'utf8')) as { position: { x: number; y: number } };
    expect(updated.position.x).toBe(96);
    expect(updated.position.y).toBe(192);
  });

  it('marks the opportunity stale instead of partially applying when before-values drift', async () => {
    const reportPath = createReportRoot();
    const fix = opportunity(reportPath);
    const visualPath = fix.mutations[0].targetFile;
    const visualJson = JSON.parse(fs.readFileSync(visualPath, 'utf8')) as { position: { x: number } };
    visualJson.position.x = 999;
    fs.writeFileSync(visualPath, JSON.stringify(visualJson), 'utf8');

    const result = await applyFixOpportunity(fix);

    expect(result.state).toBe('Stale');
    expect(result.appliedMutationCount).toBe(0);
  });

  it('detects concurrent file drift even when targeted before-values still match', async () => {
    const reportPath = createReportRoot();
    const fix = opportunity(reportPath);
    const visualPath = fix.mutations[0].targetFile;
    const visualJson = JSON.parse(fs.readFileSync(visualPath, 'utf8')) as Record<string, unknown>;
    visualJson.externalComment = 'changed-after-preview';
    fs.writeFileSync(visualPath, JSON.stringify(visualJson), 'utf8');

    const result = await applyFixOpportunity(fix);

    expect(result.state).toBe('Stale');
    expect(result.validationErrors).toEqual(expect.arrayContaining([expect.stringContaining('target-file-drift')]));
  });

  it('restores original file contents through deterministic rollback', async () => {
    const reportPath = createReportRoot();
    const fix = opportunity(reportPath);
    const original = fs.readFileSync(fix.mutations[0].targetFile, 'utf8');

    const applyResult = await applyFixOpportunity(fix);
    expect(applyResult.state).toBe('Applied');

    const rollbackResult = await rollbackFixOpportunity(fix);
    expect(rollbackResult.state).toBe('RolledBack');
    expect(fs.readFileSync(fix.mutations[0].targetFile, 'utf8')).toBe(original);
  });

  it('applies schema-correct title mutations through storage paths', async () => {
    const reportPath = createTitleReportRoot();
    const fix = titleOpportunity(reportPath);

    const result = await applyFixOpportunity(fix);

    expect(result.state).toBe('Applied');
    const updated = JSON.parse(fs.readFileSync(fix.mutations[0].targetFile, 'utf8')) as {
      position: { x: number; y: number };
      visual: { visualContainerObjects: { title: Array<{ properties: { text: { expr: { Literal: { Value: string } } } } }> } };
    };
    expect(updated.position.x).toBe(24);
    expect(updated.position.y).toBe(24);
    expect(updated.visual.visualContainerObjects.title[0].properties.text.expr.Literal.Value).toBe('\'Overview\'');
  });

  it('marks schema-correct title opportunities stale when the stored title drifts', async () => {
    const reportPath = createTitleReportRoot();
    const fix = titleOpportunity(reportPath);
    const titlePath = fix.mutations[0].targetFile;
    const titleJson = JSON.parse(fs.readFileSync(titlePath, 'utf8')) as {
      visual: { visualContainerObjects: { title: Array<{ properties: { text: { expr: { Literal: { Value: string } } } } }> } };
    };
    titleJson.visual.visualContainerObjects.title[0].properties.text.expr.Literal.Value = '\'Drifted Title\'';
    fs.writeFileSync(titlePath, JSON.stringify(titleJson), 'utf8');

    const result = await applyFixOpportunity(fix);

    expect(result.state).toBe('Stale');
    expect(result.appliedMutationCount).toBe(0);
  });

  it('applies compatible opportunities in deterministic order', async () => {
    const reportPath = createReportRoot();
    const first = opportunity(reportPath);
    const second = {
      ...opportunity(reportPath),
      id: 'fix-z',
      title: 'Secondary layout alignment',
      targetObjectIds: ['chart-2'],
      mutations: [{
        ...opportunity(reportPath).mutations[0],
        id: 'mutation-z',
        targetObjectId: 'chart-2',
        targetFile: first.mutations[0].targetFile.replace('chart-1', 'chart-2'),
        targetFileVersion: undefined,
        propertyPath: 'position.x',
        before: 80,
        after: 24,
      }],
      rollbackPlan: {
        id: 'rollback-fix-z',
        fixOpportunityId: 'fix-z',
        fileBackups: [{
          targetFile: first.mutations[0].targetFile.replace('chart-1', 'chart-2'),
          beforeContent: '{"name":"chart-2","position":{"x":80,"y":220,"width":390,"height":200},"title":{"text":"Chart 2"},"visual":{"visualType":"barChart"}}',
        }],
        reverseMutations: [],
      },
    } satisfies FixOpportunity;

    fs.mkdirSync(path.dirname(second.mutations[0].targetFile), { recursive: true });
    fs.writeFileSync(second.mutations[0].targetFile, second.rollbackPlan.fileBackups[0].beforeContent, 'utf8');

    const result = await applyFixOpportunityBatch([second, first], '2026-06-01T22:20:00.000Z');

    expect(result.state).toBe('Applied');
    expect(result.applyOrder).toEqual([first.id, second.id]);
    expect(result.appliedMutationCount).toBe(first.mutations.length + second.mutations.length);
    expect(result.session?.rollbackAvailable).toBe(true);
  });

  it('validates the full batch before applying any mutation', async () => {
    const reportPath = createReportRoot();
    const first = opportunity(reportPath);
    const second = {
      ...opportunity(reportPath),
      id: 'fix-b',
      mutations: [{
        ...opportunity(reportPath).mutations[0],
        id: 'mutation-b',
        targetObjectId: 'chart-2',
        targetFile: first.mutations[0].targetFile.replace('chart-1', 'chart-2'),
        targetFileVersion: undefined,
        before: 80,
        after: 24,
      }],
      rollbackPlan: {
        id: 'rollback-fix-b',
        fixOpportunityId: 'fix-b',
        fileBackups: [{
          targetFile: first.mutations[0].targetFile.replace('chart-1', 'chart-2'),
          beforeContent: '{"name":"chart-2","position":{"x":999,"y":220,"width":390,"height":200},"title":{"text":"Chart 2"},"visual":{"visualType":"barChart"}}',
        }],
        reverseMutations: [],
      },
    } satisfies FixOpportunity;

    fs.mkdirSync(path.dirname(second.mutations[0].targetFile), { recursive: true });
    fs.writeFileSync(second.mutations[0].targetFile, second.rollbackPlan.fileBackups[0].beforeContent, 'utf8');
    const originalFirst = fs.readFileSync(first.mutations[0].targetFile, 'utf8');

    const result = await applyFixOpportunityBatch([first, second], '2026-06-01T22:20:00.000Z');

    expect(result.state).toBe('Stale');
    expect(result.appliedMutationCount).toBe(0);
    expect(fs.readFileSync(first.mutations[0].targetFile, 'utf8')).toBe(originalFirst);
  });

  it('blocks the entire batch when one selected opportunity conflicts', async () => {
    const reportPath = createReportRoot();
    const first = opportunity(reportPath);
    const second = {
      ...opportunity(reportPath),
      id: 'fix-b',
    } satisfies FixOpportunity;

    const original = fs.readFileSync(first.mutations[0].targetFile, 'utf8');
    const result = await applyFixOpportunityBatch([first, second], '2026-06-01T22:20:00.000Z');

    expect(result.state).toBe('FailedValidation');
    expect(result.validationErrors).toEqual(expect.arrayContaining([expect.stringContaining('overlappingMutation')]));
    expect(fs.readFileSync(first.mutations[0].targetFile, 'utf8')).toBe(original);
  });

  it('restores grouped apply changes through session rollback', async () => {
    const reportPath = createReportRoot();
    const first = opportunity(reportPath);
    const second = {
      ...opportunity(reportPath),
      id: 'fix-z',
      targetObjectIds: ['chart-2'],
      mutations: [{
        ...opportunity(reportPath).mutations[0],
        id: 'mutation-z',
        targetObjectId: 'chart-2',
        targetFile: first.mutations[0].targetFile.replace('chart-1', 'chart-2'),
        targetFileVersion: undefined,
        propertyPath: 'position.x',
        before: 80,
        after: 24,
      }],
      rollbackPlan: {
        id: 'rollback-fix-z',
        fixOpportunityId: 'fix-z',
        fileBackups: [{
          targetFile: first.mutations[0].targetFile.replace('chart-1', 'chart-2'),
          beforeContent: '{"name":"chart-2","position":{"x":80,"y":220,"width":390,"height":200},"title":{"text":"Chart 2"},"visual":{"visualType":"barChart"}}',
        }],
        reverseMutations: [],
      },
    } satisfies FixOpportunity;

    fs.mkdirSync(path.dirname(second.mutations[0].targetFile), { recursive: true });
    fs.writeFileSync(second.mutations[0].targetFile, second.rollbackPlan.fileBackups[0].beforeContent, 'utf8');

    const originalFirst = fs.readFileSync(first.mutations[0].targetFile, 'utf8');
    const originalSecond = fs.readFileSync(second.mutations[0].targetFile, 'utf8');
    const applyResult = await applyFixOpportunityBatch([first, second], '2026-06-01T22:20:00.000Z');

    expect(applyResult.session).toBeDefined();

    const rollbackResult = await rollbackFixSession(applyResult.session!, [first, second], '2026-06-01T22:21:00.000Z');

    expect(rollbackResult.state).toBe('RolledBack');
    expect(fs.readFileSync(first.mutations[0].targetFile, 'utf8')).toBe(originalFirst);
    expect(fs.readFileSync(second.mutations[0].targetFile, 'utf8')).toBe(originalSecond);
  });

  it('detects rollback drift instead of overwriting unexpected external edits', async () => {
    const reportPath = createReportRoot();
    const fix = opportunity(reportPath);
    const targetFile = fix.mutations[0].targetFile;

    const applyResult = await applyFixOpportunity(fix);
    expect(applyResult.state).toBe('Applied');

    const currentJson = JSON.parse(fs.readFileSync(targetFile, 'utf8')) as Record<string, unknown>;
    currentJson.externalComment = 'edited-after-apply';
    fs.writeFileSync(targetFile, JSON.stringify(currentJson), 'utf8');

    const rollbackResult = await rollbackFixOpportunity(fix);

    expect(rollbackResult.state).toBe('FailedValidation');
    expect(rollbackResult.validationErrors).toEqual(expect.arrayContaining([expect.stringContaining('rollback-conflict')]));
    expect(JSON.parse(fs.readFileSync(targetFile, 'utf8'))).toMatchObject({
      externalComment: 'edited-after-apply',
    });
  });

  it('rolls back already-staged file changes when an atomic batch persistence step fails', async () => {
    const reportPath = createReportRoot();
    const first = opportunity(reportPath);
    const second = {
      ...opportunity(reportPath),
      id: 'fix-z',
      targetObjectIds: ['chart-2'],
      mutations: [{
        ...opportunity(reportPath).mutations[0],
        id: 'mutation-z',
        targetObjectId: 'chart-2',
        targetFile: first.mutations[0].targetFile.replace('chart-1', 'chart-2'),
        targetFileVersion: undefined,
        propertyPath: 'position.x',
        storagePath: ['position', 'x'],
        before: 80,
        after: 24,
      }],
      rollbackPlan: {
        id: 'rollback-fix-z',
        fixOpportunityId: 'fix-z',
        fileBackups: [{
          targetFile: first.mutations[0].targetFile.replace('chart-1', 'chart-2'),
          beforeContent: '{"name":"chart-2","position":{"x":80,"y":220,"width":390,"height":200},"title":{"text":"Chart 2"},"visual":{"visualType":"barChart"}}',
        }],
        reverseMutations: [],
      },
    } satisfies FixOpportunity;

    fs.mkdirSync(path.dirname(second.mutations[0].targetFile), { recursive: true });
    fs.writeFileSync(second.mutations[0].targetFile, second.rollbackPlan.fileBackups[0].beforeContent, 'utf8');
    const originalFirst = fs.readFileSync(first.mutations[0].targetFile, 'utf8');
    const originalSecond = fs.readFileSync(second.mutations[0].targetFile, 'utf8');
    const secondDir = path.dirname(second.mutations[0].targetFile);
    fs.chmodSync(second.mutations[0].targetFile, 0o400);
    fs.chmodSync(secondDir, 0o500);

    try {
      const result = await applyFixOpportunityBatch([first, second], '2026-06-06T14:00:00.000Z');
      expect(result.state).toBe('FailedValidation');
      expect(result.appliedMutationCount).toBe(0);
      expect(fs.readFileSync(first.mutations[0].targetFile, 'utf8')).toBe(originalFirst);
      expect(fs.readFileSync(second.mutations[0].targetFile, 'utf8')).toBe(originalSecond);
      expect(listTempFiles(first.mutations[0].targetFile)).toEqual([]);
      expect(listTempFiles(second.mutations[0].targetFile)).toEqual([]);
    } finally {
      fs.chmodSync(second.mutations[0].targetFile, 0o600);
      fs.chmodSync(secondDir, 0o700);
    }
  });
});
