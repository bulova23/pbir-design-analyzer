import type {
  FixPlanItem,
  RefactoringDomain,
  RefactoringProposal,
  RefactoringValidationIssue,
  ScoreResult,
} from '../../contracts/scorePanel';
import { buildRefactoringEnricherScenarios } from './enrichers';
import { classifyRefactoringScenario } from './refactoringCompilationClassifier';
import { buildRefactoringContext } from './refactoringContextBuilder';
import { buildFallbackRefactoringProposal } from './refactoringFallbacks';
import type { RefactoringProvider } from './refactoringProvider';
import { normalizeRefactoringProviderResponse } from './refactoringScenarioBuilder';
import { validateRefactoringScenarios } from './refactoringValidators';

function buildLocalFallbackProposal(input: {
  context: ReturnType<typeof buildRefactoringContext>;
  requestedDomains: RefactoringDomain[];
  providerName?: string;
  issues?: RefactoringValidationIssue[];
  status?: RefactoringProposal['status'];
}): RefactoringProposal | undefined {
  const scenarios = buildRefactoringEnricherScenarios(input.context);
  if (scenarios.length === 0) {
    return undefined;
  }

  const validation = validateRefactoringScenarios(input.context, scenarios);
  if (validation.status === 'rejected') {
    return undefined;
  }

  return buildFallbackRefactoringProposal({
    context: input.context,
    requestedDomains: input.requestedDomains,
    providerName: input.providerName,
    issues: input.issues,
    scenarios,
    status: input.status,
  });
}

export async function generateRefactoringProposal(
  result: ScoreResult,
  options: {
    remediationItem: FixPlanItem;
    requestedDomains: RefactoringDomain[];
    providerMode: 'disabled' | 'provider';
    provider?: RefactoringProvider;
  },
): Promise<RefactoringProposal> {
  const context = buildRefactoringContext({
    result,
    remediationItem: options.remediationItem,
    requestedDomains: options.requestedDomains,
  });

  if (options.providerMode !== 'provider' || !options.provider) {
    const localProposal = buildLocalFallbackProposal({
      context,
      requestedDomains: options.requestedDomains,
      status: 'available',
    });
    if (localProposal) {
      return localProposal;
    }

    return buildFallbackRefactoringProposal({
      context,
      requestedDomains: options.requestedDomains,
      status: 'fallback',
    });
  }

  const configured = await options.provider.isConfigured();
  if (!configured) {
    const localProposal = buildLocalFallbackProposal({
      context,
      requestedDomains: options.requestedDomains,
      providerName: options.provider.providerName,
      status: 'available',
    });
    if (localProposal) {
      return localProposal;
    }

    return buildFallbackRefactoringProposal({
      context,
      requestedDomains: options.requestedDomains,
      providerName: options.provider.providerName,
      status: 'fallback',
    });
  }

  const response = await options.provider.generate({
    context,
    requestedDomains: options.requestedDomains,
    optionCount: 3,
  });

  const scenarios = normalizeRefactoringProviderResponse({
    response,
    optionCount: 3,
  }).map((scenario) => classifyRefactoringScenario(scenario));

  if (response.status !== 'available' || scenarios.length === 0) {
    const localProposal = buildLocalFallbackProposal({
      context,
      requestedDomains: options.requestedDomains,
      providerName: options.provider.providerName,
      status: 'available',
    });
    if (localProposal) {
      return localProposal;
    }

    return buildFallbackRefactoringProposal({
      context,
      requestedDomains: options.requestedDomains,
      providerName: options.provider.providerName,
      status: 'fallback',
    });
  }

  const validation = validateRefactoringScenarios(context, scenarios);
  if (validation.status === 'rejected') {
    const localProposal = buildLocalFallbackProposal({
      context,
      requestedDomains: options.requestedDomains,
      providerName: options.provider.providerName,
      issues: validation.issues,
      status: 'fallback',
    });
    if (localProposal) {
      return localProposal;
    }

    return buildFallbackRefactoringProposal({
      context,
      requestedDomains: options.requestedDomains,
      providerName: options.provider.providerName,
      issues: validation.issues,
      status: 'fallback',
    });
  }

  return {
    remediationItemId: options.remediationItem.id,
    status: 'available',
    source: 'provider',
    domains: [...options.requestedDomains],
    scenarios,
    validation,
    provenance: {
      providerName: options.provider.providerName,
      usedFallback: false,
      enrichedAt: new Date().toISOString(),
      sourceFindingIds: [...options.remediationItem.sourceFindingIds],
    },
  };
}
