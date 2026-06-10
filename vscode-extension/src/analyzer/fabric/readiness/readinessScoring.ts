import type {
  FabricAppReadinessAssessment,
  FabricAppPageCandidateState,
  FabricAppReadinessBand,
  FabricAppReadinessDimensionScores,
  FabricAppRedesignEffort,
  PageScore,
  ScoreResult,
} from '../../contracts/scorePanel';
import {
  getDefaultFabricScoringConfig,
  type FabricScoringConfig,
} from '../config/fabricScoringConfig';

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

export function scorePageReadiness(
  result: ScoreResult,
  page: PageScore,
  scoringConfig: FabricScoringConfig = getDefaultFabricScoringConfig(),
): FabricAppReadinessDimensionScores {
  const formulas = scoringConfig.readiness.formulas;
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
    layoutPortability: clamp(
      ((page.gestaltScore + page.visualBestPracticesScore + page.densityScore) / 3)
      - Math.max(0, visualCount - formulas.layoutPortability.visualCountThreshold)
        * formulas.layoutPortability.visualPenaltyPerExtra,
    ),
    interactionPortability: clamp(
      formulas.interactionPortability.base
      - (slicerCount * formulas.interactionPortability.slicerPenalty)
      - (navigationVisuals * formulas.interactionPortability.navigationPenalty)
      - (hiddenVisuals * formulas.interactionPortability.hiddenVisualPenalty)
      + (page.actionabilityBreakdown?.drillPathPresent ? formulas.interactionPortability.drillPathBonus : 0),
    ),
    narrativePortability: clamp(
      (page.narrativeScore * formulas.narrativePortability.narrativeWeight)
      + (actionabilityScore * formulas.narrativePortability.actionabilityWeight),
    ),
    semanticModelSuitability: clamp(
      formulas.semanticModelSuitability.base
      + (measureHintCount * formulas.semanticModelSuitability.measureHintBonus)
      + Math.min((page.visualMetadata?.visualCount ?? 0), formulas.semanticModelSuitability.visualBonusCap)
        * formulas.semanticModelSuitability.visualBonusPerVisual
      + (page.pageIntentProfile ? formulas.semanticModelSuitability.pageIntentBonus : 0),
    ),
    navigationPortability: clamp(
      formulas.navigationPortability.base
      - (navigationVisuals * formulas.navigationPortability.navigationPenalty)
      - (slicerCount > formulas.navigationPortability.slicerThreshold
        ? formulas.navigationPortability.slicerThresholdPenalty
        : 0)
      - (navigationDrift ? formulas.navigationPortability.navigationDriftPenalty : 0),
    ),
    governancePortability: clamp(
      (page.enterpriseGovernanceScore * formulas.governancePortability.governanceScoreWeight)
      + (navigationDrift
        ? -formulas.governancePortability.navigationDriftPenalty
        : formulas.governancePortability.stableNavigationBonus),
    ),
    accessibilityPortability: clamp(
      page.accessibilityScore
      - Math.max(0, visualCount - formulas.accessibilityPortability.visualCountThreshold)
        * formulas.accessibilityPortability.visualPenaltyPerExtra
      + (titleCount > 0
        ? formulas.accessibilityPortability.titlePresentBonus
        : -formulas.accessibilityPortability.titleMissingPenalty),
    ),
    visualizationAsCodeOpportunity: clamp(
      formulas.visualizationAsCodeOpportunity.base
      + (titleCount * formulas.visualizationAsCodeOpportunity.titleBonus)
      + (semanticColors * formulas.visualizationAsCodeOpportunity.semanticColorBonus)
      + (measureHintCount * formulas.visualizationAsCodeOpportunity.measureHintBonus)
      - (hiddenVisuals * formulas.visualizationAsCodeOpportunity.hiddenVisualPenalty)
      - Math.max(0, visualCount - formulas.visualizationAsCodeOpportunity.visualCountThreshold)
        * formulas.visualizationAsCodeOpportunity.visualPenaltyPerExtra,
    ),
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
  scoringConfig: FabricScoringConfig = getDefaultFabricScoringConfig(),
): FabricAppPageCandidateState {
  const thresholds = scoringConfig.readiness.thresholds.pageCandidate;
  if (readinessScore >= thresholds.strongCandidateScore && blockerCount <= thresholds.strongCandidateMaxBlockers) {
    return 'strongCandidate';
  }

  if (readinessScore >= thresholds.possibleCandidateScore && blockerCount <= thresholds.possibleCandidateMaxBlockers) {
    return 'possibleCandidate';
  }

  if (readinessScore >= thresholds.redesignRequiredScore) {
    return 'redesignRequired';
  }

  return 'keepAsReport';
}

export function classifyReportReadinessBand(
  readinessScore: number,
  candidatePageCount: number,
  scoringConfig: FabricScoringConfig = getDefaultFabricScoringConfig(),
): FabricAppReadinessBand {
  const thresholds = scoringConfig.readiness.thresholds.reportCandidate;
  if (readinessScore >= thresholds.strongCandidateScore && candidatePageCount >= thresholds.minimumCandidatePages) {
    return 'strongCandidate';
  }

  if (readinessScore >= thresholds.possibleCandidateScore && candidatePageCount >= thresholds.minimumCandidatePages) {
    return 'possibleCandidate';
  }

  if (readinessScore >= thresholds.redesignRequiredScore) {
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
