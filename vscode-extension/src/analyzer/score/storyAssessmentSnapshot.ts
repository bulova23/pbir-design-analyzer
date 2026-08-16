import type {
  ActionabilityBreakdown,
  BenchmarkComparisonSummary,
  GuidedStoryImprovement,
  PageIntentProfile,
  PageScore,
  ScoreResult,
  StoryAssessmentDiffResult,
  StoryAssessmentPageSnapshot,
  StoryAssessmentRecommendationSnapshot,
  StoryAssessmentReportSnapshot,
} from '../contracts/scorePanel';
import { getStoryMaturityLabel } from './storyAssessmentPresentation';

export interface StoryAssessmentSnapshotDiff {
  byPage: Record<string, StoryAssessmentDiffResult>;
}

function dedupe(values: Array<string | undefined>): string[] {
  const seen = new Set<string>();
  const ordered: string[] = [];

  for (const value of values) {
    const normalized = value?.trim();
    if (!normalized || seen.has(normalized)) {
      continue;
    }

    seen.add(normalized);
    ordered.push(normalized);
  }

  return ordered;
}

function getStrongSignals(page: PageScore): string[] {
  return dedupe([
    ...(page.actionabilityBreakdown?.strengths ?? []),
    ...(page.benchmarkComparison?.strengths ?? []),
    ...(page.pageIntentProfile?.evidence ?? []).map((item) => item.length > 80 ? item.slice(0, 80).trim() : item.trim()),
  ]).slice(0, 4);
}

function mapImprovementToMissingSignal(improvement: GuidedStoryImprovement): string | undefined {
  const title = improvement.title.toLowerCase();

  if (title.includes('question') || title.includes('title')) {
    return 'No clear headline question';
  }

  if (title.includes('benchmark') || title.includes('target')) {
    return 'No visible benchmark or target';
  }

  if (title.includes('prior-period') || title.includes('trend')) {
    return 'No prior-period context';
  }

  if (title.includes('primary metric')) {
    return 'No clear primary metric';
  }

  if (title.includes('primary dimension')) {
    return 'No clear primary comparison anchor';
  }

  if (title.includes('filter')) {
    return 'No focused filter path reinforcing one main takeaway';
  }

  return undefined;
}

function mapGapToMissingSignal(gap: string): string | undefined {
  const normalized = gap.toLowerCase();

  if (normalized.includes('exception')) {
    return 'No clear exception callout';
  }

  if (normalized.includes('target') || normalized.includes('benchmark')) {
    return 'No visible benchmark or target';
  }

  if (normalized.includes('prior-period') || normalized.includes('movement over time')) {
    return 'No prior-period context';
  }

  return undefined;
}

function getMissingSignals(page: PageScore): string[] {
  const improvements = page.guidedStoryImprovements
    ? [
        ...page.guidedStoryImprovements.highPriorityImprovements,
        ...page.guidedStoryImprovements.mediumPriorityImprovements,
      ]
    : [];

  return dedupe([
    ...page.pagePurposeAnalysis?.topGaps.map(mapGapToMissingSignal) ?? [],
    ...page.actionabilityBreakdown?.gaps.map(mapGapToMissingSignal) ?? [],
    ...page.benchmarkComparison?.gaps.map(mapGapToMissingSignal) ?? [],
    ...improvements.map(mapImprovementToMissingSignal),
  ]).slice(0, 5);
}

function buildRecommendationSnapshot(improvement: GuidedStoryImprovement): StoryAssessmentRecommendationSnapshot {
  return {
    id: improvement.id,
    title: improvement.title,
    summary: improvement.summary,
    rationale: improvement.rationale,
    expectedImpact: improvement.expectedImpact,
    priority: improvement.priority,
    relatedImpactArea: improvement.relatedImpactArea,
    navigationTarget: improvement.navigationTarget,
  };
}

function buildPageSnapshot(page: PageScore): StoryAssessmentPageSnapshot | undefined {
  if (!page.guidedStoryImprovements || !page.pagePurposeAnalysis) {
    return undefined;
  }

  const recommendations = [
    ...page.guidedStoryImprovements.highPriorityImprovements,
    ...page.guidedStoryImprovements.mediumPriorityImprovements,
  ].map(buildRecommendationSnapshot);

  return {
    pageName: page.pageName,
    storyMaturity: getStoryMaturityLabel({
      analysis: page.pagePurposeAnalysis,
      guidedStoryImprovements: page.guidedStoryImprovements,
    }),
    strongSignals: getStrongSignals(page),
    missingSignals: getMissingSignals(page),
    topImprovementIds: recommendations.slice(0, 3).map((item) => item.id),
    recommendations,
  };
}

export function buildStoryAssessmentReportSnapshot(result: ScoreResult): StoryAssessmentReportSnapshot {
  return {
    reportPath: result.reportPath,
    scoredAt: result.scoredAt,
    pages: (result.pageScores ?? [])
      .map((page) => buildPageSnapshot(page))
      .filter((page): page is StoryAssessmentPageSnapshot => Boolean(page)),
  };
}

function compareList(previous: string[], current: string[]): { added: string[]; removed: string[] } {
  const previousSet = new Set(previous);
  const currentSet = new Set(current);

  return {
    added: current.filter((item) => !previousSet.has(item)),
    removed: previous.filter((item) => !currentSet.has(item)),
  };
}

function compareRecommendations(
  previous: StoryAssessmentRecommendationSnapshot[],
  current: StoryAssessmentRecommendationSnapshot[],
): Pick<StoryAssessmentDiffResult, 'resolvedRecommendations' | 'newRecommendations' | 'unchangedRecommendations'> {
  const previousById = new Map(previous.map((item) => [item.id, item] as const));
  const currentById = new Map(current.map((item) => [item.id, item] as const));

  return {
    resolvedRecommendations: previous.filter((item) => !currentById.has(item.id)),
    newRecommendations: current.filter((item) => !previousById.has(item.id)),
    unchangedRecommendations: current.filter((item) => previousById.has(item.id)),
  };
}

function maturityRank(value: StoryAssessmentPageSnapshot['storyMaturity']): number {
  switch (value) {
    case 'Draft':
      return 0;
    case 'Developing':
      return 1;
    case 'Strong':
      return 2;
    default:
      return 3;
  }
}

function compareMaturity(
  previous: StoryAssessmentPageSnapshot['storyMaturity'],
  current: StoryAssessmentPageSnapshot['storyMaturity'],
): StoryAssessmentDiffResult['maturityChange'] {
  if (maturityRank(current) > maturityRank(previous)) {
    return 'improved';
  }

  if (maturityRank(current) < maturityRank(previous)) {
    return 'regressed';
  }

  return 'unchanged';
}

function buildSummary(diff: Omit<StoryAssessmentDiffResult, 'summary'>): string {
  const parts: string[] = [];

  if (diff.maturityChange === 'improved') {
    parts.push('Story maturity improved');
  } else if (diff.maturityChange === 'regressed') {
    parts.push('Story maturity regressed');
  }

  if (diff.resolvedRecommendations.length > 0) {
    parts.push(`${diff.resolvedRecommendations.length} recommendation resolved`);
  }

  if (diff.newRecommendations.length > 0) {
    parts.push(`${diff.newRecommendations.length} new recommendation added`);
  }

  if (diff.addedStrongSignals.length > 0) {
    parts.push(`${diff.addedStrongSignals.length} strong signal added`);
  }

  if (diff.removedMissingSignals.length > 0) {
    parts.push(`${diff.removedMissingSignals.length} missing signal removed`);
  }

  return parts.length > 0 ? parts.join('. ') : 'No public Story Assessment changes detected.';
}

export function compareStoryAssessmentSnapshots(
  previous: StoryAssessmentReportSnapshot,
  current: StoryAssessmentReportSnapshot,
): StoryAssessmentSnapshotDiff {
  const previousPages = new Map(previous.pages.map((page) => [page.pageName, page] as const));
  const byPage: Record<string, StoryAssessmentDiffResult> = {};

  for (const currentPage of current.pages) {
    const previousPage = previousPages.get(currentPage.pageName);
    if (!previousPage) {
      continue;
    }

    const strongSignals = compareList(previousPage.strongSignals, currentPage.strongSignals);
    const missingSignals = compareList(previousPage.missingSignals, currentPage.missingSignals);
    const recommendationChanges = compareRecommendations(previousPage.recommendations, currentPage.recommendations);

    const diffWithoutSummary: Omit<StoryAssessmentDiffResult, 'summary'> = {
      pageName: currentPage.pageName,
      maturityChange: compareMaturity(previousPage.storyMaturity, currentPage.storyMaturity),
      resolvedRecommendations: recommendationChanges.resolvedRecommendations,
      newRecommendations: recommendationChanges.newRecommendations,
      unchangedRecommendations: recommendationChanges.unchangedRecommendations,
      addedStrongSignals: strongSignals.added,
      removedStrongSignals: strongSignals.removed,
      addedMissingSignals: missingSignals.added,
      removedMissingSignals: missingSignals.removed,
    };

    byPage[currentPage.pageName] = {
      ...diffWithoutSummary,
      summary: buildSummary(diffWithoutSummary),
    };
  }

  return { byPage };
}
