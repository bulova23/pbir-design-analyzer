import type {
  RefactoringProposal,
  RefactoringScenario,
  RefactoringDomain,
  RefactoringValidationIssue,
} from '../../contracts/scorePanel';
import type { RefactoringContext } from './refactoringContextBuilder';
import { classifyRefactoringScenario } from './refactoringCompilationClassifier';

function fallbackScenario(context: RefactoringContext, domain: RefactoringDomain): RefactoringScenario {
  return classifyRefactoringScenario({
    scenarioId: `fallback-${domain}-${context.remediationItemId}`,
    domain,
    title: context.remediationTitle,
    summary: context.remediationDetail,
    options: [
      {
        optionId: `fallback-${domain}-option-a`,
        label: 'Option A',
        title: context.recommendedAction,
        summary: context.remediationWhy,
        proposedChanges: [context.recommendedAction],
        affectedScope: {
          scope: 'page',
          pageNames: [...context.affectedPages],
        },
        rationale: context.findings[0]?.summary ?? context.remediationWhy,
        evidenceLinks: context.findings.map((finding) => ({
          findingId: finding.id,
          label: finding.title,
          pageName: context.affectedPages[0],
        })),
        businessImpact: `Expected to improve ${context.resolvedOutcomes.join(' and ').toLowerCase()}.`,
        tradeoffs: [],
        confidence: 0.72,
        compilation: {
          status: 'advisoryOnly',
          hints: [],
        },
      },
    ],
  });
}

export function buildFallbackRefactoringProposal(input: {
  context: RefactoringContext;
  requestedDomains: RefactoringDomain[];
  providerName?: string;
  issues?: RefactoringValidationIssue[];
  scenarios?: RefactoringScenario[];
  status?: RefactoringProposal['status'];
}): RefactoringProposal {
  const domains: RefactoringDomain[] = input.requestedDomains.length > 0
    ? input.requestedDomains
    : ['layout'];
  const scenarios = input.scenarios && input.scenarios.length > 0
    ? input.scenarios
    : [fallbackScenario(input.context, domains[0])];
  const status = input.status
    ?? (input.issues && input.issues.length > 0 ? 'fallback' : 'available');

  return {
    remediationItemId: input.context.remediationItemId,
    status,
    source: 'fallback',
    domains,
    scenarios,
    validation: {
      status: input.issues && input.issues.length > 0 ? 'rejected' : 'passed',
      issues: input.issues ?? [],
    },
    provenance: {
      providerName: input.providerName,
      usedFallback: true,
      enrichedAt: new Date().toISOString(),
      sourceFindingIds: input.context.findings.map((finding) => finding.id),
    },
  };
}
