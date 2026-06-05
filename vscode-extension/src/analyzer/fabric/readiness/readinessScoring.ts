import type {
  FabricAppPageCandidateState,
  FabricAppReadinessBand,
  FabricAppReadinessDimensionScores,
  FabricAppRedesignEffort,
  PageScore,
  ScoreResult,
} from '../../contracts/scorePanel';

function clamp(value: number): number {
  return Math.max(0, Math.min(100, Math.round(value)));
}

function countHiddenVisuals(page: PageScore): number {
  return page.visualMetadata?.visuals.filter((visual) => visual.isHidden).length ?? 0;
}

function countNavigationVisuals(page: PageScore): number {
  return page.visualMetadata?.visuals.filter((visual) => visual.isNavigationElement).length ?? 0;
}

function countMeasureHints(page: PageScore): number {
  return page.visualMetadata?.visuals.reduce((sum, visual) => sum + visual.measureHints.length, 0) ?? 0;
}

function hasNavigationDrift(result: ScoreResult, pageName: string): boolean {
  return (result.normalizedFindings ?? []).some((finding) =>
    finding.impactArea === 'navigation' &&
    (finding.affectedPages.length === 0 || finding.affectedPages.includes(pageName)),
  );
}

export function scorePageReadiness(result: ScoreResult, page: PageScore): FabricAppReadinessDimensionScores {
  const visualCount = page.visualMetadata?.visualCount ?? 0;
  const slicerCount = page.visualMetadata?.slicerCount ?? 0;
  const hiddenVisuals = countHiddenVisuals(page);
  const navigationVisuals = countNavigationVisuals(page);
  const titleCount = page.visualMetadata?.visibleTitleVisualCount ?? 0;
  const semanticColors = page.visualMetadata?.semanticColorMap.length ?? 0;
  const measureHintCount = countMeasureHints(page);
  const actionabilityScore = page.actionabilityBreakdown?.score ?? 60;
  const navigationDrift = hasNavigationDrift(result, page.pageName);

  return {
    layoutPortability: clamp(((page.gestaltScore + page.visualBestPracticesScore + page.densityScore) / 3) - Math.max(0, visualCount - 8) * 3),
    interactionPortability: clamp(82 - (slicerCount * 7) - (navigationVisuals * 5) - (hiddenVisuals * 10) + (page.actionabilityBreakdown?.drillPathPresent ? 4 : 0)),
    narrativePortability: clamp((page.narrativeScore * 0.6) + (actionabilityScore * 0.4)),
    semanticModelSuitability: clamp(52 + (measureHintCount * 8) + Math.min((page.visualMetadata?.visualCount ?? 0), 6) * 3 + (page.pageIntentProfile ? 8 : 0)),
    navigationPortability: clamp(84 - (navigationVisuals * 7) - (slicerCount > 3 ? 12 : 0) - (navigationDrift ? 10 : 0)),
    governancePortability: clamp((page.enterpriseGovernanceScore * 0.8) + (navigationDrift ? -10 : 6)),
    accessibilityPortability: clamp(page.accessibilityScore - Math.max(0, visualCount - 8) * 4 + (titleCount > 0 ? 6 : -6)),
    visualizationAsCodeOpportunity: clamp(48 + (titleCount * 10) + (semanticColors * 8) + (measureHintCount * 5) - (hiddenVisuals * 8) - Math.max(0, visualCount - 10) * 3),
  };
}

export function scoreReadinessOverall(dimensions: FabricAppReadinessDimensionScores): number {
  const weights = [
    dimensions.layoutPortability,
    dimensions.interactionPortability,
    dimensions.narrativePortability,
    dimensions.semanticModelSuitability,
    dimensions.navigationPortability,
    dimensions.governancePortability,
    dimensions.accessibilityPortability,
    dimensions.visualizationAsCodeOpportunity,
  ];

  return clamp(weights.reduce((sum, score) => sum + score, 0) / weights.length);
}

export function classifyPageCandidateState(
  readinessScore: number,
  blockerCount: number,
): FabricAppPageCandidateState {
  if (readinessScore >= 75 && blockerCount === 0) {
    return 'strongCandidate';
  }

  if (readinessScore >= 60 && blockerCount <= 1) {
    return 'possibleCandidate';
  }

  if (readinessScore >= 45) {
    return 'redesignRequired';
  }

  return 'keepAsReport';
}

export function classifyReportReadinessBand(
  readinessScore: number,
  candidatePageCount: number,
): FabricAppReadinessBand {
  if (readinessScore >= 75 && candidatePageCount > 0) {
    return 'strongCandidate';
  }

  if (readinessScore >= 60 && candidatePageCount > 0) {
    return 'possibleCandidate';
  }

  if (readinessScore >= 45) {
    return 'redesignRequired';
  }

  return 'keepAsReport';
}

export function classifyRedesignEffort(
  readinessBand: FabricAppReadinessBand,
): FabricAppRedesignEffort {
  switch (readinessBand) {
    case 'strongCandidate':
      return 'low';
    case 'possibleCandidate':
      return 'medium';
    case 'redesignRequired':
      return 'medium';
    default:
      return 'high';
  }
}
