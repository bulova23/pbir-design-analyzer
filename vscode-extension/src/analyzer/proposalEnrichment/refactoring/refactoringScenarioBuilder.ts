import type { RefactoringScenario } from '../../contracts/scorePanel';
import type { RefactoringProviderResponse } from './refactoringProvider';

function optionLabel(index: number): string {
  return `Option ${String.fromCharCode(65 + index)}`;
}

export function normalizeRefactoringProviderResponse(input: {
  response: RefactoringProviderResponse;
  optionCount: number;
}): RefactoringScenario[] {
  if (input.response.status !== 'available' || !input.response.scenarios) {
    return [];
  }

  return input.response.scenarios.map((scenario, scenarioIndex) => ({
    scenarioId: scenario.scenarioId ?? `scenario-${scenarioIndex + 1}`,
    domain: scenario.domain,
    title: scenario.title,
    summary: scenario.summary,
    options: scenario.options
      .slice(0, input.optionCount)
      .map((option, optionIndex) => ({
        optionId: option.optionId ?? `scenario-${scenarioIndex + 1}-option-${optionIndex + 1}`,
        label: option.label ?? optionLabel(optionIndex),
        title: option.title,
        summary: option.summary,
        proposedChanges: [...option.proposedChanges],
        affectedScope: {
          scope: option.affectedScope.scope,
          pageNames: [...option.affectedScope.pageNames],
        },
        rationale: option.rationale,
        evidenceLinks: [...(option.evidenceLinks ?? [])],
        businessImpact: option.businessImpact,
        tradeoffs: [...(option.tradeoffs ?? [])],
        confidence: option.confidence ?? 0.7,
        compilation: {
          status: 'advisoryOnly',
          hints: [],
        },
      })),
  }));
}

export type { RefactoringProviderResponse } from './refactoringProvider';
