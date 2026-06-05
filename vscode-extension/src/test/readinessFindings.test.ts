import type { ScoreResult } from '../analyzer/contracts/scorePanel';
import { assessFabricAppReadiness } from '../analyzer/fabric/readiness/readinessAnalyzer';
import { buildFabricReadinessFindings } from '../analyzer/fabric/readiness/readinessFindings';

const result: ScoreResult = {
  gestaltScore: 76,
  cognitiveLoadScore: 70,
  dataInkScore: 74,
  accessibilityScore: 65,
  visualBestPracticesScore: 78,
  stephenFewScore: 73,
  enterpriseGovernanceScore: 68,
  tufteScore: 66,
  graphicalPerceptionScore: 72,
  densityScore: 61,
  narrativeScore: 69,
  compositeScore: 74,
  feedback: {},
  pageCount: 1,
  recommendations: [],
  reportPath: '/tmp/Sales.Report',
  scoredAt: '2026-06-03T21:00:00.000Z',
  dataVisualCount: 6,
  navigationVisualCount: 2,
  hiddenVisualCount: 0,
  pageScores: [
    {
      pageName: 'Executive Overview',
      gestaltScore: 81,
      cognitiveLoadScore: 76,
      dataInkScore: 77,
      accessibilityScore: 72,
      visualBestPracticesScore: 80,
      stephenFewScore: 75,
      enterpriseGovernanceScore: 74,
      tufteScore: 71,
      graphicalPerceptionScore: 74,
      densityScore: 70,
      narrativeScore: 78,
      compositeScore: 78,
      feedback: {},
      recommendations: [],
      actionabilityBreakdown: {
        score: 70,
        targetBenchmarkPresent: true,
        exceptionVisibility: true,
        urgencySignaling: false,
        priorPeriodContext: true,
        drillPathPresent: true,
        expectationLevel: 'high',
        strengths: ['Benchmark visible'],
        gaps: [],
        summary: 'Clear decision path with room to improve urgency signaling.',
      },
      pageIntentProfile: {
        inferredProfile: 'executive',
        actionabilityExpectation: 'high',
        reviewGuidance: [],
        evidence: [],
      },
      visualMetadata: {
        pageName: 'Executive Overview',
        visiblePageTitle: 'Executive Overview',
        semanticColorMap: [],
        visualCount: 5,
        visibleTitleVisualCount: 1,
        textVisualCount: 1,
        slicerCount: 1,
        legendVisualCount: 1,
        axisLabelVisualCount: 1,
        dataLabelVisualCount: 1,
        formattedVisualCount: 5,
        visuals: [],
      },
    },
  ],
  normalizedFindings: [],
};

describe('buildFabricReadinessFindings', () => {
  it('generates readiness issues and evidence-backed opportunities for the workspace', () => {
    const readiness = assessFabricAppReadiness(result, 'migrationReadiness');
    const findings = buildFabricReadinessFindings(result, readiness);

    expect(findings).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          title: 'Good Fabric App Candidate',
          sourceKind: 'fabricAppReadiness',
          sourceSection: 'issues',
        }),
        expect.objectContaining({
          title: 'Visualization Opportunity',
          sourceKind: 'fabricAppReadiness',
          sourceSection: 'issues',
        }),
      ]),
    );

    const candidateFinding = findings.find((finding) => finding.title === 'Good Fabric App Candidate');
    expect(candidateFinding?.evidence).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          kind: 'readiness',
        }),
      ]),
    );
  });
});
