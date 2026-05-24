import {
  buildGovernanceExportData,
  exportAsJson,
  exportAsMarkdown,
  type GovernanceCheckResult,
} from '../analyzer/score/governanceExport';
import type { ScoreResult } from '../analyzer/contracts/scorePanel';

function makeScoreResult(overrides: Partial<ScoreResult> = {}): ScoreResult {
  return {
    reportPath: '/workspace/MyReport.Report',
    scoredAt: '2026-05-23T10:00:00Z',
    compositeScore: 78.5,
    gestaltScore: 80,
    cognitiveLoadScore: 75,
    dataInkScore: 82,
    accessibilityScore: 70,
    visualBestPracticesScore: 77,
    stephenFewScore: 79,
    enterpriseGovernanceScore: 85,
    tufteScore: 76,
    graphicalPerceptionScore: 73,
    densityScore: 81,
    narrativeScore: 74,
    feedback: {},
    pageCount: 3,
    recommendations: [],
    ...overrides,
  };
}

function makeGovernanceResult(overrides: Partial<GovernanceCheckResult> = {}): GovernanceCheckResult {
  return {
    policyState: 'active',
    blocked: false,
    evaluatedScore: 78.5,
    requiredThreshold: 70,
    reasons: [],
    ...overrides,
  };
}

describe('buildGovernanceExportData', () => {
  it('maps all 11 framework scores to labeled keys', () => {
    const data = buildGovernanceExportData(makeScoreResult(), makeGovernanceResult());

    expect(Object.keys(data.frameworkScores)).toHaveLength(11);
    expect(data.frameworkScores['Gestalt']).toBe(80);
    expect(data.frameworkScores['Narrative']).toBe(74);
    expect(data.frameworkScores['Enterprise Governance']).toBe(85);
  });

  it('rounds framework scores to one decimal', () => {
    const data = buildGovernanceExportData(
      makeScoreResult({ gestaltScore: 78.567 }),
      makeGovernanceResult(),
    );

    expect(data.frameworkScores['Gestalt']).toBe(78.6);
  });

  it('marks governance as passed when not blocked', () => {
    const data = buildGovernanceExportData(
      makeScoreResult(),
      makeGovernanceResult({ blocked: false }),
    );

    expect(data.governance.passed).toBe(true);
    expect(data.governance.reasons).toEqual([]);
  });

  it('marks governance as failed and carries reasons when blocked', () => {
    const data = buildGovernanceExportData(
      makeScoreResult(),
      makeGovernanceResult({
        blocked: true,
        reasons: ['Score below threshold', 'Missing approved theme'],
      }),
    );

    expect(data.governance.passed).toBe(false);
    expect(data.governance.reasons).toHaveLength(2);
    expect(data.governance.reasons[0]).toBe('Score below threshold');
  });

  it('preserves reportPath and scoredAt from score result', () => {
    const data = buildGovernanceExportData(makeScoreResult(), makeGovernanceResult());

    expect(data.reportPath).toBe('/workspace/MyReport.Report');
    expect(data.scoredAt).toBe('2026-05-23T10:00:00Z');
  });
});

describe('exportAsJson', () => {
  it('produces valid JSON with expected top-level keys', () => {
    const data = buildGovernanceExportData(makeScoreResult(), makeGovernanceResult());
    const json = JSON.parse(exportAsJson(data)) as Record<string, unknown>;

    expect(json).toHaveProperty('reportPath');
    expect(json).toHaveProperty('scoredAt');
    expect(json).toHaveProperty('compositeScore');
    expect(json).toHaveProperty('frameworkScores');
    expect(json).toHaveProperty('governance');
  });

  it('governance block includes passed, evaluatedScore, requiredThreshold, reasons', () => {
    const data = buildGovernanceExportData(makeScoreResult(), makeGovernanceResult());
    const json = JSON.parse(exportAsJson(data)) as {
      governance: Record<string, unknown>;
    };

    expect(json.governance).toHaveProperty('passed', true);
    expect(json.governance).toHaveProperty('evaluatedScore', 78.5);
    expect(json.governance).toHaveProperty('requiredThreshold', 70);
    expect(json.governance).toHaveProperty('reasons');
  });
});

describe('exportAsMarkdown', () => {
  it('includes the report path and scored timestamp', () => {
    const data = buildGovernanceExportData(makeScoreResult(), makeGovernanceResult());
    const md = exportAsMarkdown(data);

    expect(md).toContain('/workspace/MyReport.Report');
    expect(md).toContain('2026-05-23T10:00:00Z');
  });

  it('renders a passed governance section', () => {
    const data = buildGovernanceExportData(makeScoreResult(), makeGovernanceResult());
    const md = exportAsMarkdown(data);

    expect(md).toContain('✅ PASSED');
    expect(md).not.toContain('⛔');
  });

  it('renders a blocked governance section with reasons', () => {
    const data = buildGovernanceExportData(
      makeScoreResult(),
      makeGovernanceResult({
        blocked: true,
        reasons: ['Score below threshold'],
      }),
    );
    const md = exportAsMarkdown(data);

    expect(md).toContain('⛔ BLOCKED');
    expect(md).toContain('Score below threshold');
  });

  it('includes a framework scores table', () => {
    const data = buildGovernanceExportData(makeScoreResult(), makeGovernanceResult());
    const md = exportAsMarkdown(data);

    expect(md).toContain('| Gestalt |');
    expect(md).toContain('| Narrative |');
    expect(md).toContain('Framework | Score');
  });

  it('omits policy notes section when policyNotes is undefined', () => {
    const data = buildGovernanceExportData(makeScoreResult(), makeGovernanceResult());
    const md = exportAsMarkdown(data);

    expect(md).not.toContain('Policy notes:');
  });

  it('includes policy notes when present', () => {
    const data = buildGovernanceExportData(
      makeScoreResult(),
      makeGovernanceResult({ policyNotes: 'Contact IT for theme approval.' }),
    );
    const md = exportAsMarkdown(data);

    expect(md).toContain('Contact IT for theme approval.');
  });
});
