import type {
  DesignBrief,
  DesignStudioArtifactKind,
  DraftLayoutArtifact,
  DraftNavigationArtifact,
  DraftPageArtifact,
  DraftReportArtifact,
  KpiHierarchyConcept,
  NavigationConcept,
  PageConcept,
  RefinementProposal,
  ReportConcept,
} from '../contracts/designStudioModels';
import { getRecommendationState } from '../contracts/designStudioModels';
import type {
  DesignStudioRefinementExperienceViewModel,
  DesignStudioRefinementGroupId,
  DesignStudioRefinementGroupViewModel,
  DesignStudioRefinementProposalViewModel,
} from '../contracts/designStudioShell';

type ArtifactRecord =
  | DesignBrief
  | ReportConcept
  | NavigationConcept
  | KpiHierarchyConcept
  | PageConcept
  | DraftReportArtifact
  | DraftPageArtifact
  | DraftLayoutArtifact
  | DraftNavigationArtifact;

const GROUP_META: Record<DesignStudioRefinementGroupId, { title: string; summary: string }> = {
  story: {
    title: 'Story Improvements',
    summary: 'Clarify the headline question, comparison context, and decision path.',
  },
  layout: {
    title: 'Layout Improvements',
    summary: 'Tighten the page layout so the reading order and emphasis feel intentional.',
  },
  kpi: {
    title: 'KPI Improvements',
    summary: 'Make KPI hierarchy, benchmarks, and business meaning easier to understand.',
  },
  navigation: {
    title: 'Navigation Improvements',
    summary: 'Reduce branching and make the report flow easier to follow.',
  },
  structure: {
    title: 'Report Structure Improvements',
    summary: 'Strengthen the report-wide sequence, overview posture, and page-to-page coherence.',
  },
};

function hasText(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0;
}

function readPayloadString(payload: unknown, key: string): string | undefined {
  if (!payload || typeof payload !== 'object' || Array.isArray(payload)) {
    return undefined;
  }

  const value = (payload as Record<string, unknown>)[key];
  return hasText(value) ? value : undefined;
}

function sourceAnalyzerLabel(source: RefinementProposal['sourceAnalyzerOutput']['analyzerSource']): string {
  switch (source) {
    case 'storyAssessment':
      return 'Story Assessment';
    case 'guidedStoryImprovements':
      return 'Guided Story Improvements';
    case 'issues':
      return 'Issues';
    case 'fixPlan':
      return 'Fix Plan';
    case 'crossPageNarrative':
      return 'Cross-Page Narrative';
  }
}

function artifactLabel(artifact: ArtifactRecord): string {
  switch (artifact.kind) {
    case 'designBrief':
      return 'Design Brief';
    case 'reportConcept':
      return 'Report concept baseline';
    case 'navigationConcept':
      return `Navigation concept: ${artifact.pattern}`;
    case 'kpiHierarchyConcept':
      return 'KPI hierarchy concept';
    case 'pageConcept':
      return `Page concept: ${artifact.title}`;
    case 'draftReportArtifact':
      return 'Current draft report';
    case 'draftPageArtifact':
      return `Draft page: ${artifact.structureSummary}`;
    case 'draftLayoutArtifact':
      return `Layout: ${artifact.title}`;
    case 'draftNavigationArtifact':
      return `Navigation draft: ${artifact.frameworkType}`;
  }
}

function buildArtifactMap(input: {
  brief: DesignBrief;
  concept: ReportConcept;
  draft: DraftReportArtifact;
  pageConcepts: PageConcept[];
  pageArtifacts: DraftPageArtifact[];
  layoutArtifacts: DraftLayoutArtifact[];
  navigationArtifacts: DraftNavigationArtifact[];
}): Map<string, ArtifactRecord> {
  const entries: ReadonlyArray<readonly [string, ArtifactRecord]> = [
    [input.brief.id, input.brief],
    [input.concept.id, input.concept],
    [input.concept.navigationStructure.id, input.concept.navigationStructure],
    [input.concept.kpiHierarchy.id, input.concept.kpiHierarchy],
    ...input.pageConcepts.map((artifact) => [artifact.id, artifact] as const),
    [input.draft.id, input.draft],
    ...input.pageArtifacts.map((artifact) => [artifact.id, artifact] as const),
    ...input.layoutArtifacts.map((artifact) => [artifact.id, artifact] as const),
    ...input.navigationArtifacts.map((artifact) => [artifact.id, artifact] as const),
  ];

  return new Map<string, ArtifactRecord>(entries);
}

function classifyGroup(
  proposal: RefinementProposal,
  affectedArtifacts: ArtifactRecord[],
): DesignStudioRefinementGroupId {
  const artifactKinds = new Set<DesignStudioArtifactKind>(affectedArtifacts.map((artifact) => artifact.kind));
  const text = `${proposal.suggestedDesignChange} ${proposal.rationale} ${proposal.expectedImpact}`.toLowerCase();

  if (artifactKinds.has('draftLayoutArtifact')) {
    return 'layout';
  }

  if (
    artifactKinds.has('navigationConcept')
    || artifactKinds.has('draftNavigationArtifact')
    || text.includes('navigation')
    || text.includes('flow')
    || text.includes('path')
  ) {
    return 'navigation';
  }

  if (
    artifactKinds.has('kpiHierarchyConcept')
    || text.includes('kpi')
    || text.includes('benchmark')
    || text.includes('metric')
  ) {
    return 'kpi';
  }

  if (
    proposal.sourceAnalyzerOutput.analyzerSource === 'crossPageNarrative'
    || text.includes('report structure')
    || text.includes('overview')
    || text.includes('entry point')
    || text.includes('chapter')
  ) {
    return 'structure';
  }

  return 'story';
}

function buildSupportingEvidence(proposal: RefinementProposal, affectedArtifactLabels: string[]): string[] {
  const payload = proposal.sourceAnalyzerOutput.payload;
  const evidence: string[] = [];

  const summary = readPayloadString(payload, 'summary');
  if (summary) {
    evidence.push(summary);
  }

  const recommendation = readPayloadString(payload, 'recommendation');
  if (recommendation) {
    evidence.push(recommendation);
  }

  const why = readPayloadString(payload, 'why');
  if (why) {
    evidence.push(why);
  }

  const title = readPayloadString(payload, 'title');
  if (title) {
    evidence.push(`Analyzer signal: ${title}`);
  }

  if (affectedArtifactLabels.length > 0) {
    evidence.push(`Affected area: ${affectedArtifactLabels.join(', ')}`);
  }

  return [...new Set(evidence)];
}

function proposalTitle(proposal: RefinementProposal): string {
  return readPayloadString(proposal.sourceAnalyzerOutput.payload, 'title')
    ?? proposal.suggestedDesignChange;
}

function proposalSummary(proposal: RefinementProposal): string {
  return readPayloadString(proposal.sourceAnalyzerOutput.payload, 'summary')
    ?? proposal.suggestedDesignChange;
}

function proposalComparison(
  proposal: RefinementProposal,
  brief: DesignBrief,
  draft: DraftReportArtifact,
  affectedArtifactLabels: string[],
) {
  return {
    originalDesignIntent: brief.intendedStory,
    currentDesignState: affectedArtifactLabels.length > 0
      ? `${draft.summary} Focus area: ${affectedArtifactLabels.join(', ')}.`
      : draft.summary,
    proposedRefinement: proposal.suggestedDesignChange,
  };
}

function proposalActions(proposal: RefinementProposal): DesignStudioRefinementProposalViewModel['availableActions'] {
  switch (getRecommendationState(proposal)) {
    case 'approved':
      return ['defer', 'reject'];
    case 'rejected':
      return ['defer', 'approve'];
    default:
      return ['approve', 'reject', 'defer'];
  }
}

export function buildRefinementExperience(input: {
  brief: DesignBrief;
  concept: ReportConcept;
  draft: DraftReportArtifact;
  pageConcepts: PageConcept[];
  pageArtifacts: DraftPageArtifact[];
  layoutArtifacts: DraftLayoutArtifact[];
  navigationArtifacts: DraftNavigationArtifact[];
  proposals: RefinementProposal[];
}): DesignStudioRefinementExperienceViewModel {
  if (input.proposals.length === 0) {
    return {
      title: 'Suggested Improvements',
      summary: 'Analyzer recommendations will appear here after an explicit review pass returns to Design Studio.',
      groups: [],
      emptyState: 'No advisory refinement proposals are available yet.',
    };
  }

  const artifactMap = buildArtifactMap(input);
  const grouped = new Map<DesignStudioRefinementGroupId, DesignStudioRefinementProposalViewModel[]>();

  for (const proposal of input.proposals) {
    const affectedArtifacts = proposal.affectedArtifactIds
      .map((artifactId) => artifactMap.get(artifactId))
      .filter((artifact): artifact is ArtifactRecord => Boolean(artifact));
    const affectedArtifactLabels = affectedArtifacts.map(artifactLabel);
    const groupId = classifyGroup(proposal, affectedArtifacts);
    const nextProposal: DesignStudioRefinementProposalViewModel = {
      id: proposal.id,
      title: proposalTitle(proposal),
      summary: proposalSummary(proposal),
      recommendation: proposal.suggestedDesignChange,
      rationale: proposal.rationale,
      expectedImpact: proposal.expectedImpact,
      approvalState: proposal.approvalState,
      recommendationState: getRecommendationState(proposal),
      sourceAnalyzerLabel: sourceAnalyzerLabel(proposal.sourceAnalyzerOutput.analyzerSource),
      affectedArtifacts: affectedArtifactLabels.length > 0 ? affectedArtifactLabels : ['Current draft review surface'],
      supportingEvidence: buildSupportingEvidence(proposal, affectedArtifactLabels),
      comparison: proposalComparison(proposal, input.brief, input.draft, affectedArtifactLabels),
      availableActions: proposalActions(proposal),
    };

    grouped.set(groupId, [...(grouped.get(groupId) ?? []), nextProposal]);
  }

  const groups: DesignStudioRefinementGroupViewModel[] = (Object.keys(GROUP_META) as DesignStudioRefinementGroupId[])
    .map((groupId) => ({
      id: groupId,
      title: GROUP_META[groupId].title,
      summary: GROUP_META[groupId].summary,
      proposals: grouped.get(groupId) ?? [],
    }))
    .filter((group) => group.proposals.length > 0);

  return {
    title: 'Suggested Improvements',
    summary: 'Review grouped consultant-style recommendations, understand the reasoning, and decide which proposals should shape the next iteration.',
    groups,
  };
}
