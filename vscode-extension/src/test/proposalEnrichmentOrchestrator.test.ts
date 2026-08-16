import type { ScoreResult } from '../analyzer/contracts/scorePanel';
import { enrichFixPlanWithAdvisoryContent } from '../analyzer/proposalEnrichment/proposalEnrichmentOrchestrator';
import type { ProposalEnrichmentProvider } from '../analyzer/proposalEnrichment/proposalEnrichmentProvider';

function resultWithFixPlan(): ScoreResult {
  return {
    gestaltScore: 81,
    cognitiveLoadScore: 74,
    dataInkScore: 72,
    accessibilityScore: 68,
    visualBestPracticesScore: 75,
    stephenFewScore: 70,
    enterpriseGovernanceScore: 71,
    tufteScore: 69,
    graphicalPerceptionScore: 70,
    densityScore: 66,
    narrativeScore: 73,
    compositeScore: 74,
    feedback: {},
    pageCount: 1,
    recommendations: [],
    reportPath: '/tmp/Sales.Report',
    scoredAt: '2026-06-02T20:00:00.000Z',
    normalizedFindings: [
      {
        id: 'finding-1',
        title: 'Missing benchmark',
        summary: 'The KPI band shows performance but no target benchmark.',
        severity: 'high',
        confidence: 91,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'benchmark',
        frameworkImpact: ['Narrative Design'],
        recommendation: 'Add a clear target benchmark beside the KPI.',
        sourceKind: 'framework',
        sourceSection: 'issues',
        evidence: [],
      },
    ],
    fixPlan: [
      {
        id: 'fix-decision-context:Overview',
        title: 'Add benchmarks and decision context',
        detail: 'Resolve the decision-context gap on this page.',
        severity: 'high',
        effort: 'low',
        impact: 'high',
        why: 'Reduces risk of KPI misinterpretation.',
        scope: 'page',
        affectedPages: ['Overview'],
        recommendedAction: 'Add a target benchmark beside the KPI.',
        resolvedOutcomes: ['Benchmark gap'],
        sourceFindingIds: ['finding-1'],
      },
    ],
    fixOpportunities: [],
    pageScores: [
      {
        pageName: 'Overview',
        gestaltScore: 81,
        cognitiveLoadScore: 74,
        dataInkScore: 72,
        accessibilityScore: 68,
        visualBestPracticesScore: 75,
        stephenFewScore: 70,
        enterpriseGovernanceScore: 71,
        tufteScore: 69,
        graphicalPerceptionScore: 70,
        densityScore: 66,
        narrativeScore: 73,
        compositeScore: 74,
        feedback: {},
        recommendations: [],
        visualMetadata: {
          pageName: 'Overview',
          visiblePageTitle: 'Sales Overview',
          strictVisiblePageTitle: 'Sales Overview',
          semanticColorMap: [],
          visualCount: 1,
          visibleTitleVisualCount: 1,
          textVisualCount: 1,
          slicerCount: 0,
          legendVisualCount: 0,
          axisLabelVisualCount: 0,
          dataLabelVisualCount: 0,
          formattedVisualCount: 1,
          visuals: [],
        },
        pagePurposeAnalysis: {
          inferredPurpose: 'Executive',
          confidence: 'high',
          actionabilityScore: 58,
          benchmarkStatus: 'Benchmark missing',
          topGaps: ['Target benchmark is missing'],
          whyThisMatters: 'Executive readers need a benchmark to interpret performance.',
        },
      },
    ],
    pagePurposeAnalysis: {
      inferredPurpose: 'Executive',
      confidence: 'high',
      actionabilityScore: 58,
      benchmarkStatus: 'Benchmark missing',
      topGaps: ['Target benchmark is missing'],
      whyThisMatters: 'Executive readers need a benchmark to interpret performance.',
    },
  };
}

describe('enrichFixPlanWithAdvisoryContent', () => {
  it('returns deterministic fallback enrichment when provider-backed enrichment is disabled', async () => {
    const result = await enrichFixPlanWithAdvisoryContent(resultWithFixPlan(), {
      providerMode: 'disabled',
      enabledEnrichers: ['storytelling', 'executiveReadability'],
    });

    expect(result.proposalEnrichments).toEqual([
      expect.objectContaining({
        remediationItemId: 'fix-decision-context:Overview',
        status: 'fallback',
        source: 'fallback',
        enrichersApplied: ['storytelling', 'executiveReadability'],
        titleSuggestions: expect.arrayContaining([
          expect.objectContaining({ title: expect.stringContaining('Overview') }),
        ]),
        explanation: expect.objectContaining({
          shortText: expect.stringContaining('benchmark'),
        }),
        validation: expect.objectContaining({
          status: 'passed',
        }),
        provenance: expect.objectContaining({
          usedFallback: true,
        }),
      }),
    ]);
  });

  it('uses validated provider output when available and falls back only invalid sections', async () => {
    const provider: ProposalEnrichmentProvider = {
      providerName: 'Test Provider',
      isConfigured: async () => true,
      enrich: async () => ({
        titleSuggestions: [{ title: 'Executive Sales Overview', confidence: 0.91, rationale: 'Matches the page purpose.' }],
        explanation: {
          shortText: 'Adding a benchmark gives readers a clear target for interpretation.',
          expandedText: 'Without a benchmark, the KPI shows performance but not whether the result is good or bad.',
        },
        whyThisMatters: {
          text: 'Without a benchmark, users cannot quickly tell whether performance is on target.',
        },
        advisoryPriority: {
          tier: 'highLeverage',
          rationale: 'This improves decision context on the page.',
        },
        expectedOutcome: {
          text: 'This fix already proves performance is healthy.',
          areas: ['readability', 'consistency'],
        },
        advisoryAlternatives: [
          {
            title: 'Consolidate the KPI section',
            description: 'Instead of adding another KPI card, consolidate the KPI section around one benchmarked summary.',
          },
        ],
      }),
    };

    const result = await enrichFixPlanWithAdvisoryContent(resultWithFixPlan(), {
      providerMode: 'provider',
      enabledEnrichers: ['storytelling'],
      provider,
    });

    expect(result.proposalEnrichments?.[0]).toEqual(
      expect.objectContaining({
        status: 'available',
        source: 'provider',
        titleSuggestions: [expect.objectContaining({ title: 'Executive Sales Overview' })],
        expectedOutcome: expect.objectContaining({
          text: expect.stringContaining('expected'),
        }),
        validation: expect.objectContaining({
          issues: expect.arrayContaining([
            expect.objectContaining({ code: 'outcomeOverclaim' }),
          ]),
        }),
        provenance: expect.objectContaining({
          providerName: 'Test Provider',
          usedFallback: false,
        }),
      }),
    );
  });
});
