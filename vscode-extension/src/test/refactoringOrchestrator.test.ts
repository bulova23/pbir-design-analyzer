import type { FixPlanItem, ScoreResult } from '../analyzer/contracts/scorePanel';
import { generateRefactoringProposal } from '../analyzer/proposalEnrichment/refactoring/refactoringOrchestrator';
import type { RefactoringProvider } from '../analyzer/proposalEnrichment/refactoring/refactoringProvider';

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
    scoredAt: '2026-06-05T12:00:00.000Z',
    normalizedFindings: [
      {
        id: 'finding-layout-1',
        title: 'KPI row lacks alignment',
        summary: 'The KPI row uses inconsistent spacing and alignment on the overview page.',
        severity: 'high',
        confidence: 92,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'layout',
        frameworkImpact: ['Narrative Design'],
        recommendation: 'Align KPI cards and normalize spacing.',
        sourceKind: 'framework',
        sourceSection: 'issues',
        evidence: [
          {
            kind: 'metadata',
            label: 'KPI spacing mismatch',
            pageName: 'Overview',
          },
        ],
      },
    ],
    fixPlan: [
      {
        id: 'layout-density:Overview',
        title: 'Reduce visual density and align layout',
        detail: 'Resolve the KPI strip spacing and alignment gap on the overview page.',
        severity: 'high',
        effort: 'low',
        impact: 'high',
        why: 'Improves scanability and reduces cognitive load.',
        scope: 'page',
        affectedPages: ['Overview'],
        recommendedAction: 'Align KPI cards and normalize spacing.',
        resolvedOutcomes: ['Layout consistency', 'Readability'],
        sourceFindingIds: ['finding-layout-1'],
      },
    ],
    fixOpportunities: [
      {
        id: 'fixopp-1',
        remediationItemId: 'layout-density:Overview',
        title: 'Align KPI row',
        category: 'alignment',
        summary: 'Normalize KPI positions.',
        confidence: 0.88,
        safetyClass: 'safe',
        affectedPages: ['Overview'],
        targetObjectIds: ['kpi-1'],
        sourceFindingIds: ['finding-layout-1'],
        expectedResolutions: ['Layout consistency'],
        mutations: [],
        previewRows: [],
        rollbackPlan: {
          id: 'rb-1',
          fixOpportunityId: 'fixopp-1',
          fileBackups: [],
          reverseMutations: [],
        },
        state: 'Previewed',
      },
    ],
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
        inferredStorySummary: {
          intentProfile: 'executive',
          storyArchetype: 'summary-to-detail',
          inferredStory: 'Lead with KPI status before trend detail.',
          confidence: 'high',
          evidence: ['KPI row appears before trend detail'],
        },
        visualMetadata: {
          pageName: 'Overview',
          visiblePageTitle: 'Sales Overview',
          strictVisiblePageTitle: 'Sales Overview',
          semanticColorMap: [],
          visualCount: 6,
          visibleTitleVisualCount: 1,
          textVisualCount: 2,
          slicerCount: 1,
          legendVisualCount: 0,
          axisLabelVisualCount: 2,
          dataLabelVisualCount: 1,
          formattedVisualCount: 6,
          visuals: [],
        },
        pagePurposeAnalysis: {
          inferredPurpose: 'Executive',
          confidence: 'high',
          actionabilityScore: 58,
          benchmarkStatus: 'Benchmark missing',
          topGaps: ['Target benchmark is missing'],
          whyThisMatters: 'Executive readers need a benchmark and a clear scan path.',
        },
      },
    ],
  };
}

describe('generateRefactoringProposal', () => {
  it('returns local bounded enricher scenarios when provider mode is disabled and grounded evidence is sufficient', async () => {
    const remediationItem = resultWithFixPlan().fixPlan?.[0] as FixPlanItem;

    const proposal = await generateRefactoringProposal(resultWithFixPlan(), {
      remediationItem,
      requestedDomains: ['layout', 'storytelling', 'executiveExperience'],
      providerMode: 'disabled',
    });

    expect(proposal).toEqual(
      expect.objectContaining({
        remediationItemId: remediationItem.id,
        status: 'available',
        source: 'fallback',
        domains: ['layout', 'storytelling', 'executiveExperience'],
        scenarios: expect.arrayContaining([
          expect.objectContaining({ domain: 'layout' }),
          expect.objectContaining({ domain: 'storytelling' }),
          expect.objectContaining({ domain: 'executiveExperience' }),
        ]),
      }),
    );
  });

  it('orchestrates context provider classification validation and provenance', async () => {
    const remediationItem = resultWithFixPlan().fixPlan?.[0] as FixPlanItem;
    const provider: RefactoringProvider = {
      providerName: 'Test Provider',
      isConfigured: async () => true,
      generate: async () => ({
        status: 'available',
        scenarios: [
          {
            domain: 'layout',
            title: 'Executive KPI layout refactor',
            summary: 'Provide bounded alternatives for the KPI strip.',
            options: [
              {
                title: 'Tighten KPI alignment',
                summary: 'Creates a cleaner top-line scan path.',
                proposedChanges: ['Align KPI cards to a single baseline.'],
                affectedScope: {
                  scope: 'page',
                  pageNames: ['Overview'],
                },
                rationale: 'This improves executive scanability.',
                evidenceLinks: [
                  {
                    findingId: 'finding-layout-1',
                    label: 'KPI spacing mismatch',
                    pageName: 'Overview',
                  },
                ],
                businessImpact: 'Expected to improve first-pass interpretation.',
                confidence: 0.84,
              },
            ],
          },
        ],
      }),
    };

    const proposal = await generateRefactoringProposal(resultWithFixPlan(), {
      remediationItem,
      requestedDomains: ['layout'],
      providerMode: 'provider',
      provider,
    });

    expect(proposal).toEqual(
      expect.objectContaining({
        remediationItemId: remediationItem.id,
        status: 'available',
        source: 'provider',
        domains: ['layout'],
        scenarios: [
          expect.objectContaining({
            options: [
              expect.objectContaining({
                compilation: expect.objectContaining({
                  status: 'compilable',
                }),
              }),
            ],
          }),
        ],
        validation: expect.objectContaining({
          status: 'passed',
        }),
        provenance: expect.objectContaining({
          providerName: 'Test Provider',
          usedFallback: false,
          sourceFindingIds: ['finding-layout-1'],
        }),
      }),
    );
  });

  it('downgrades invalid provider output to deterministic fallback wording without blocking deterministic flows', async () => {
    const remediationItem = resultWithFixPlan().fixPlan?.[0] as FixPlanItem;
    const provider: RefactoringProvider = {
      providerName: 'Test Provider',
      isConfigured: async () => true,
      generate: async () => ({
        status: 'available',
        scenarios: [
          {
            domain: 'layout',
            title: 'Unsafe redesign',
            summary: 'Apply a new bullet chart automatically on the Details page.',
            options: [
              {
                title: 'Unsafe option',
                summary: 'Apply a new bullet chart automatically on the Details page.',
                proposedChanges: ['Auto-apply a new bullet chart on the Details page.'],
                affectedScope: {
                  scope: 'page',
                  pageNames: ['Details'],
                },
                rationale: 'Execute the redesign immediately.',
                businessImpact: 'This already improves the dashboard.',
              },
            ],
          },
        ],
      }),
    };

    const proposal = await generateRefactoringProposal(resultWithFixPlan(), {
      remediationItem,
      requestedDomains: ['layout'],
      providerMode: 'provider',
      provider,
    });

    expect(proposal.status).toBe('fallback');
    expect(proposal.source).toBe('fallback');
    expect(proposal.validation.issues).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ code: 'inventedArtifact' }),
        expect.objectContaining({ code: 'unsupportedExecutionClaim' }),
      ]),
    );
    expect(proposal.provenance.usedFallback).toBe(true);
    expect(proposal.scenarios[0]?.options[0]?.compilation.status).toBe('compilable');
  });
});
