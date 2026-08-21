import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ScoreResult } from '../analyzer/contracts/scorePanel';
import {
  buildReportFingerprint,
  buildScoreDeterminismDiagnostics,
  normalizeFingerprintPath,
} from '../analyzer/score/scoreDiagnostics';

describe('score diagnostics', () => {
  let tempDir: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-score-diagnostics-'));
    const reportRoot = path.join(tempDir, 'Sales.Report');
    fs.mkdirSync(path.join(reportRoot, 'definition', 'pages', 'Page1'), { recursive: true });
    fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
    fs.writeFileSync(path.join(reportRoot, 'definition', 'report.json'), '{"name":"Sales"}');
    fs.writeFileSync(
      path.join(reportRoot, 'definition', 'pages', 'Page1', 'page.json'),
      '{"displayName":"Overview","visuals":[{"id":"v1","type":"barChart","x":0,"y":0,"width":100,"height":100}]}',
    );
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('normalizes Windows-style and POSIX-style fingerprint paths', () => {
    expect(normalizeFingerprintPath('definition\\pages\\Page1\\page.json')).toBe('definition/pages/Page1/page.json');
    expect(normalizeFingerprintPath('definition/pages/Page1/page.json')).toBe('definition/pages/Page1/page.json');
  });

  it('builds a stable report fingerprint regardless of the selected entry path', () => {
    const reportRoot = path.join(tempDir, 'Sales.Report');
    const fromReportRoot = buildReportFingerprint(reportRoot);
    const fromReportJson = buildReportFingerprint(path.join(reportRoot, 'definition', 'report.json'));

    expect(fromReportRoot.fingerprint).toBe(fromReportJson.fingerprint);
    expect(fromReportRoot.sourceFiles.map((entry) => entry.relativePath)).toEqual([
      'definition.pbir',
      'definition/pages/Page1/page.json',
      'definition/report.json',
    ]);
  });

  it('excludes generated and cache files from the fingerprint', () => {
    const reportRoot = path.join(tempDir, 'Sales.Report');
    const before = buildReportFingerprint(reportRoot);

    fs.mkdirSync(path.join(reportRoot, 'cache'), { recursive: true });
    fs.writeFileSync(path.join(reportRoot, 'cache', 'result.json'), '{"generated":true}');

    const after = buildReportFingerprint(reportRoot);
    expect(after.fingerprint).toBe(before.fingerprint);
  });

  it('captures deterministic page and finding snapshots', () => {
    const reportRoot = path.join(tempDir, 'Sales.Report');
    const diagnostics = buildScoreDeterminismDiagnostics({
      reportPath: reportRoot,
      extensionVersion: '0.5.0',
      backendVersion: '1.0.0',
      result: {
        gestaltScore: 80,
        cognitiveLoadScore: 80,
        dataInkScore: 80,
        accessibilityScore: 80,
        visualBestPracticesScore: 80,
        stephenFewScore: 80,
        enterpriseGovernanceScore: 80,
        tufteScore: 80,
        graphicalPerceptionScore: 80,
        densityScore: 80,
        narrativeScore: 80,
        compositeScore: 80,
        feedback: {},
        pageCount: 1,
        recommendations: [],
        reportPath: reportRoot,
        scoredAt: '2026-06-05T00:00:00.000Z',
        frameworkWeights: {
          cognitiveLoad: 20,
          gestalt: 30,
        },
        analysisContext: {
          surfaceType: 'pbirReport',
          analyzerType: 'fabricAppReadiness',
          analyzerProfile: 'migrationReadiness',
          surfaceDisplayName: 'Sales',
          sourceLocation: reportRoot,
          availableAnalyzerTypes: ['pbirDesignReview', 'fabricAppReadiness'],
          availableAnalyzerProfiles: ['default', 'migrationReadiness'],
        },
        readinessAssessment: {
          overallReadinessScore: 78,
          readinessBand: 'strongCandidate',
          migrationSummary: 'Good candidate.',
          candidatePages: ['Overview'],
          blockers: [],
          unsupportedPatterns: [],
          redesignRequiredAreas: [],
          recommendedNextActions: [],
          estimatedRedesignEffort: 'low',
          dimensionScores: {
            layoutPortability: 80,
            interactionPortability: 80,
            narrativePortability: 80,
            semanticModelSuitability: 80,
            navigationPortability: 80,
            governancePortability: 80,
            accessibilityPortability: 80,
            visualizationAsCodeOpportunity: 80,
          },
          pageAssessments: [],
          evidence: [],
          governanceSignals: [],
        },
        normalizedFindings: [
          {
            id: 'z-finding',
            title: 'Z finding',
            summary: 'Later finding.',
            severity: 'medium',
            confidence: 70,
            scope: 'page',
            detectionType: 'deterministic',
            affectedPages: ['Overview'],
            impactArea: 'layout',
            frameworkImpact: ['Gestalt Principles'],
            recommendation: 'Fix it.',
            sourceKind: 'frameworkFeedback',
            sourceSection: 'issues',
            evidence: [{ kind: 'framework', label: 'Gestalt Principles' }],
          },
          {
            id: 'a-finding',
            title: 'A finding',
            summary: 'Earlier finding.',
            severity: 'high',
            confidence: 90,
            scope: 'page',
            detectionType: 'deterministic',
            affectedPages: ['Overview'],
            impactArea: 'layout',
            frameworkImpact: ['Gestalt Principles'],
            recommendation: 'Fix first.',
            sourceKind: 'frameworkFeedback',
            sourceSection: 'issues',
            evidence: [{ kind: 'framework', label: 'Gestalt Principles' }],
          },
        ],
        pageScores: [
          {
            pageName: 'Overview',
            gestaltScore: 80,
            cognitiveLoadScore: 80,
            dataInkScore: 80,
            accessibilityScore: 80,
            visualBestPracticesScore: 80,
            stephenFewScore: 80,
            enterpriseGovernanceScore: 80,
            tufteScore: 80,
            graphicalPerceptionScore: 80,
            densityScore: 80,
            narrativeScore: 80,
            compositeScore: 80,
            feedback: {},
            recommendations: [],
            frameworkWeights: {
              cognitiveLoad: 20,
              gestalt: 30,
            },
            visualMetadata: {
              pageName: 'Overview',
              semanticColorMap: [],
              visualCount: 2,
              visibleTitleVisualCount: 1,
              textVisualCount: 1,
              slicerCount: 0,
              legendVisualCount: 0,
              axisLabelVisualCount: 0,
              dataLabelVisualCount: 0,
              formattedVisualCount: 0,
              visuals: [
                { visualId: 'title-1', visualType: 'textbox', x: 0, y: 0, width: 100, height: 20, isHidden: false, isNavigationElement: false, isDecorative: false, isSlicer: false, categoryHints: [], valueHints: [], seriesHints: [], measureHints: [], semanticColors: [], hasVisibleTitleIntent: true },
                { visualId: 'chart-1', visualType: 'barChart', x: 0, y: 40, width: 200, height: 100, isHidden: false, isNavigationElement: false, isDecorative: false, isSlicer: false, categoryHints: [], valueHints: [], seriesHints: [], measureHints: [], semanticColors: [], hasVisibleTitleIntent: false },
              ],
            },
          },
        ],
      } as ScoreResult,
    });

    expect(diagnostics.pageProcessingOrder).toEqual(['Overview']);
    expect(diagnostics.resultSource).toBe('freshAnalysis');
    expect(diagnostics.cachedPayload).toBe(false);
    expect(diagnostics.findings.map((finding) => finding.id)).toEqual(['a-finding', 'z-finding']);
    expect(diagnostics.frameworkWeights).toEqual({
      cognitiveLoad: 20,
      gestalt: 30,
    });
    expect(diagnostics.overallFrameworkScores).toEqual({
      gestaltScore: 80,
      cognitiveLoadScore: 80,
      dataInkScore: 80,
      accessibilityScore: 80,
      visualBestPracticesScore: 80,
      stephenFewScore: 80,
      enterpriseGovernanceScore: 80,
      tufteScore: 80,
      graphicalPerceptionScore: 80,
      densityScore: 80,
      narrativeScore: 80,
      compositeScore: 80,
    });
    expect(diagnostics.pageSnapshots[0]).toMatchObject({
      frameworkWeights: {
        cognitiveLoad: 20,
        gestalt: 30,
      },
      frameworkScores: {
        gestaltScore: 80,
        cognitiveLoadScore: 80,
        dataInkScore: 80,
        accessibilityScore: 80,
        visualBestPracticesScore: 80,
        stephenFewScore: 80,
        enterpriseGovernanceScore: 80,
        tufteScore: 80,
        graphicalPerceptionScore: 80,
        densityScore: 80,
        narrativeScore: 80,
        compositeScore: 80,
      },
      visualCount: 2,
      navigationVisualCount: 0,
      hiddenVisualCount: 0,
      visibleTitleVisualCount: 1,
    });
    expect(diagnostics.reportFingerprint.fingerprint).toHaveLength(64);
  });
});
