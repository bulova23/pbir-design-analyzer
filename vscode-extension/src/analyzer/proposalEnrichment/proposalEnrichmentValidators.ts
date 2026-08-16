import type {
  ProposalEnrichmentValidationIssue,
  ProposalEnrichmentValidationResult,
} from '../contracts/scorePanel';
import type { ProposalEnrichmentCandidate, ProposalEnrichmentContext } from './proposalEnrichmentProvider';

function findMatches(text: string, needles: RegExp[]): boolean {
  return needles.some((needle) => needle.test(text));
}

function joinCandidateText(candidate: ProposalEnrichmentCandidate): string {
  return [
    ...(candidate.titleSuggestions ?? []).map((item) => `${item.title} ${item.rationale}`),
    candidate.explanation?.shortText,
    candidate.explanation?.expandedText,
    candidate.whyThisMatters?.text,
    candidate.advisoryPriority?.rationale,
    candidate.expectedOutcome?.text,
    ...(candidate.advisoryAlternatives ?? []).flatMap((item) => [item.title, item.description]),
  ]
    .filter((value): value is string => typeof value === 'string' && value.length > 0)
    .join(' ');
}

export function validateProposalEnrichmentCandidate(
  _context: ProposalEnrichmentContext,
  candidate: ProposalEnrichmentCandidate,
): ProposalEnrichmentValidationResult {
  const text = joinCandidateText(candidate);
  const issues: ProposalEnrichmentValidationIssue[] = [];

  if (findMatches(text, [/\bbullet chart\b/i, /\bnew visual\b/i, /\bnew measure\b/i, /\bDAX\b/i])) {
    issues.push({
      code: 'inventedArtifact',
      message: 'Candidate invents visuals, measures, or other unsupported report artifacts.',
    });
  }

  if (findMatches(text, [/\bapply it automatically\b/i, /\bauto(?:matically)? apply\b/i, /\bmutate\b/i, /\bgenerate direct\b/i])) {
    issues.push({
      code: 'executionLeak',
      message: 'Candidate implies mutation authority or automatic execution.',
    });
  }

  if (findMatches(text, [/\balready improves\b/i, /\bproves performance is healthy\b/i, /\bguarantee(?:s|d)?\b/i])) {
    issues.push({
      code: 'outcomeOverclaim',
      message: 'Candidate presents expected outcomes as already-proven outcomes.',
      section: 'expectedOutcome',
    });
  }

  if (findMatches(text, [/\bscore to\b/i, /\blowers severity\b/i, /\bincreases confidence\b/i])) {
    issues.push({
      code: 'semanticRewrite',
      message: 'Candidate attempts to rewrite score, severity, or confidence semantics.',
    });
  }

  const hasFatalIssue = issues.some((issue) => issue.code !== 'outcomeOverclaim');
  if (hasFatalIssue) {
    return { status: 'rejected', issues };
  }

  if (issues.length > 0) {
    return { status: 'degraded', issues };
  }

  return { status: 'passed', issues: [] };
}
