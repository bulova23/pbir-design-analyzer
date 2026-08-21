import type {
  FixPlanItem,
  GuidedStoryImprovement,
  GuidedStoryImprovements,
  NormalizedFinding,
  PageVisualMetadataSummary,
  ScorePanelNavigationTarget,
  ScoreResult,
  VisualMetadataItem,
} from '../contracts/scorePanel';

type ImprovementCategory =
  | 'missingTitleQuestionAnchor'
  | 'missingBenchmarkTarget'
  | 'missingPriorPeriodContext'
  | 'missingPrimaryMetric'
  | 'missingPrimaryDimension'
  | 'scatteredFilters'
  | 'unknown';

interface ScoredVisualCandidate {
  score: number;
  visual: VisualMetadataItem;
}

function normalizeText(value: string | undefined): string {
  return (value ?? '').trim().toLowerCase();
}

function classifyImprovement(improvement: GuidedStoryImprovement): ImprovementCategory {
  const id = normalizeText(improvement.id);
  const title = normalizeText(improvement.title);
  const combined = `${id} ${title}`;

  if (combined.includes('title') || combined.includes('question-anchor') || combined.includes('question anchor')) {
    return 'missingTitleQuestionAnchor';
  }

  if (combined.includes('benchmark') || combined.includes('target')) {
    return 'missingBenchmarkTarget';
  }

  if (combined.includes('prior-period') || combined.includes('prior period')) {
    return 'missingPriorPeriodContext';
  }

  if (combined.includes('primary metric') || combined.includes('primary-metric')) {
    return 'missingPrimaryMetric';
  }

  if (
    combined.includes('primary dimension')
    || combined.includes('primary-dimension')
    || combined.includes('comparison dimension')
  ) {
    return 'missingPrimaryDimension';
  }

  if (combined.includes('filter')) {
    return 'scatteredFilters';
  }

  return 'unknown';
}

function createPageTarget(pageName: string, supportState: ScorePanelNavigationTarget['supportState'], reason: string): ScorePanelNavigationTarget {
  return {
    kind: 'page',
    pageName,
    label: `Open ${pageName} page`,
    reason,
    supportState,
  };
}

function createVisualTarget(pageName: string, visual: VisualMetadataItem, reason: string): ScorePanelNavigationTarget {
  return {
    kind: 'visual',
    pageName,
    visualId: visual.visualId,
    label: `Open ${visual.visibleTitleText?.trim() || visual.bestVisibleText?.trim() || visual.visualType}`,
    reason,
    supportState: 'direct',
  };
}

function isEligibleVisual(visual: VisualMetadataItem): boolean {
  return !visual.isHidden && !visual.isDecorative && !visual.isNavigationElement;
}

function topOfScanBias(visual: VisualMetadataItem): number {
  const verticalBias = Math.max(0, 300 - Math.min(visual.y, 300)) / 30;
  const horizontalBias = Math.max(0, 300 - Math.min(visual.x, 300)) / 60;
  return verticalBias + horizontalBias;
}

function scoreBenchmarkVisual(visual: VisualMetadataItem): number {
  let score = topOfScanBias(visual);
  const visualType = normalizeText(visual.visualType);

  if (visualType.includes('card') || visualType.includes('kpi') || visualType.includes('score')) {
    score += 10;
  }

  if (visual.valueHints.length > 0 || visual.measureHints.length > 0) {
    score += 4;
  }

  if (visual.hasVisibleTitleIntent) {
    score += 2;
  }

  return score;
}

function scoreTrendVisual(visual: VisualMetadataItem): number {
  let score = topOfScanBias(visual);
  const visualType = normalizeText(visual.visualType);
  const intent = normalizeText(visual.chartIntent?.intent);

  if (intent === 'trend') {
    score += 10;
  }

  if (visualType.includes('line') || visualType.includes('area')) {
    score += 8;
  }

  return score;
}

function scorePrimaryMetricVisual(visual: VisualMetadataItem): number {
  let score = topOfScanBias(visual);
  const visualType = normalizeText(visual.visualType);

  if (visualType.includes('card') || visualType.includes('kpi') || visualType.includes('score')) {
    score += 10;
  }

  if (visual.valueHints.length > 0 || visual.measureHints.length > 0) {
    score += 3;
  }

  return score;
}

function scorePrimaryDimensionVisual(visual: VisualMetadataItem): number {
  let score = topOfScanBias(visual);
  const visualType = normalizeText(visual.visualType);
  const intent = normalizeText(visual.chartIntent?.intent);

  if (intent === 'comparison') {
    score += 8;
  }

  if (visualType.includes('bar') || visualType.includes('column')) {
    score += 8;
  }

  if (visual.categoryHints.length > 0) {
    score += 3;
  }

  return score;
}

function chooseStableCandidate(
  visuals: VisualMetadataItem[],
  scorer: (visual: VisualMetadataItem) => number,
): VisualMetadataItem | undefined {
  const candidates: ScoredVisualCandidate[] = visuals
    .filter(isEligibleVisual)
    .map((visual) => ({
      visual,
      score: scorer(visual),
    }))
    .filter((candidate) => candidate.score > 0)
    .sort((left, right) => right.score - left.score);

  if (candidates.length === 0) {
    return undefined;
  }

  if (candidates.length > 1 && candidates[0].score === candidates[1].score) {
    return undefined;
  }

  return candidates[0].visual;
}

function chooseFilterVisual(visuals: VisualMetadataItem[]): VisualMetadataItem | undefined {
  const slicers = visuals
    .filter(isEligibleVisual)
    .filter((visual) => visual.isSlicer);

  if (slicers.length !== 1) {
    return undefined;
  }

  return slicers[0];
}

function buildTargetForImprovement(
  pageName: string,
  metadata: PageVisualMetadataSummary | undefined,
  improvement: GuidedStoryImprovement,
): ScorePanelNavigationTarget {
  const category = classifyImprovement(improvement);
  const visuals = metadata?.visuals ?? [];

  switch (category) {
    case 'missingTitleQuestionAnchor':
      return createPageTarget(pageName, 'direct', 'This recommendation affects page framing.');
    case 'missingBenchmarkTarget': {
      const visual = chooseStableCandidate(visuals, scoreBenchmarkVisual);
      return visual
        ? createVisualTarget(pageName, visual, 'This recommendation is tied to the lead metric or benchmark visual.')
        : createPageTarget(pageName, 'fallback', 'No stable benchmark visual could be inferred from public metadata.');
    }
    case 'missingPriorPeriodContext': {
      const visual = chooseStableCandidate(visuals, scoreTrendVisual);
      return visual
        ? createVisualTarget(pageName, visual, 'This recommendation is tied to the page trend visual.')
        : createPageTarget(pageName, 'fallback', 'No stable trend visual could be inferred from public metadata.');
    }
    case 'missingPrimaryMetric': {
      const visual = chooseStableCandidate(visuals, scorePrimaryMetricVisual);
      return visual
        ? createVisualTarget(pageName, visual, 'This recommendation is tied to the lead metric visual.')
        : createPageTarget(pageName, 'fallback', 'No stable lead metric visual could be inferred from public metadata.');
    }
    case 'missingPrimaryDimension': {
      const visual = chooseStableCandidate(visuals, scorePrimaryDimensionVisual);
      return visual
        ? createVisualTarget(pageName, visual, 'This recommendation is tied to the page comparison visual.')
        : createPageTarget(pageName, 'fallback', 'No stable comparison visual could be inferred from public metadata.');
    }
    case 'scatteredFilters': {
      const visual = chooseFilterVisual(visuals);
      return visual
        ? createVisualTarget(pageName, visual, 'This recommendation is tied to the clearest public filter cluster.')
        : createPageTarget(pageName, 'fallback', 'No single stable filter target could be inferred from public metadata.');
    }
    default:
      return createPageTarget(pageName, 'fallback', 'This recommendation could not be mapped to a stable public target.');
  }
}

function mapGuidedStoryImprovements(
  pageName: string,
  metadata: PageVisualMetadataSummary | undefined,
  improvements: GuidedStoryImprovements | undefined,
): GuidedStoryImprovements | undefined {
  if (!improvements) {
    return undefined;
  }

  const attach = (improvement: GuidedStoryImprovement): GuidedStoryImprovement => ({
    ...improvement,
    navigationTarget: buildTargetForImprovement(pageName, metadata, improvement),
  });

  return {
    ...improvements,
    highPriorityImprovements: improvements.highPriorityImprovements.map(attach),
    mediumPriorityImprovements: improvements.mediumPriorityImprovements.map(attach),
  };
}

function resolveResultPageName(result: ScoreResult): string | undefined {
  if (result.scoredPageName) {
    return result.scoredPageName;
  }

  if ((result.pageScores?.length ?? 0) === 1) {
    return result.pageScores?.[0].pageName;
  }

  return undefined;
}

function buildImprovementTargetLookup(result: ScoreResult): Map<string, ScorePanelNavigationTarget> {
  const lookup = new Map<string, ScorePanelNavigationTarget>();

  const resultPageName = resolveResultPageName(result);
  const resultImprovements = result.guidedStoryImprovements
    ? [
        ...result.guidedStoryImprovements.highPriorityImprovements,
        ...result.guidedStoryImprovements.mediumPriorityImprovements,
      ]
    : [];

  for (const improvement of resultImprovements) {
    if (resultPageName && improvement.navigationTarget) {
      lookup.set(`guided:${resultPageName}:${improvement.id}`, improvement.navigationTarget);
      lookup.set(`finding:${resultPageName}:${improvement.id}`, improvement.navigationTarget);
    }
  }

  for (const page of result.pageScores ?? []) {
    const improvements = page.guidedStoryImprovements
      ? [
          ...page.guidedStoryImprovements.highPriorityImprovements,
          ...page.guidedStoryImprovements.mediumPriorityImprovements,
        ]
      : [];

    for (const improvement of improvements) {
      if (improvement.navigationTarget) {
        lookup.set(`guided:${page.pageName}:${improvement.id}`, improvement.navigationTarget);
        lookup.set(`finding:${page.pageName}:${improvement.id}`, improvement.navigationTarget);
      }
    }
  }

  return lookup;
}

function attachTargetsToFindings(
  findings: NormalizedFinding[] | undefined,
  targetLookup: Map<string, ScorePanelNavigationTarget>,
): NormalizedFinding[] | undefined {
  return findings?.map((finding) => {
    if (finding.sourceKind !== 'guidedStoryImprovement') {
      return finding;
    }

    const pageName = finding.affectedPages[0];
    const improvementId = finding.id.split('-guided-story-')[1];
    const navigationTarget = pageName && improvementId
      ? targetLookup.get(`finding:${pageName}:${improvementId}`)
      : undefined;

    return navigationTarget
      ? {
          ...finding,
          navigationTarget,
        }
      : finding;
  });
}

function attachTargetsToFixPlan(
  fixPlan: FixPlanItem[] | undefined,
  findings: NormalizedFinding[] | undefined,
): FixPlanItem[] | undefined {
  if (!fixPlan || !findings) {
    return fixPlan;
  }

  const findingTargets = new Map(
    findings
      .filter((finding) => finding.navigationTarget)
      .map((finding) => [finding.id, finding.navigationTarget] as const),
  );

  return fixPlan.map((item) => {
    const navigationTarget = item.sourceFindingIds
      .map((id) => findingTargets.get(id))
      .find((target): target is ScorePanelNavigationTarget => Boolean(target));

    return navigationTarget
      ? {
          ...item,
          navigationTarget,
        }
      : item;
  });
}

export function attachNavigationTargets(result: ScoreResult): ScoreResult {
  const resultPageName = resolveResultPageName(result);
  const pageScores = result.pageScores?.map((page) => ({
    ...page,
    guidedStoryImprovements: mapGuidedStoryImprovements(
      page.pageName,
      page.visualMetadata,
      page.guidedStoryImprovements,
    ),
  }));

  const withPageTargets: ScoreResult = {
    ...result,
    guidedStoryImprovements: resultPageName
      ? mapGuidedStoryImprovements(resultPageName, result.visualMetadata, result.guidedStoryImprovements)
      : result.guidedStoryImprovements,
    pageScores,
  };

  const targetLookup = buildImprovementTargetLookup(withPageTargets);
  const normalizedFindings = attachTargetsToFindings(withPageTargets.normalizedFindings, targetLookup);
  const fixPlan = attachTargetsToFixPlan(withPageTargets.fixPlan, normalizedFindings);

  return {
    ...withPageTargets,
    normalizedFindings,
    fixPlan,
  };
}
