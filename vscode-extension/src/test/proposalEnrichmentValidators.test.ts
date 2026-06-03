import type { ProposalEnrichmentCandidate, ProposalEnrichmentContext } from '../analyzer/proposalEnrichment/proposalEnrichmentProvider';
import { validateProposalEnrichmentCandidate } from '../analyzer/proposalEnrichment/proposalEnrichmentValidators';

const baseContext: ProposalEnrichmentContext = {
  remediationItemId: 'fix-decision-context:Overview',
  remediationTitle: 'Add benchmarks and decision context',
  remediationDetail: 'Resolve the decision-context gap on this page.',
  remediationWhy: 'Reduces risk of KPI misinterpretation.',
  recommendedAction: 'Add a target benchmark beside the KPI.',
  resolvedOutcomes: ['Benchmark gap'],
  affectedPages: ['Overview'],
  findings: [
    {
      id: 'finding-1',
      title: 'Missing benchmark',
      summary: 'The KPI band shows performance but no target benchmark.',
      severity: 'high',
      impactArea: 'benchmark',
      recommendation: 'Add a clear target benchmark beside the KPI.',
    },
  ],
  pageSummaries: [
    {
      pageName: 'Overview',
      visiblePageTitle: 'Sales Overview',
      inferredPurpose: 'Executive',
      whyThisMatters: 'Executive readers need a benchmark to interpret performance.',
    },
  ],
  supportedOpportunityCategories: [],
  hasDeterministicOpportunities: false,
  enricherIds: ['storytelling'],
};

function candidate(overrides: Partial<ProposalEnrichmentCandidate> = {}): ProposalEnrichmentCandidate {
  return {
    titleSuggestions: [
      {
        title: 'Executive Sales Overview',
        confidence: 0.82,
        rationale: 'Matches the visible KPI summary and page purpose.',
      },
    ],
    explanation: {
      shortText: 'Adding a benchmark gives readers a clear target for interpretation.',
      expandedText: 'Without a benchmark, the KPI communicates performance but not whether the result is good or bad.',
    },
    whyThisMatters: {
      text: 'Without a benchmark, users cannot quickly tell whether performance is on target.',
    },
    advisoryPriority: {
      tier: 'highLeverage',
      rationale: 'This improves decision context on the page.',
    },
    expectedOutcome: {
      text: 'If applied, this change is expected to improve readability and decision context.',
      areas: ['readability', 'consistency'],
    },
    advisoryAlternatives: [
      {
        title: 'Consolidate the KPI section',
        description: 'Instead of adding another KPI card, consolidate the KPI section around one benchmarked summary.',
      },
    ],
    ...overrides,
  };
}

describe('validateProposalEnrichmentCandidate', () => {
  it('accepts grounded advisory output', () => {
    const result = validateProposalEnrichmentCandidate(baseContext, candidate());

    expect(result.status).toBe('passed');
    expect(result.issues).toEqual([]);
  });

  it('rejects invented visuals unsupported execution leaks and overclaimed outcomes', () => {
    const result = validateProposalEnrichmentCandidate(
      baseContext,
      candidate({
        explanation: {
          shortText: 'Create a new bullet chart and a DAX measure, then apply it automatically.',
          expandedText: 'This will add a new bullet chart visual, create a new measure, and guarantee improved outcomes after apply.',
        },
        expectedOutcome: {
          text: 'This fix already improves readability and proves performance is healthy.',
          areas: ['readability'],
        },
      }),
    );

    expect(result.status).toBe('rejected');
    expect(result.issues).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ code: 'inventedArtifact' }),
        expect.objectContaining({ code: 'executionLeak' }),
        expect.objectContaining({ code: 'outcomeOverclaim' }),
      ]),
    );
  });

  it('rejects attempts to rewrite score severity or confidence semantics', () => {
    const result = validateProposalEnrichmentCandidate(
      baseContext,
      candidate({
        advisoryPriority: {
          tier: 'highLeverage',
          rationale: 'This changes the score to 91, lowers severity, and increases confidence to 100.',
        },
      }),
    );

    expect(result.status).toBe('rejected');
    expect(result.issues).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ code: 'semanticRewrite' }),
      ]),
    );
  });
});
