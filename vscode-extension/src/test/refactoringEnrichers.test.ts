import type { RefactoringContext } from '../analyzer/proposalEnrichment/refactoring/refactoringContextBuilder';
import {
  buildRefactoringEnricherScenarios,
} from '../analyzer/proposalEnrichment/refactoring/enrichers';
import { buildExecutiveExperienceRefactoringScenarios } from '../analyzer/proposalEnrichment/refactoring/enrichers/executiveExperienceEnricher';
import { buildLayoutRefactoringScenarios } from '../analyzer/proposalEnrichment/refactoring/enrichers/layoutRefactoringEnricher';
import { buildNavigationRefactoringScenarios } from '../analyzer/proposalEnrichment/refactoring/enrichers/navigationRefactoringEnricher';
import { buildStorytellingRefactoringScenarios } from '../analyzer/proposalEnrichment/refactoring/enrichers/storytellingRefactoringEnricher';

function baseContext(overrides: Partial<RefactoringContext> = {}): RefactoringContext {
  return {
    remediationItemId: 'layout-density:Overview',
    remediationTitle: 'Reduce visual density and align layout',
    remediationDetail: 'Resolve the KPI strip spacing and alignment gap on the overview page.',
    remediationWhy: 'Improves scanability and reduces cognitive load.',
    recommendedAction: 'Align KPI cards and normalize spacing around the KPI strip.',
    resolvedOutcomes: ['Layout consistency', 'Readability', 'Executive clarity'],
    affectedPages: ['Overview'],
    requestedDomains: ['layout', 'storytelling', 'navigation', 'executiveExperience'],
    findings: [
      {
        id: 'finding-layout-1',
        title: 'KPI row lacks alignment',
        summary: 'The KPI row uses inconsistent spacing and headline hierarchy on the overview page.',
        severity: 'high',
        scope: 'page',
        impactArea: 'layout',
        recommendation: 'Align KPI cards and tighten the headline scan path.',
        evidenceLabels: ['KPI spacing mismatch', 'Headline drift'],
      },
      {
        id: 'finding-nav-2',
        title: 'Detail path is easy to miss',
        summary: 'The route from the executive page to supporting detail is unclear.',
        severity: 'medium',
        scope: 'crossPage',
        impactArea: 'navigation',
        recommendation: 'Clarify the summary-to-detail navigation path and return path.',
        evidenceLabels: ['Missing return cue'],
      },
    ],
    pageSummaries: [
      {
        pageName: 'Overview',
        visiblePageTitle: 'Sales Overview',
        inferredPurpose: 'Executive',
        whyThisMatters: 'Executive readers need benchmark context and a fast decision-support scan path.',
        storyArchetype: 'summary-to-detail',
        inferredStory: 'Lead with the KPI headline, then support it with evidence and a clear drill path.',
        visualSummary: {
          visualCount: 10,
          dataVisualCount: 7,
          navigationVisualCount: 1,
          hiddenVisualCount: 0,
          slicerCount: 2,
          visibleTitleVisualCount: 1,
          textVisualCount: 2,
        },
      },
    ],
    crossPageCues: [
      {
        pageName: 'Overview',
        dimension: 'navigation',
        status: 'watch',
        summary: 'Navigation cues between summary and detail pages are inconsistent.',
        relatedFindingIds: ['finding-nav-2'],
      },
      {
        pageName: 'Overview',
        dimension: 'story',
        status: 'weak',
        summary: 'The supporting evidence appears before the top-line narrative is established.',
        relatedFindingIds: ['finding-layout-1'],
      },
    ],
    deterministicSupport: {
      supportedOpportunityCategories: ['alignment', 'spacing', 'title', 'navigation'],
      hasDeterministicOpportunities: true,
    },
    ...overrides,
  };
}

describe('refactoring enrichers', () => {
  it('routes deterministically from grounded requested domains and evidence', () => {
    const routed = buildRefactoringEnricherScenarios(baseContext());

    expect(routed.map((scenario) => scenario.domain)).toEqual([
      'layout',
      'storytelling',
      'navigation',
      'executiveExperience',
    ]);
  });

  it('produces a layout scenario with evidence links and compilable classification when layout evidence exists', () => {
    const scenarios = buildLayoutRefactoringScenarios(baseContext());

    expect(scenarios).toEqual(expect.arrayContaining([
      expect.objectContaining({
        domain: 'layout',
        options: expect.arrayContaining([
          expect.objectContaining({
            evidenceLinks: expect.arrayContaining([
              expect.objectContaining({
                findingId: 'finding-layout-1',
                pageName: 'Overview',
              }),
            ]),
            compilation: expect.objectContaining({
              status: 'compilable',
            }),
          }),
        ]),
      }),
    ]));
    expect(JSON.stringify(scenarios)).not.toContain('mutations');
  });

  it('produces a storytelling scenario when page story and headline-to-evidence flow context exist', () => {
    const scenarios = buildStorytellingRefactoringScenarios(baseContext());

    expect(scenarios[0]).toEqual(
      expect.objectContaining({
        domain: 'storytelling',
        options: [
          expect.objectContaining({
            compilation: {
              status: 'advisoryOnly',
              hints: [],
            },
          }),
        ],
      }),
    );
  });

  it('produces a navigation scenario with evidence-path guidance and preserved evidence links', () => {
    const scenarios = buildNavigationRefactoringScenarios(baseContext());

    expect(scenarios[0]).toEqual(
      expect.objectContaining({
        domain: 'navigation',
        options: [
          expect.objectContaining({
            evidenceLinks: expect.arrayContaining([
              expect.objectContaining({
                findingId: 'finding-nav-2',
                label: expect.stringContaining('Detail path'),
              }),
            ]),
            compilation: expect.objectContaining({
              status: 'compilable',
            }),
          }),
        ],
      }),
    );
  });

  it('produces an executive-experience scenario with benchmark and KPI framing guidance', () => {
    const scenarios = buildExecutiveExperienceRefactoringScenarios(baseContext());

    expect(scenarios[0]).toEqual(
      expect.objectContaining({
        domain: 'executiveExperience',
        options: expect.arrayContaining([
          expect.objectContaining({
            tradeoffs: expect.arrayContaining([
              expect.objectContaining({
                title: expect.stringContaining('benchmark'),
              }),
            ]),
          }),
        ]),
      }),
    );
  });

  it('returns no enricher scenarios for unsupported contexts and leaves fallback handling to the orchestrator', () => {
    const scenarios = buildRefactoringEnricherScenarios(baseContext({
      remediationTitle: 'Investigate governance follow-up',
      remediationDetail: 'Track an open follow-up item.',
      remediationWhy: 'Needs documentation review.',
      recommendedAction: 'Review the issue later.',
      resolvedOutcomes: ['Documentation'],
      findings: [],
      pageSummaries: [],
      crossPageCues: [],
      deterministicSupport: {
        supportedOpportunityCategories: [],
        hasDeterministicOpportunities: false,
      },
    }));

    expect(scenarios).toEqual([]);
  });
});
