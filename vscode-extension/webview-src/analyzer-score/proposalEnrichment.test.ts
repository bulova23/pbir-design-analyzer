import type { ProposalEnrichment } from '../../src/analyzer/contracts/scorePanel';
import { getAdvisoryPriorityLabel, getProposalEnrichmentSummary, hasProposalEnrichmentContent } from './proposalEnrichment';

function enrichment(overrides: Partial<ProposalEnrichment> = {}): ProposalEnrichment {
  return {
    remediationItemId: 'fix-1',
    status: 'available',
    source: 'provider',
    enrichersApplied: ['storytelling'],
    titleSuggestions: [
      { title: 'Executive Sales Overview', confidence: 0.82, rationale: 'Matches the page purpose.' },
    ],
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
      text: 'If applied, this change is expected to improve readability and decision context.',
      areas: ['readability', 'decision context'],
    },
    advisoryAlternatives: [
      {
        title: 'Consolidate the KPI section',
        description: 'Instead of adding another KPI card, consolidate the KPI section around one benchmarked summary.',
      },
    ],
    validation: {
      status: 'passed',
      issues: [],
    },
    provenance: {
      providerName: 'Test Provider',
      usedFallback: false,
      enrichedAt: '2026-06-02T20:00:00.000Z',
      sourceFindingIds: ['finding-1'],
    },
    ...overrides,
  };
}

describe('proposalEnrichment helpers', () => {
  it('detects whether advisory enrichment contains useful content', () => {
    expect(hasProposalEnrichmentContent(enrichment())).toBe(true);
    expect(hasProposalEnrichmentContent(enrichment({
      titleSuggestions: [],
      explanation: undefined,
      whyThisMatters: undefined,
      advisoryAlternatives: [],
      expectedOutcome: undefined,
    }))).toBe(false);
  });

  it('formats advisory priority and summary labels for the webview', () => {
    expect(getAdvisoryPriorityLabel('highLeverage')).toBe('High leverage');
    expect(getProposalEnrichmentSummary(enrichment())).toContain('AI-enriched');
    expect(getProposalEnrichmentSummary(enrichment({ source: 'fallback', status: 'fallback' }))).toContain('Fallback guidance');
  });
});
