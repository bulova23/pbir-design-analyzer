import type {
  ClosedLoopIterationComparison,
  DesignArtifactApprovalState,
  DesignIterationRecord,
  IterationComparisonSnapshot,
  IterationRecommendationSnapshot,
  RefinementAnalyzerSource,
  ValidationResultStatus,
} from '../contracts/designStudioModels';

export interface IterationTimelineEntryViewModel {
  iterationId: string;
  stageLabel: string;
  timestampLabel: string;
  versionLabel: string;
  summary: string;
  detailItems: string[];
  isCurrentResult: boolean;
}

function formatTimestamp(value: string): string {
  const date = new Date(value);
  const year = String(date.getUTCFullYear());
  const month = String(date.getUTCMonth() + 1).padStart(2, '0');
  const day = String(date.getUTCDate()).padStart(2, '0');
  const hours = String(date.getUTCHours()).padStart(2, '0');
  const minutes = String(date.getUTCMinutes()).padStart(2, '0');
  return `${year}-${month}-${day} ${hours}:${minutes} UTC`;
}

function formatApprovalState(value: DesignArtifactApprovalState): string {
  switch (value) {
    case 'approved':
      return 'Approved';
    case 'pendingApproval':
      return 'Pending approval';
    case 'rejected':
      return 'Rejected';
    default:
      return 'Not submitted';
  }
}

function formatValidationStatus(value: DesignArtifactApprovalState | ValidationResultStatus): string {
  switch (value) {
    case 'validated':
      return 'Validated';
    case 'needsReview':
      return 'Needs review';
    case 'rejected':
      return 'Rejected';
    default:
      return formatApprovalState(value);
  }
}

function formatAnalyzerLabel(value: RefinementAnalyzerSource): string {
  switch (value) {
    case 'storyAssessment':
      return 'Story Assessment';
    case 'guidedStoryImprovements':
      return 'Guided Story Improvements';
    case 'crossPageNarrative':
      return 'Cross-page narrative review';
    case 'fixPlan':
      return 'Fix Plan';
    case 'issues':
      return 'Issues review';
  }
}

function sentenceCase(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function withoutTrailingPeriod(value: string): string {
  return value.trim().replace(/[.]+$/u, '');
}

function describeRecommendationState(state: IterationRecommendationSnapshot['approvalState']): string {
  switch (state) {
    case 'approved':
      return 'Accepted';
    case 'rejected':
      return 'Rejected';
    case 'pendingApproval':
      return 'Deferred';
    default:
      return 'Proposed';
  }
}

function stageLabel(iteration: DesignIterationRecord): string {
  if (iteration.approvalCheckpoint.validationApproval.approvalState === 'approved') {
    return 'Validation checkpoint';
  }

  if (iteration.refinementProposals.length > 0) {
    return 'Refinement review';
  }

  if (iteration.analyzerResults.length > 0) {
    return 'Analyzer review';
  }

  if (iteration.materializedCandidate) {
    return 'Materialized candidate';
  }

  return 'Draft review';
}

function detailItems(iteration: DesignIterationRecord): string[] {
  const items: string[] = [];
  if (iteration.comparisonSnapshot.concept) {
    items.push('Concept ready');
  }
  if (iteration.comparisonSnapshot.draft) {
    items.push('Draft ready');
  }
  if (iteration.materializedCandidate) {
    items.push('Materialized candidate prepared');
  }
  if (iteration.analyzerResults.length > 0) {
    items.push('Analyzer review recorded');
  }
  if (iteration.refinementProposals.length > 0) {
    items.push('Recommendation decisions recorded');
  }
  items.push('Approval checkpoint recorded');
  return items;
}

function uniquePush(items: string[], value: string): void {
  if (!items.includes(value)) {
    items.push(value);
  }
}

function describeListChanges(
  beforeValues: string[],
  afterValues: string[],
  formatter: (value: string) => string,
): string[] {
  const changes: string[] = [];
  const before = new Set(beforeValues);
  const after = new Set(afterValues);

  for (const value of afterValues) {
    if (!before.has(value)) {
      uniquePush(changes, formatter(value));
    }
  }

  return changes;
}

function buildConceptChanges(base: IterationComparisonSnapshot, candidate: IterationComparisonSnapshot): string[] {
  const changes: string[] = [];

  if (base.concept?.navigationPattern !== candidate.concept?.navigationPattern) {
    changes.push('Changed navigation structure.');
  }

  return changes.concat(describeListChanges(
    base.concept?.pageTitles ?? [],
    candidate.concept?.pageTitles ?? [],
    (value) => `Added ${value.toLowerCase()}.`,
  ));
}

function buildDraftChanges(base: IterationComparisonSnapshot, candidate: IterationComparisonSnapshot): string[] {
  const changes: string[] = [];
  changes.push(...describeListChanges(
    base.draft?.pageStructureSummaries ?? [],
    candidate.draft?.pageStructureSummaries ?? [],
    (value) => `Added ${value.toLowerCase()}.`,
  ));
  changes.push(...describeListChanges(
    base.draft?.layoutTitles ?? [],
    candidate.draft?.layoutTitles ?? [],
    (value) => `Added ${value.toLowerCase()}.`,
  ));
  return changes;
}

function buildAnalyzerOutputChanges(base: IterationComparisonSnapshot, candidate: IterationComparisonSnapshot): string[] {
  const changes: string[] = [];
  const baseSources = new Set(base.analyzerOutputs.map((output) => output.analyzerSource));
  const candidateSources = new Set(candidate.analyzerOutputs.map((output) => output.analyzerSource));

  for (const output of candidate.analyzerOutputs) {
    if (!baseSources.has(output.analyzerSource)) {
      changes.push(`${formatAnalyzerLabel(output.analyzerSource)} review was added.`);
    }
  }

  for (const output of base.analyzerOutputs) {
    if (!candidateSources.has(output.analyzerSource)) {
      changes.push(`${formatAnalyzerLabel(output.analyzerSource)} review was replaced.`);
    }
  }

  return changes;
}

function buildRecommendationEvolution(candidate: IterationComparisonSnapshot): string[] {
  return candidate.recommendations.map((recommendation) =>
    `${describeRecommendationState(recommendation.approvalState)} recommendation: ${withoutTrailingPeriod(recommendation.suggestedDesignChange)}.`);
}

function buildRecommendationChanges(base: IterationComparisonSnapshot, candidate: IterationComparisonSnapshot): string[] {
  const changes: string[] = [];
  const seen = new Set<string>();

  for (const recommendation of candidate.recommendations) {
    if (!seen.has(recommendation.suggestedDesignChange)) {
      seen.add(recommendation.suggestedDesignChange);
      changes.push(`${describeRecommendationState(recommendation.approvalState)} recommendation: ${withoutTrailingPeriod(recommendation.suggestedDesignChange)}.`);
    }
  }

  for (const recommendation of base.recommendations) {
    if (!candidate.recommendations.some((entry) => entry.proposalId === recommendation.proposalId)) {
      changes.push(`Removed recommendation: ${withoutTrailingPeriod(recommendation.suggestedDesignChange)}.`);
    }
  }

  return changes;
}

function buildApprovalEvolution(base: DesignIterationRecord, candidate: DesignIterationRecord): string[] {
  const changes: string[] = [];
  const pairs: Array<{
    label: string;
    before: DesignArtifactApprovalState;
    after: DesignArtifactApprovalState;
  }> = [
    {
      label: 'Design Approval',
      before: base.approvalCheckpoint.designApproval.approvalState,
      after: candidate.approvalCheckpoint.designApproval.approvalState,
    },
    {
      label: 'Materialization Approval',
      before: base.approvalCheckpoint.materializationApproval.approvalState,
      after: candidate.approvalCheckpoint.materializationApproval.approvalState,
    },
    {
      label: 'Refinement Approval',
      before: base.approvalCheckpoint.refinementApproval.approvalState,
      after: candidate.approvalCheckpoint.refinementApproval.approvalState,
    },
    {
      label: 'Validation Approval',
      before: base.approvalCheckpoint.validationApproval.approvalState,
      after: candidate.approvalCheckpoint.validationApproval.approvalState,
    },
  ];

  for (const pair of pairs) {
    if (pair.before !== pair.after) {
      changes.push(`${pair.label} changed from ${formatApprovalState(pair.before)} to ${formatApprovalState(pair.after)}.`);
    }
  }

  return changes;
}

function buildValidationEvolution(base: IterationComparisonSnapshot, candidate: IterationComparisonSnapshot): string[] {
  const changes: string[] = [];

  if (base.validationStatus !== candidate.validationStatus) {
    changes.push(`Validation status changed from ${formatValidationStatus(base.validationStatus)} to ${formatValidationStatus(candidate.validationStatus)}.`);
  }

  const baseSources = new Set(base.analyzerOutputs.map((output) => output.analyzerSource));
  const candidateSources = new Set(candidate.analyzerOutputs.map((output) => output.analyzerSource));
  for (const output of candidate.analyzerOutputs) {
    if (!baseSources.has(output.analyzerSource)) {
      const replaced = base.analyzerOutputs[0]?.analyzerSource;
      if (replaced) {
        changes.push(`${formatAnalyzerLabel(output.analyzerSource)} review replaced ${formatAnalyzerLabel(replaced)}.`);
      } else {
        changes.push(`${formatAnalyzerLabel(output.analyzerSource)} review was added.`);
      }
    }
  }

  for (const output of base.analyzerOutputs) {
    if (!candidateSources.has(output.analyzerSource) && candidate.analyzerOutputs.length === 0) {
      changes.push(`${formatAnalyzerLabel(output.analyzerSource)} review was removed.`);
    }
  }

  return changes;
}

function buildChangeSummary(
  conceptChanges: string[],
  draftChanges: string[],
  recommendationEvolution: string[],
  validationEvolution: string[],
): string[] {
  const changes: string[] = [];
  for (const item of [...conceptChanges, ...draftChanges]) {
    uniquePush(changes, item);
  }

  for (const item of recommendationEvolution) {
    const normalized = item
      .replace(/^Accepted recommendation: /, '')
      .replace(/^Rejected recommendation: /, '')
      .replace(/^Deferred recommendation: /, '')
      .replace(/^Proposed recommendation: /, '');
    uniquePush(changes, sentenceCase(normalized));
  }

  for (const item of validationEvolution) {
    uniquePush(changes, item);
  }

  return changes;
}

export function buildIterationTimeline(iterations: DesignIterationRecord[]): IterationTimelineEntryViewModel[] {
  return iterations.map((iteration, index) => ({
    iterationId: iteration.id,
    stageLabel: stageLabel(iteration),
    timestampLabel: formatTimestamp(iteration.updatedAt),
    versionLabel: `Version ${iteration.version}`,
    summary: iteration.comparisonSummary,
    detailItems: detailItems(iteration),
    isCurrentResult: index === iterations.length - 1,
  }));
}

export function buildIterationComparison(
  base: DesignIterationRecord,
  candidate: DesignIterationRecord,
): ClosedLoopIterationComparison {
  const conceptChanges = buildConceptChanges(base.comparisonSnapshot, candidate.comparisonSnapshot);
  const draftChanges = buildDraftChanges(base.comparisonSnapshot, candidate.comparisonSnapshot);
  const analyzerOutputChanges = buildAnalyzerOutputChanges(base.comparisonSnapshot, candidate.comparisonSnapshot);
  const recommendationEvolution = buildRecommendationEvolution(candidate.comparisonSnapshot);
  const recommendationChanges = buildRecommendationChanges(base.comparisonSnapshot, candidate.comparisonSnapshot);
  const approvalEvolution = buildApprovalEvolution(base, candidate);
  const validationEvolution = buildValidationEvolution(base.comparisonSnapshot, candidate.comparisonSnapshot);
  const changeSummary = buildChangeSummary(conceptChanges, draftChanges, recommendationEvolution, validationEvolution);
  const summary = validationEvolution.length > 0 || approvalEvolution.length > 0
    ? 'This iteration improved the design and validation story.'
    : 'This iteration updated the design history.';

  return {
    baseIterationId: base.id,
    candidateIterationId: candidate.id,
    summary,
    changeSummary,
    conceptChanges,
    draftChanges,
    analyzerOutputChanges,
    recommendationChanges,
    recommendationEvolution,
    approvalEvolution,
    validationStatusChanges: validationEvolution,
    validationEvolution,
  };
}
