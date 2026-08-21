import type { AnalyzerProfileId } from '../../analyzers/types';
import type {
  FabricAppPageReadinessAssessment,
  FabricAppReadinessAssessment,
  FabricAppReadinessDimensionScores,
  FabricAppReadinessEvidence,
  PageScore,
  ScoreResult,
} from '../../contracts/scorePanel';
import {
  classifyPageCandidateState,
  classifyRedesignEffort,
  classifyReportReadinessBand,
  scorePageReadiness,
  scoreReadinessOverall,
} from './readinessScoring';
import {
  getDefaultFabricScoringConfig,
  type FabricScoringConfig,
} from '../config/fabricScoringConfig';

function averageDimension(
  pageAssessments: FabricAppPageReadinessAssessment[],
  selector: (dimensions: FabricAppReadinessDimensionScores) => number,
): number {
  if (pageAssessments.length === 0) {
    return 0;
  }

  return Math.round(pageAssessments.reduce((sum, page) => sum + selector(page.readinessDimensions), 0) / pageAssessments.length);
}

function buildPageBlockers(
  page: PageScore,
  dimensions: FabricAppReadinessDimensionScores,
  scoringConfig: FabricScoringConfig,
): string[] {
  const blockers: string[] = [];
  const thresholds = scoringConfig.readiness.thresholds.blockers;

  if ((page.visualMetadata?.slicerCount ?? 0) >= thresholds.slicerHeavyCount) {
    blockers.push('High slicer dependence increases migration complexity.');
  }

  if (dimensions.accessibilityPortability < thresholds.accessibilityLowScore) {
    blockers.push('Low accessibility portability requires redesign before migration.');
  }

  if (dimensions.navigationPortability < thresholds.navigationLowScore) {
    blockers.push('Navigation complexity is likely too Power BI-specific for direct migration.');
  }

  if (dimensions.layoutPortability < thresholds.layoutLowScore) {
    blockers.push('Dense layout portability is weak and will require substantial restructuring.');
  }

  return blockers;
}

function buildUnsupportedPatterns(page: PageScore, scoringConfig: FabricScoringConfig): string[] {
  const patterns: string[] = [];
  const hasHiddenVisual = page.visualMetadata?.visuals.some((visual) => visual.isHidden) ?? false;
  const thresholds = scoringConfig.readiness.thresholds;

  if (hasHiddenVisual) {
    patterns.push('Hidden-visual state switching is difficult to translate directly.');
  }

  if ((page.visualMetadata?.slicerCount ?? 0) >= thresholds.blockers.slicerHeavyCount) {
    patterns.push('Slicer-heavy interaction models usually need redesign for Fabric Apps.');
  }

  if ((page.visualMetadata?.visuals.filter((visual) => visual.isNavigationElement).length ?? 0) >= thresholds.unsupportedPatterns.navigationVisualCount) {
    patterns.push('Power BI navigation shells may not map cleanly to app-native routing.');
  }

  return patterns;
}

function buildPositiveSignals(
  page: PageScore,
  dimensions: FabricAppReadinessDimensionScores,
  scoringConfig: FabricScoringConfig,
): string[] {
  const signals: string[] = [];
  const thresholds = scoringConfig.readiness.thresholds.positiveSignals;

  if ((page.visualMetadata?.visibleTitleVisualCount ?? 0) > 0) {
    signals.push('Clear visible page titling supports app information architecture.');
  }

  if ((page.visualMetadata?.visualCount ?? 0) <= thresholds.focusedVisualCount) {
    signals.push('Focused visual scope is easier to recompose as an app surface.');
  }

  if (dimensions.semanticModelSuitability >= thresholds.semanticModelStructuredScore) {
    signals.push('Semantic-model usage appears structured enough for app migration.');
  }

  if (dimensions.visualizationAsCodeOpportunity >= thresholds.codeFirstOpportunityScore) {
    signals.push('Visualization structure looks amenable to code-first recreation.');
  }

  return signals;
}

function buildMigrationNotes(
  page: PageScore,
  dimensions: FabricAppReadinessDimensionScores,
  scoringConfig: FabricScoringConfig,
): string[] {
  const notes: string[] = [];
  const thresholds = scoringConfig.readiness.thresholds.migrationNotes;

  if (dimensions.narrativePortability < thresholds.narrativeLowScore) {
    notes.push('Clarify the page narrative before treating it as an app candidate.');
  }

  if (dimensions.navigationPortability < thresholds.navigationLowScore) {
    notes.push('Simplify navigation and reduce cross-page shell complexity.');
  }

  if (dimensions.semanticModelSuitability < thresholds.semanticModelLowScore) {
    notes.push('Strengthen semantic labeling and measure framing for app reuse.');
  }

  return notes;
}

function buildRedesignAreas(
  dimensions: FabricAppReadinessDimensionScores,
  scoringConfig: FabricScoringConfig,
): string[] {
  const areas: string[] = [];
  const thresholds = scoringConfig.readiness.thresholds.redesignAreas;

  if (dimensions.layoutPortability < thresholds.layoutLowScore) {
    areas.push('layout portability');
  }

  if (dimensions.navigationPortability < thresholds.navigationLowScore) {
    areas.push('navigation portability');
  }

  if (dimensions.accessibilityPortability < thresholds.accessibilityLowScore) {
    areas.push('accessibility portability');
  }

  if (dimensions.narrativePortability < thresholds.narrativeLowScore) {
    areas.push('narrative portability');
  }

  return areas;
}

function buildEvidence(page: PageScore, dimensions: FabricAppReadinessDimensionScores): FabricAppReadinessEvidence[] {
  return [
    {
      kind: 'pbirMetadata',
      pageName: page.pageName,
      label: 'Visual metadata',
      detail: `${page.visualMetadata?.visualCount ?? 0} visual(s), ${page.visualMetadata?.slicerCount ?? 0} slicer(s), ${page.visualMetadata?.visibleTitleVisualCount ?? 0} title visual(s).`,
    },
    {
      kind: 'interaction',
      pageName: page.pageName,
      label: 'Interaction portability',
      detail: `Interaction portability score ${dimensions.interactionPortability}/100.`,
    },
    {
      kind: 'navigation',
      pageName: page.pageName,
      label: 'Navigation portability',
      detail: `Navigation portability score ${dimensions.navigationPortability}/100.`,
    },
    {
      kind: 'semanticModel',
      pageName: page.pageName,
      label: 'Semantic-model suitability',
      detail: `Semantic-model suitability score ${dimensions.semanticModelSuitability}/100.`,
    },
    {
      kind: 'portability',
      pageName: page.pageName,
      label: 'Migration rationale',
      detail: `Overall readiness score ${scoreReadinessOverall(dimensions)}/100.`,
    },
  ];
}

function buildPageAssessment(
  result: ScoreResult,
  page: PageScore,
  scoringConfig: FabricScoringConfig,
): FabricAppPageReadinessAssessment {
  const readinessDimensions = scorePageReadiness(result, page, scoringConfig);
  const blockers = buildPageBlockers(page, readinessDimensions, scoringConfig);
  const unsupportedPatterns = buildUnsupportedPatterns(page, scoringConfig);
  const readinessScore = scoreReadinessOverall(readinessDimensions);

  return {
    pageName: page.pageName,
    readinessScore,
    readinessDimensions,
    candidateState: classifyPageCandidateState(readinessScore, blockers.length, scoringConfig),
    positiveSignals: buildPositiveSignals(page, readinessDimensions, scoringConfig),
    blockers,
    unsupportedPatterns,
    redesignRequiredAreas: buildRedesignAreas(readinessDimensions, scoringConfig),
    migrationNotes: buildMigrationNotes(page, readinessDimensions, scoringConfig),
    evidence: buildEvidence(page, readinessDimensions),
  };
}

function buildMigrationSummary(assessment: FabricAppReadinessAssessment): string {
  if (assessment.readinessBand === 'strongCandidate') {
    return `This PBIR report is a strong migration candidate with ${assessment.candidatePages.length} candidate page${assessment.candidatePages.length === 1 ? '' : 's'}.`;
  }

  if (assessment.readinessBand === 'possibleCandidate') {
    return `This PBIR report has promising migration candidates, but ${assessment.blockers.length} blocker${assessment.blockers.length === 1 ? '' : 's'} should be addressed first.`;
  }

  if (assessment.readinessBand === 'redesignRequired') {
    return 'This PBIR report can inform a future Fabric App, but meaningful redesign is required first.';
  }

  return 'This PBIR report should likely remain a report until the most Power BI-specific interaction patterns are reduced.';
}

function buildRecommendedNextActions(assessment: FabricAppReadinessAssessment): string[] {
  const actions = new Set<string>();

  for (const page of assessment.pageAssessments) {
    if (page.blockers.some((blocker) => blocker.toLowerCase().includes('slicer'))) {
      actions.add('Reduce Power BI-only dependencies such as slicer-heavy interaction patterns.');
    }

    if (page.blockers.some((blocker) => blocker.toLowerCase().includes('navigation'))) {
      actions.add('Simplify navigation before treating the report as an app candidate.');
    }

    if (page.readinessDimensions.semanticModelSuitability < 65) {
      actions.add('Improve semantic labeling and measure framing for app reuse.');
    }

    if (page.readinessDimensions.narrativePortability < 65) {
      actions.add('Improve narrative hierarchy so each page maps to a clearer app experience.');
    }
  }

  return [...actions];
}

export function assessFabricAppReadiness(
  result: ScoreResult,
  _profile: AnalyzerProfileId = 'migrationReadiness',
  scoringConfig: FabricScoringConfig = getDefaultFabricScoringConfig(),
): FabricAppReadinessAssessment {
  void _profile;
  const pageAssessments = (result.pageScores ?? []).map((page) => buildPageAssessment(result, page, scoringConfig));
  const overallReadinessScore = pageAssessments.length > 0
    ? Math.round(pageAssessments.reduce((sum, page) => sum + page.readinessScore, 0) / pageAssessments.length)
    : 0;
  const candidatePages = pageAssessments
    .filter((page) => page.candidateState === 'strongCandidate' || page.candidateState === 'possibleCandidate')
    .map((page) => page.pageName);
  const blockers = [...new Set(pageAssessments.flatMap((page) => page.blockers))];
  const unsupportedPatterns = [...new Set(pageAssessments.flatMap((page) => page.unsupportedPatterns))];
  const redesignRequiredAreas = [...new Set(pageAssessments.flatMap((page) => page.redesignRequiredAreas))];
  const readinessBand = classifyReportReadinessBand(overallReadinessScore, candidatePages.length, scoringConfig);

  const assessment: FabricAppReadinessAssessment = {
    overallReadinessScore,
    readinessBand,
    migrationSummary: '',
    candidatePages,
    blockers,
    unsupportedPatterns,
    redesignRequiredAreas,
    recommendedNextActions: [],
    estimatedRedesignEffort: classifyRedesignEffort(readinessBand),
    dimensionScores: {
      layoutPortability: averageDimension(pageAssessments, (dimensions) => dimensions.layoutPortability),
      interactionPortability: averageDimension(pageAssessments, (dimensions) => dimensions.interactionPortability),
      narrativePortability: averageDimension(pageAssessments, (dimensions) => dimensions.narrativePortability),
      semanticModelSuitability: averageDimension(pageAssessments, (dimensions) => dimensions.semanticModelSuitability),
      navigationPortability: averageDimension(pageAssessments, (dimensions) => dimensions.navigationPortability),
      governancePortability: averageDimension(pageAssessments, (dimensions) => dimensions.governancePortability),
      accessibilityPortability: averageDimension(pageAssessments, (dimensions) => dimensions.accessibilityPortability),
      visualizationAsCodeOpportunity: averageDimension(pageAssessments, (dimensions) => dimensions.visualizationAsCodeOpportunity),
    },
    pageAssessments,
    evidence: pageAssessments.flatMap((page) => page.evidence),
    governanceSignals: [
      ...pageAssessments
        .filter((page) => page.readinessDimensions.navigationPortability < 60)
        .map((page) => ({
          category: 'navigation' as const,
          severity: 'medium' as const,
          pageName: page.pageName,
          summary: 'Navigation portability falls below the app-ready threshold.',
        })),
      ...pageAssessments
        .filter((page) => page.readinessDimensions.accessibilityPortability < 60)
        .map((page) => ({
          category: 'accessibility' as const,
          severity: 'high' as const,
          pageName: page.pageName,
          summary: 'Accessibility portability falls below the migration-ready threshold.',
        })),
    ],
  };

  assessment.migrationSummary = buildMigrationSummary(assessment);
  assessment.recommendedNextActions = buildRecommendedNextActions(assessment);
  return assessment;
}
