import type {
  RefactoringScenario,
  RefactoringValidationIssue,
  RefactoringValidationResult,
} from '../../contracts/scorePanel';
import type { RefactoringContext } from './refactoringContextBuilder';

function joinScenarioText(scenarios: RefactoringScenario[]): string {
  return scenarios
    .flatMap((scenario) => [
      scenario.title,
      scenario.summary,
      ...scenario.options.flatMap((option) => [
        option.title,
        option.summary,
        option.rationale,
        option.businessImpact,
        ...option.proposedChanges,
        ...option.tradeoffs.flatMap((tradeoff) => [tradeoff.title, tradeoff.description]),
      ]),
    ])
    .join(' ');
}

function hasDuplicateOptions(scenario: RefactoringScenario): boolean {
  const seen = new Set<string>();

  for (const option of scenario.options) {
    const signature = JSON.stringify({
      title: option.title.toLowerCase(),
      summary: option.summary.toLowerCase(),
      proposedChanges: option.proposedChanges.map((item) => item.toLowerCase()),
      pages: option.affectedScope.pageNames.map((item) => item.toLowerCase()),
    });

    if (seen.has(signature)) {
      return true;
    }

    seen.add(signature);
  }

  return false;
}

export function validateRefactoringScenarios(
  context: RefactoringContext,
  scenarios: RefactoringScenario[],
): RefactoringValidationResult {
  const issues: RefactoringValidationIssue[] = [];
  const text = joinScenarioText(scenarios);
  const allowedPages = new Set(context.pageSummaries.map((page) => page.pageName));
  const allowedFindingIds = new Set(context.findings.map((finding) => finding.id));

  if (/\bbullet chart\b/i.test(text) || /\bnew visual\b/i.test(text) || /\bnew KPI\b/i.test(text) || /\bnew measure\b/i.test(text) || /\bDAX\b/i.test(text)) {
    issues.push({
      code: 'inventedArtifact',
      message: 'Scenario invents visuals, KPIs, or measures that are not grounded in local evidence.',
    });
  }

  if (/\bautomatically apply\b/i.test(text) || /\bauto-apply\b/i.test(text) || /\bapply .* immediately\b/i.test(text) || /\bexecute immediately\b/i.test(text)) {
    issues.push({
      code: 'unsupportedExecutionClaim',
      message: 'Scenario implies direct execution authority or automatic apply behavior.',
    });
  }

  if (/\balready improves\b/i.test(text) || /\bguarantee(?:s|d)?\b/i.test(text)) {
    issues.push({
      code: 'outcomeOverclaim',
      message: 'Scenario presents expected outcomes as already-proven outcomes.',
    });
  }

  if (/\bauto-execute\b/i.test(text) || /\bedit PBIR\b/i.test(text) || /\bchange readiness scoring\b/i.test(text)) {
    issues.push({
      code: 'scopeEscape',
      message: 'Scenario escapes the advisory refactoring scope or execution boundary.',
    });
  }

  for (const scenario of scenarios) {
    if (hasDuplicateOptions(scenario)) {
      issues.push({
        code: 'optionDuplication',
        message: 'Scenario options are near-duplicates and do not express real tradeoff diversity.',
        scenarioId: scenario.scenarioId,
      });
    }

    for (const option of scenario.options) {
      if (option.affectedScope.pageNames.some((pageName) => !allowedPages.has(pageName))) {
        issues.push({
          code: 'contradictoryEvidence',
          message: 'Scenario references pages outside the grounded remediation context.',
          scenarioId: scenario.scenarioId,
          optionId: option.optionId,
        });
      }

      if (option.evidenceLinks.some((link) => link.findingId && !allowedFindingIds.has(link.findingId))) {
        issues.push({
          code: 'contradictoryEvidence',
          message: 'Scenario evidence links reference findings outside the grounded remediation context.',
          scenarioId: scenario.scenarioId,
          optionId: option.optionId,
        });
      }

      if (option.compilation.status === 'compilable' && option.compilation.hints.length === 0) {
        issues.push({
          code: 'unsupportedExecutionClaim',
          message: 'Scenario claims deterministic support without classifier-backed hints.',
          scenarioId: scenario.scenarioId,
          optionId: option.optionId,
        });
      }
    }
  }

  if (issues.length > 0) {
    return {
      status: 'rejected',
      issues,
    };
  }

  return {
    status: 'passed',
    issues: [],
  };
}
