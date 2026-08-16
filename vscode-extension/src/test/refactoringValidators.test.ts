import type { RefactoringScenario } from '../analyzer/contracts/scorePanel';
import type { RefactoringContext } from '../analyzer/proposalEnrichment/refactoring/refactoringContextBuilder';
import { validateRefactoringScenarios } from '../analyzer/proposalEnrichment/refactoring/refactoringValidators';

const baseContext: RefactoringContext = {
  remediationItemId: 'layout-density:Overview',
  remediationTitle: 'Reduce visual density and align layout',
  remediationDetail: 'Resolve the KPI strip spacing and alignment gap on the overview page.',
  remediationWhy: 'Improves scanability and reduces cognitive load.',
  recommendedAction: 'Align KPI cards and normalize spacing.',
  resolvedOutcomes: ['Layout consistency', 'Readability'],
  affectedPages: ['Overview'],
  requestedDomains: ['layout', 'executiveExperience'],
  findings: [
    {
      id: 'finding-layout-1',
      title: 'KPI row lacks alignment',
      summary: 'The KPI row uses inconsistent spacing and alignment on the overview page.',
      severity: 'high',
      scope: 'page',
      impactArea: 'layout',
      recommendation: 'Align KPI cards and normalize spacing.',
      evidenceLabels: ['KPI spacing mismatch'],
    },
  ],
  pageSummaries: [
    {
      pageName: 'Overview',
      visiblePageTitle: 'Sales Overview',
      inferredPurpose: 'Executive',
      whyThisMatters: 'Executive readers need a clear scan path.',
      storyArchetype: 'summary-to-detail',
      inferredStory: 'Lead with KPI status before supporting trend context.',
      visualSummary: {
        visualCount: 6,
        slicerCount: 1,
        visibleTitleVisualCount: 1,
        textVisualCount: 2,
      },
    },
  ],
  crossPageCues: [],
  deterministicSupport: {
    supportedOpportunityCategories: ['alignment'],
    hasDeterministicOpportunities: true,
  },
};

function scenario(overrides: Partial<RefactoringScenario> = {}): RefactoringScenario {
  return {
    scenarioId: 'scenario-1',
    domain: 'layout',
    title: 'Executive KPI layout refactor',
    summary: 'Provide bounded alternatives for the KPI strip.',
    options: [
      {
        optionId: 'option-a',
        label: 'Option A',
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
        tradeoffs: [],
        confidence: 0.84,
        compilation: {
          status: 'compilable',
          coverage: 'full',
          hints: [
            {
              category: 'alignment',
              confidence: 0.8,
              rationale: 'Maps to existing deterministic support.',
              supportedScopes: ['page'],
            },
          ],
        },
      },
    ],
    ...overrides,
  };
}

describe('validateRefactoringScenarios', () => {
  it('accepts grounded advisory scenarios', () => {
    const result = validateRefactoringScenarios(baseContext, [scenario()]);

    expect(result.status).toBe('passed');
    expect(result.issues).toEqual([]);
  });

  it('rejects invented pages unsupported execution claims and contradictory evidence', () => {
    const result = validateRefactoringScenarios(baseContext, [
      scenario({
        options: [
          {
            ...scenario().options[0]!,
            summary: 'Automatically apply a new bullet chart on the Details page.',
            affectedScope: {
              scope: 'page',
              pageNames: ['Details'],
            },
            businessImpact: 'This already improves the dashboard and guarantees better performance.',
            compilation: {
              status: 'compilable',
              coverage: 'full',
              hints: [],
            },
          },
        ],
      }),
    ]);

    expect(result.status).toBe('rejected');
    expect(result.issues).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ code: 'inventedArtifact' }),
        expect.objectContaining({ code: 'unsupportedExecutionClaim' }),
        expect.objectContaining({ code: 'contradictoryEvidence' }),
        expect.objectContaining({ code: 'outcomeOverclaim' }),
      ]),
    );
  });

  it('rejects duplicate options with fake tradeoffs and execution leakage', () => {
    const duplicate = scenario().options[0]!;
    const result = validateRefactoringScenarios(baseContext, [
      scenario({
        options: [
          duplicate,
          {
            ...duplicate,
            optionId: 'option-b',
            label: 'Option B',
            tradeoffs: [
              {
                title: 'Fake tradeoff',
                description: 'Same outcome, same scope, same execution path.',
              },
            ],
            rationale: 'Auto-execute this deterministic redesign immediately.',
          },
        ],
      }),
    ]);

    expect(result.status).toBe('rejected');
    expect(result.issues).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ code: 'optionDuplication' }),
        expect.objectContaining({ code: 'scopeEscape' }),
      ]),
    );
  });
});
