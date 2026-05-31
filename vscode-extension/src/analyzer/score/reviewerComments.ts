import type { PageIntentProfileType, PageScore, ReviewerPersona } from '../contracts/scorePanel';

export interface ReviewerCommentResult {
  headline: string;
  comments: string[];
}

export interface ReviewerCommentOptions {
  selectedProfile: PageIntentProfileType;
  persona: ReviewerPersona;
}

function profileLabel(profile: PageIntentProfileType): string {
  switch (profile) {
    case 'executive':
      return 'executive';
    case 'operational':
      return 'operational';
    case 'appendix':
      return 'appendix';
    default:
      return 'analytical';
  }
}

function personaLead(persona: ReviewerPersona): string {
  switch (persona) {
    case 'coach':
      return 'Coaching review';
    case 'consultant':
      return 'Consultant review';
    case 'executiveReviewer':
      return 'Executive review';
    default:
      return 'Design-critic review';
  }
}

function personaVerb(persona: ReviewerPersona): string {
  switch (persona) {
    case 'coach':
      return 'Tighten';
    case 'consultant':
      return 'Prioritize';
    case 'executiveReviewer':
      return 'Clarify';
    default:
      return 'Fix';
  }
}

export function buildReviewerComments(
  page: PageScore,
  options: ReviewerCommentOptions,
): ReviewerCommentResult {
  const comments: string[] = [];
  const actionability = page.actionabilityBreakdown;
  const benchmark = page.benchmarkComparison;
  const story = page.inferredStorySummary;
  const profile = options.selectedProfile;
  const lead = personaLead(options.persona);

  if (benchmark?.beautifulButUseless) {
    comments.push(`${lead}: beautiful but useless is the risk here. The page looks presentable, but the decision path is still weak for a ${profileLabel(profile)} page.`);
  } else if (actionability) {
    comments.push(`${lead}: this page is reading as ${profileLabel(profile)} and its actionability score is ${actionability.score.toFixed(0)}/100.`);
  }

  if (actionability?.gaps.length) {
    comments.push(`${personaVerb(options.persona)} the decision support first: ${actionability.gaps[0]}`);
  }

  if (benchmark?.gaps.length) {
    comments.push(`${personaVerb(options.persona)} the benchmark gap next: ${benchmark.gaps[0]}.`);
  }

  if (story) {
    comments.push(`The current story reads as ${story.storyArchetype}, so the recommendation should reinforce that scan path rather than add more surface polish.`);
  }

  return {
    headline: `${lead} for ${page.pageName} (${profileLabel(profile)})`,
    comments: comments.slice(0, 4),
  };
}
