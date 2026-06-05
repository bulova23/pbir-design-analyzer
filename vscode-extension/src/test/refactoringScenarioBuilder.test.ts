import type { RefactoringScenario } from '../analyzer/contracts/scorePanel';
import {
  normalizeRefactoringProviderResponse,
  type RefactoringProviderResponse,
} from '../analyzer/proposalEnrichment/refactoring/refactoringScenarioBuilder';

function providerResponse(overrides: Partial<RefactoringProviderResponse> = {}): RefactoringProviderResponse {
  return {
    status: 'available',
    scenarios: [
      {
        domain: 'layout',
        title: 'Executive KPI layout refactor',
        summary: 'Provide bounded alternatives for the KPI strip.',
        options: [
          {
            title: 'Tighten KPI alignment and spacing',
            summary: 'Creates a cleaner top-line scan path.',
            proposedChanges: [
              'Align KPI cards to a single baseline.',
              'Reduce spacing variance around the KPI strip.',
            ],
            affectedScope: {
              scope: 'page',
              pageNames: ['Overview'],
            },
            rationale: 'This improves executive scanability.',
            evidenceLinks: [
              {
                findingId: 'finding-layout-1',
                label: 'Misaligned KPI strip',
                pageName: 'Overview',
              },
            ],
            businessImpact: 'Faster first-pass interpretation.',
            tradeoffs: [
              {
                title: 'Less annotation room',
                description: 'The cleaner KPI band leaves less room for explanatory notes.',
              },
            ],
            confidence: 0.84,
          },
        ],
      },
      {
        domain: 'navigation',
        title: 'Navigation comparison',
        summary: 'Compare bounded navigation cleanup patterns.',
        options: [
          {
            title: 'Primary nav at the top',
            summary: 'Keeps executive pages one click away.',
            proposedChanges: ['Move primary navigation into a clearer top navigation row.'],
            affectedScope: {
              scope: 'crossPage',
              pageNames: ['Overview', 'Details'],
            },
            rationale: 'This improves predictability for related pages.',
            businessImpact: 'Less hunting for drill pages.',
          },
          {
            label: 'Option C',
            title: 'Persistent left rail',
            summary: 'Keeps navigation visible during detail review.',
            proposedChanges: ['Adopt a persistent left navigation rail for related pages.'],
            affectedScope: {
              scope: 'crossPage',
              pageNames: ['Overview', 'Details'],
            },
            rationale: 'This improves orientation across summary and detail pages.',
            businessImpact: 'Shorter path to supporting detail pages.',
            confidence: 0.77,
          },
        ],
      },
    ],
    ...overrides,
  };
}

describe('normalizeRefactoringProviderResponse', () => {
  it('normalizes one bounded option with evidence links and confidence', () => {
    const normalized = normalizeRefactoringProviderResponse({
      response: providerResponse({
        scenarios: [providerResponse().scenarios?.[0] ?? null].filter(Boolean) as NonNullable<RefactoringProviderResponse['scenarios']>,
      }),
      optionCount: 1,
    });

    expect(normalized).toEqual([
      expect.objectContaining({
        domain: 'layout',
        options: [
          expect.objectContaining({
            optionId: 'scenario-1-option-1',
            label: 'Option A',
            confidence: 0.84,
            evidenceLinks: [
              expect.objectContaining({
                findingId: 'finding-layout-1',
                label: 'Misaligned KPI strip',
              }),
            ],
          }),
        ],
      }),
    ] satisfies RefactoringScenario[]);
  });

  it('normalizes option A/B/C style comparisons and option-level tradeoffs', () => {
    const normalized = normalizeRefactoringProviderResponse({
      response: providerResponse(),
      optionCount: 3,
    });

    expect(normalized[1]?.options.map((item) => item.label)).toEqual(['Option A', 'Option C']);
    expect(normalized[0]?.options[0]?.tradeoffs).toEqual([
      expect.objectContaining({
        title: 'Less annotation room',
      }),
    ]);
  });
});
