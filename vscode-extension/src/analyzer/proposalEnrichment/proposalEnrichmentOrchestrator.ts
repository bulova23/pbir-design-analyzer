import type {
  ProposalEnrichment,
  ProposalEnrichmentValidationIssue,
  ProposalEnricherId,
  ScoreResult,
} from '../contracts/scorePanel';
import { buildProposalEnrichmentContext } from './proposalEnrichmentContextBuilder';
import {
  buildFallbackAlternatives,
  buildFallbackExpectedOutcome,
  buildFallbackExplanation,
  buildFallbackPriority,
  buildFallbackTitleSuggestions,
  buildFallbackWhyThisMatters,
} from './proposalEnrichmentFallbacks';
import type { ProposalEnrichmentCandidate, ProposalEnrichmentProvider } from './proposalEnrichmentProvider';
import { validateProposalEnrichmentCandidate } from './proposalEnrichmentValidators';

function fallbackEnrichment(result: ScoreResult, options: {
  remediationItemId: string;
  enrichersApplied: ProposalEnricherId[];
}): ProposalEnrichment {
  const remediationItem = (result.fixPlan ?? []).find((item) => item.id === options.remediationItemId);
  if (!remediationItem) {
    throw new Error(`Unknown remediation item for proposal enrichment fallback: ${options.remediationItemId}`);
  }

  const context = buildProposalEnrichmentContext({
    result,
    remediationItem,
    enricherIds: options.enrichersApplied,
  });

  return {
    remediationItemId: remediationItem.id,
    status: 'fallback',
    source: 'fallback',
    enrichersApplied: options.enrichersApplied,
    titleSuggestions: buildFallbackTitleSuggestions(context),
    explanation: buildFallbackExplanation(context),
    whyThisMatters: buildFallbackWhyThisMatters(context),
    advisoryPriority: buildFallbackPriority(context),
    expectedOutcome: buildFallbackExpectedOutcome(context),
    advisoryAlternatives: buildFallbackAlternatives(context),
    validation: {
      status: 'passed',
      issues: [],
    },
    provenance: {
      usedFallback: true,
      enrichedAt: new Date().toISOString(),
      sourceFindingIds: [...remediationItem.sourceFindingIds],
    },
  };
}

function applySectionFallbacks(input: {
  context: ReturnType<typeof buildProposalEnrichmentContext>;
  candidate: ProposalEnrichmentCandidate;
  issues: ProposalEnrichmentValidationIssue[];
}): ProposalEnrichmentCandidate {
  const invalidExpectedOutcome = input.issues.some((issue) => issue.section === 'expectedOutcome');

  return {
    ...input.candidate,
    expectedOutcome: invalidExpectedOutcome
      ? buildFallbackExpectedOutcome(input.context)
      : input.candidate.expectedOutcome,
  };
}

export async function enrichFixPlanWithAdvisoryContent(
  result: ScoreResult,
  options: {
    providerMode: 'disabled' | 'provider';
    enabledEnrichers: ProposalEnricherId[];
    provider?: ProposalEnrichmentProvider;
  },
): Promise<ScoreResult> {
  const fixPlan = result.fixPlan ?? [];
  const proposalEnrichments: ProposalEnrichment[] = [];

  for (const remediationItem of fixPlan) {
    const context = buildProposalEnrichmentContext({
      result,
      remediationItem,
      enricherIds: options.enabledEnrichers,
    });

    if (options.providerMode !== 'provider' || !options.provider) {
      proposalEnrichments.push(fallbackEnrichment(result, {
        remediationItemId: remediationItem.id,
        enrichersApplied: options.enabledEnrichers,
      }));
      continue;
    }

    const configured = await options.provider.isConfigured();
    if (!configured) {
      proposalEnrichments.push(fallbackEnrichment(result, {
        remediationItemId: remediationItem.id,
        enrichersApplied: options.enabledEnrichers,
      }));
      continue;
    }

    const candidate = await options.provider.enrich({
      context,
      enricherIds: options.enabledEnrichers,
    });

    const validation = validateProposalEnrichmentCandidate(context, candidate);
    if (validation.status === 'rejected') {
      proposalEnrichments.push(fallbackEnrichment(result, {
        remediationItemId: remediationItem.id,
        enrichersApplied: options.enabledEnrichers,
      }));
      continue;
    }

    const merged = applySectionFallbacks({
      context,
      candidate,
      issues: validation.issues,
    });

    proposalEnrichments.push({
      remediationItemId: remediationItem.id,
      status: 'available',
      source: 'provider',
      enrichersApplied: options.enabledEnrichers,
      titleSuggestions: merged.titleSuggestions ?? buildFallbackTitleSuggestions(context),
      explanation: merged.explanation ?? buildFallbackExplanation(context),
      whyThisMatters: merged.whyThisMatters ?? buildFallbackWhyThisMatters(context),
      advisoryPriority: merged.advisoryPriority ?? buildFallbackPriority(context),
      expectedOutcome: merged.expectedOutcome ?? buildFallbackExpectedOutcome(context),
      advisoryAlternatives: merged.advisoryAlternatives ?? buildFallbackAlternatives(context),
      validation,
      provenance: {
        providerName: options.provider.providerName,
        usedFallback: false,
        enrichedAt: new Date().toISOString(),
        sourceFindingIds: [...remediationItem.sourceFindingIds],
      },
    });
  }

  return {
    ...result,
    proposalEnrichments,
  };
}
