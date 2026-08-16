import type {
  RefactoringCompilationHint,
  RefactoringScenario,
  RefactoringScenarioOption,
  RefactoringValidationResult,
} from '../analyzer/contracts/scorePanel';
import {
  buildRefactoringDeterministicHints,
  classifyRefactoringScenario,
} from '../analyzer/proposalEnrichment/refactoring/refactoringCompilationClassifier';

function hint(overrides: Partial<RefactoringCompilationHint> = {}): RefactoringCompilationHint {
  return {
    category: 'alignment',
    confidence: 0.85,
    rationale: 'The recommendation maps to an existing deterministic alignment opportunity.',
    supportedScopes: ['page'],
    ...overrides,
  };
}

function option(overrides: Partial<RefactoringScenarioOption> = {}): RefactoringScenarioOption {
  return {
    optionId: 'option-a',
    label: 'Option A',
    title: 'Tighten KPI alignment and spacing',
    summary: 'Regroup the KPI strip into a cleaner top-line scan path.',
    proposedChanges: [
      'Align KPI cards to a single baseline.',
      'Reduce spacing variance between the KPI row and the supporting trend.',
    ],
    affectedScope: {
      scope: 'page',
      pageNames: ['Overview'],
    },
    rationale: 'This improves first-glance comparability for executives.',
    evidenceLinks: [
      {
        findingId: 'finding-layout-1',
        label: 'Misaligned KPI strip',
        pageName: 'Overview',
      },
    ],
    businessImpact: 'Faster first-pass interpretation of KPI performance.',
    tradeoffs: [
      {
        title: 'Less whitespace for annotations',
        description: 'The page gains alignment consistency but leaves less room for supporting callouts.',
      },
    ],
    confidence: 0.83,
    compilation: {
      status: 'compilable',
      coverage: 'partial',
      hints: [hint()],
    },
    ...overrides,
  };
}

function scenario(overrides: Partial<RefactoringScenario> = {}): RefactoringScenario {
  return {
    scenarioId: 'scenario-layout-1',
    domain: 'layout',
    title: 'Executive KPI layout refactor',
    summary: 'Presents bounded layout alternatives for the executive summary page.',
    options: [
      option(),
      option({
        optionId: 'option-b',
        label: 'Option B',
        title: 'Consolidate the KPI band into a grid',
        compilation: {
          status: 'advisoryOnly',
          hints: [],
        },
      }),
    ],
    ...overrides,
  };
}

describe('refactoring advisory contracts', () => {
  it('expresses multiple grounded options evidence confidence and binary execution classification', () => {
    const candidate = scenario();
    const validation: RefactoringValidationResult = {
      status: 'passed',
      issues: [],
    };

    expect(candidate.options).toHaveLength(2);
    expect(candidate.options[0]?.evidenceLinks).toEqual([
      expect.objectContaining({
        findingId: 'finding-layout-1',
        label: 'Misaligned KPI strip',
      }),
    ]);
    expect(candidate.options[0]?.confidence).toBeCloseTo(0.83);
    expect(candidate.options[0]?.compilation).toEqual(
      expect.objectContaining({
        status: 'compilable',
        coverage: 'partial',
      }),
    );
    expect(candidate.options[1]?.compilation.status).toBe('advisoryOnly');
    expect(validation.status).toBe('passed');
  });
});

describe('refactoring compilation classification', () => {
  it('classifies supported layout guidance as compilable with deterministic hints only', () => {
    const hints = buildRefactoringDeterministicHints([
      'Align KPI cards to a single baseline.',
      'Reduce spacing variance between related visuals.',
      'Keep the page story intact.',
    ]);

    const classified = classifyRefactoringScenario(scenario({
      options: [
        option({
          proposedChanges: [
            'Align KPI cards to a single baseline.',
            'Reduce spacing variance between related visuals.',
            'Keep the page story intact.',
          ],
          compilation: undefined,
        }),
      ],
    }));

    expect(hints).toEqual([
      expect.objectContaining({ category: 'alignment' }),
      expect.objectContaining({ category: 'spacing' }),
    ]);
    expect(classified.options[0]?.compilation).toEqual(
      expect.objectContaining({
        status: 'compilable',
        coverage: 'partial',
        hints: [
          expect.objectContaining({ category: 'alignment' }),
          expect.objectContaining({ category: 'spacing' }),
        ],
      }),
    );
    expect(JSON.stringify(classified)).not.toContain('mutations');
    expect(JSON.stringify(classified)).not.toContain('rollback');
  });

  it('classifies unsupported storytelling guidance as advisory-only', () => {
    const classified = classifyRefactoringScenario(scenario({
      domain: 'storytelling',
      options: [
        option({
          title: 'Resequence the page into question-answer narrative',
          proposedChanges: [
            'Lead with the business question before the KPI explanation.',
            'Add a clearer narrative bridge between summary and detail sections.',
          ],
          compilation: undefined,
        }),
      ],
    }));

    expect(classified.options[0]?.compilation).toEqual({
      status: 'advisoryOnly',
      hints: [],
    });
  });

  it('preserves per-option classification in mixed scenarios', () => {
    const classified = classifyRefactoringScenario(scenario({
      options: [
        option({
          optionId: 'option-a',
          proposedChanges: ['Normalize title hierarchy and alignment for the KPI band.'],
          compilation: undefined,
        }),
        option({
          optionId: 'option-b',
          proposedChanges: ['Resequence the page into a decision-first executive story.'],
          compilation: undefined,
        }),
      ],
    }));

    expect(classified.options[0]?.compilation.status).toBe('compilable');
    expect(classified.options[1]?.compilation.status).toBe('advisoryOnly');
  });
});
